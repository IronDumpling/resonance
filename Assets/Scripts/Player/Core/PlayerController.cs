using UnityEngine;
using System.Collections.Generic;
using Resonance.Player.Data;
using Resonance.Player.States;
using Resonance.Player.Actions;
using Resonance.Player.Shooting;
using Resonance.Player.Inventory;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Utilities.CrystalCore;
using Resonance.Items;
using Resonance.Interfaces.Objects;
using Resonance.Interfaces.Services;
using Resonance.Interfaces.Operations;

namespace Resonance.Player.Core
{
    /// <summary>
    /// Invulnerability tracking - DamageInfo-based system
    /// Tracks recent damage sources to prevent duplicate damage from the same attack
    /// </summary>
    public class DamageSourceRecord
    {
        public GameObject sourceObject;
        public float timestamp;
        
        public DamageSourceRecord(GameObject source, float time)
        {
            sourceObject = source;
            timestamp = time;
        }
    }
    
    /// <summary>
    /// Core player controller that manages player state and behavior.
    /// This is a Non-MonoBehaviour class that handles the player logic.
    /// </summary>
    public class PlayerController : IPausable
    {
        // Core Data
        private PlayerRuntimeStats _stats;
        private PlayerInventory _inventory;
        private PlayerMovement _movement;
        private ShootingSystem _shootingSystem;

        private WeaponManager _weaponManager;
        private ConsumableManager _consumableManager;
        private InventoryOperationManager _gridOperationManager;

        // Player State Management
        private PlayerStateMachine _stateMachine;
        private PlayerActionController _actionController;

        // Services
        private IAudioService _audioService;
        private GameObject _playerGameObject; // For 3D audio positioning

        // Combat State
        private float _lastAttackTime = 0f;

        // Dual Health Events
        public System.Action<float, float> OnHealthChanged; // current, max
        public System.Action<float, float> OnCoreEnergyChanged; // current, max
        public System.Action OnDeath; // Health reaches 0
        
        // Health Tier Events
        public System.Action<CrystalEnergyTier> OnCoreTierChanged;
        public System.Action<HealthTier> OnHealthTierChanged;
        
        // Other Events
        public System.Action<string> OnStateChanged; // Changed to string for state name
        public System.Action OnShoot;

        // Properties
        public PlayerRuntimeStats Stats => _stats;
        public PlayerInventory Inventory => _inventory;
        public PlayerMovement Movement => _movement;
        public WeaponManager WeaponManager => _weaponManager;
        public ShootingSystem ShootingSystem => _shootingSystem;
        public ConsumableManager ConsumableManager => _consumableManager;
        public InventoryOperationManager InventoryOperationManager => _gridOperationManager;
        public GameObject PlayerGameObject => _playerGameObject;
        
        // Health Properties
        public bool IsAlive => _stats.IsAlive;
        public bool IsCoreAlive => _stats.crystalCore != null && _stats.crystalCore.CoreHealthState == CoreHealthState.Intact;
        public bool IsCoreDestroyed => _stats.IsCoreDestroyed;

        // State Properties
        public bool IsAiming => CurrentState == "Aiming";
        public bool IsStunned => CurrentState == "Stun";

        // Health Tier Properties
        public CrystalEnergyTier CoreTier => _stats.crystalCore.EnergyTier;
        public HealthTier HealthTier => _stats.healthTier;
        public float SlotValue => _stats.crystalCore.EnergyPerSlot;
        public float CoreHealthInSlots => _stats.crystalCore.GetEnergyInSlots();
        public bool CanConsumeSlot => _stats.crystalCore.CanConsumeSlot();
        
        public string CurrentState => _stateMachine?.CurrentStateName ?? "None";
        public bool HasEquippedWeapon => _weaponManager?.HasEquippedWeapon ?? false;
        public PlayerStateMachine StateMachine => _stateMachine;
        public PlayerActionController PlayerActionController => _actionController;

        public PlayerController(PlayerBaseStats baseStats)
        {
            Initialize(baseStats, null);
        }

        /// <summary>
        /// Initialize the PlayerController
        /// </summary>
        /// <param name="baseStats">Base stats</param>
        /// <param name="playerGameObject">Player GameObject (for shooting system and audio positioning)</param>
        public void Initialize(PlayerBaseStats baseStats, GameObject playerGameObject)
        {
            Initialize(baseStats);
            
            // Get the audio service
            _audioService = ServiceRegistry.Get<IAudioService>();
            if (_audioService == null)
            {
                Debug.LogWarning("PlayerController: AudioService not found. Audio effects will be disabled.");
            }
            
            // If there is a GameObject reference, initialize the shooting system
            if (playerGameObject != null)
            {
                _shootingSystem = new ShootingSystem(playerGameObject);
                Debug.Log("PlayerController: ShootingSystem initialized");
                _playerGameObject = playerGameObject;
            }
        }

        private void Initialize(PlayerBaseStats baseStats)
        {
            _stats = baseStats.CreateRuntimeStats();
            _inventory = new PlayerInventory(_stats.inventoryGridWidth, _stats.inventoryGridHeight);
            _movement = new PlayerMovement(_stats);
            
            // Initialize invulnerability duration
            _invulnerabilityDuration = _stats.invulnerabilityTime;
            
            // Initialize inventory managers
            _consumableManager = new ConsumableManager(_inventory);
            _weaponManager = new WeaponManager(_inventory);
            _gridOperationManager = new InventoryOperationManager(_inventory, _weaponManager, _consumableManager);
            
            // Set PlayerController reference for PlayerMovement (for state-based speed calculation)
            _movement.SetPlayerController(this);

            // Initialize state machine
            _stateMachine = new PlayerStateMachine(this);
            _stateMachine.OnStateChanged += (stateName) => OnStateChanged?.Invoke(stateName);
            _stateMachine.Initialize();

            // Initialize action controller
            _actionController = new PlayerActionController(this);
            _actionController.Initialize();
            
            // Register available actions
            RegisterPlayerActions();

            // Register with SelectivePauseService
            RegisterWithPauseService();
        }

        /// <summary>
        /// Update player controller (called from MonoBehaviour)
        /// </summary>
        public void Update(float deltaTime)
        {
            UpdateInvulnerabilityTimer();
            _stateMachine?.Update();

            UpdateStunTimer();
            _stats.UpdateChaos(deltaTime);
            _movement.Update(deltaTime);
            _actionController.Update(deltaTime);
        }

        #region Health System

        // Stun tracking
        private float _stunEndTime = 0f;
        
        private List<DamageSourceRecord> _recentDamageSources = new List<DamageSourceRecord>();
        private float _invulnerabilityDuration = 0f; // Cache the duration

        /// <summary>
        /// Update chaos (natural recovery) and clean up old damage records
        /// </summary>
        private void UpdateInvulnerabilityTimer()
        {
            // Clean up old damage source records
            // remove entries older than invulnerability duration
            if (_recentDamageSources.Count > 0 && _invulnerabilityDuration > 0f)
            {
                float currentTime = Time.time;
                _recentDamageSources.RemoveAll(record => 
                    currentTime - record.timestamp > _invulnerabilityDuration
                );
            }
        }

        
        private void UpdateStunTimer()
        {
            if (CurrentState == "Stun" && Time.time >= _stunEndTime)
            {
                ExitStun();
            }
        }
        
        /// <summary>
        /// Check if a DamageInfo should be blocked by invulnerability
        /// Returns true if this damage source was already processed recently
        /// </summary>
        private bool ShouldBlockDamageInfo(DamageInfo damageInfo)
        {
            if (_invulnerabilityDuration <= 0f) return false; // No invulnerability system
            if (damageInfo.sourceObject == null) return false; // No source to track
            
            float currentTime = Time.time;
            
            // Check if this source has hit recently
            foreach (var record in _recentDamageSources)
            {
                if (record.sourceObject == damageInfo.sourceObject && 
                    currentTime - record.timestamp < _invulnerabilityDuration)
                {
                    return true; // Block: this source already dealt damage recently
                }
            }
            
            return false; // Allow: new damage source
        }
        
        /// <summary>
        /// Register a damage source to prevent duplicate hits
        /// Should be called once per DamageInfo, not per damage type
        /// </summary>
        private void RegisterDamageSource(DamageInfo damageInfo)
        {
            if (_invulnerabilityDuration <= 0f) return;
            if (damageInfo.sourceObject == null) return;
            
            _recentDamageSources.Add(new DamageSourceRecord(
                damageInfo.sourceObject, 
                Time.time
            ));
            
            Debug.Log($"PlayerController: Registered damage source '{damageInfo.sourceObject.name}', " +
                     $"invulnerable for {_invulnerabilityDuration}s");
        }

        /// <summary>
        /// Add chaos value (when taking chaos damage)
        /// </summary>
        private void AddChaos(float amount)
        {
            if (amount <= 0f) return;
            
            float actualAdded = _stats.crystalCore.AddChaos(amount);
            
            if (actualAdded > 0f)
            {
                // Enter stun state proportional to chaos damage
                // Each point of chaos = 0.1 seconds of stun (adjustable)
                float stunDuration = actualAdded * 0.1f;
                EnterStun(stunDuration);
                
                Debug.Log($"PlayerController: Added {actualAdded} chaos, entering stun for {stunDuration:F2}s");
            }
        }

        /// <summary>
        /// Enter stun state
        /// </summary>
        private void EnterStun(float duration)
        {
            if (CurrentState == "Death") return;
            
            _stunEndTime = Time.time + duration;
            _stateMachine?.EnterStun();
            
            Debug.Log($"PlayerController: Entered stun state for {duration:F2}s");
        }

        /// <summary>
        /// Exit stun state
        /// </summary>
        private void ExitStun()
        {
            if (CurrentState != "Stun") return;
            
            _stateMachine?.ExitStun();
            
            Debug.Log("PlayerController: Exited stun state");
        }

        /// <summary>
        /// Take damage from a DamageInfo (unified entry point)
        /// Handles invulnerability check at the DamageInfo level, not per damage type
        /// This ensures all damage types from the same attack are processed together
        /// </summary>
        public void TakeDamage(DamageInfo damageInfo)
        {
            // Check if this damage source should be blocked by invulnerability
            if (ShouldBlockDamageInfo(damageInfo))
            {
                Debug.Log($"PlayerController: Damage from '{damageInfo.sourceObject?.name}' blocked by invulnerability");
                return;
            }
            
            // Register this damage source for invulnerability tracking
            RegisterDamageSource(damageInfo);
            
            // Process all damage types in the DamageInfo
            Damages damages = damageInfo.damages;
            if (damages == null) return;
            
            bool tookAnyDamage = false;
            
            // Apply Physical Health damage
            if (damages.HasDamage(DamageType.PhysicalHealth))
            {
                float damageAmount = damages.GetDamage(DamageType.PhysicalHealth);
                TakeHealthDamage(damageAmount);
                tookAnyDamage = true;
            }
            
            // Apply Core Health damage
            if (damages.HasDamage(DamageType.CoreHealth))
            {
                float damageAmount = damages.GetDamage(DamageType.CoreHealth);
                TakeCoreDamage(damageAmount);
                tookAnyDamage = true;
            }
            
            // Apply Chaos damage (processed last to avoid stun blocking other damage)
            if (damages.HasDamage(DamageType.Chaos))
            {
                float damageAmount = damages.GetDamage(DamageType.Chaos);
                TakeChaosDamage(damageAmount);
                tookAnyDamage = true;
            }
            
            // Trigger common effects only once per DamageInfo
            if (tookAnyDamage)
            {
                // Notify PlayerActionController of damage taken (for interruption logic)
                _actionController?.OnPlayerDamageTaken();
                
                // Interrupt aiming when taking damage
                if (_stateMachine != null && _stateMachine.IsInState("Aiming"))
                {
                    _stateMachine.StopAiming();
                    Debug.Log("PlayerController: Aiming interrupted by damage");
                }

                // Play hit audio effect (once per DamageInfo)
                PlayHitAudio();
            }
            
            Debug.Log($"PlayerController: Processed DamageInfo - {damageInfo}");
        }
        
        /// <summary>
        /// Take physical health damage (internal method, called from TakeDamage)
        /// </summary>
        private void TakeHealthDamage(float damage)
        {
            if (!IsAlive) return;

            // Store old tier for comparison
            var oldHealthTier = _stats.healthTier;

            _stats.TakeHealthDamage(damage);
            _stats.UpdateHealthTier();
            
            // Fire tier change event if tier changed
            if (oldHealthTier != _stats.healthTier)
            {
                OnHealthTierChanged?.Invoke(_stats.healthTier);
                Debug.Log($"PlayerController: Health tier changed from {oldHealthTier} to {_stats.healthTier}");
            }

            OnHealthChanged?.Invoke(_stats.currentHealth, _stats.maxHealth);

            if (_stats.currentHealth <= 0f)
            {
                HandleDeath();
            }
            else
            {
                Debug.Log($"PlayerController: Took {damage} health damage. Current: {_stats.currentHealth}/{_stats.maxHealth}");
            }
        }

        /// <summary>
        /// Take core health damage (internal method, called from TakeDamage)
        /// </summary>
        private void TakeCoreDamage(float damage)
        {
            if (!IsCoreAlive) return;

            // Store old tier for comparison
            var oldCoreTier = _stats.crystalCore.EnergyTier;

            _stats.crystalCore.TakeCoreHealthDamage(damage);
            _stats.crystalCore.UpdateCalculatedValues();
            
            // Fire tier change event if tier changed
            if (oldCoreTier != _stats.crystalCore.EnergyTier)
            {
                OnCoreTierChanged?.Invoke(_stats.crystalCore.EnergyTier);
                Debug.Log($"PlayerController: Core tier changed from {oldCoreTier} to {_stats.crystalCore.EnergyTier}");
            }

            OnCoreEnergyChanged?.Invoke(_stats.crystalCore.CurrentEnergy, _stats.crystalCore.CurrentCoreHealth);

            if (_stats.crystalCore.CurrentCoreHealth <= 0f)
            {
                HandleCoreDeath();
            }
            else
            {
                Debug.Log($"PlayerController: Took {damage} core health damage. Current: {_stats.crystalCore.CurrentCoreHealth}/{_stats.crystalCore.MaxCoreHealth}");
            }
        }

        /// <summary>
        /// Take chaos damage (causes stun) (internal method, called from TakeDamage)
        /// </summary>
        private void TakeChaosDamage(float damage)
        {
            if (!IsCoreAlive) return;
            
            AddChaos(damage);
        }

        /// <summary>
        /// Heal health health
        /// </summary>
        public void HealHealth(float amount)
        {
            if (!IsCoreAlive) return;

            // Store old tier for comparison
            var oldHealthTier = _stats.healthTier;

            _stats.RestoreHealth(amount);
            _stats.UpdateHealthTier();
            
            // Fire tier change event if tier changed
            if (oldHealthTier != _stats.healthTier)
            {
                OnHealthTierChanged?.Invoke(_stats.healthTier);
                Debug.Log($"PlayerController: Health tier changed from {oldHealthTier} to {_stats.healthTier}");
            }

            OnHealthChanged?.Invoke(_stats.currentHealth, _stats.maxHealth);
            Debug.Log($"PlayerController: Healed {amount} health health, current: {_stats.currentHealth}");
        }

        /// <summary>
        /// Gain core energy
        /// </summary>
        public void GainCoreEnergy(float amount)
        {
            if (!IsCoreAlive) return;

            // Store old tier for comparison
            var oldCoreTier = _stats.crystalCore.EnergyTier;

            _stats.crystalCore.AddEnergy(amount);
            _stats.crystalCore.UpdateCalculatedValues();
            
            // Fire tier change event if tier changed
            if (oldCoreTier != _stats.crystalCore.EnergyTier)
            {
                OnCoreTierChanged?.Invoke(_stats.crystalCore.EnergyTier);
                Debug.Log($"PlayerController: Core tier changed from {oldCoreTier} to {_stats.crystalCore.EnergyTier}");
            }

            OnCoreEnergyChanged?.Invoke(_stats.crystalCore.CurrentEnergy, _stats.crystalCore.CurrentCoreHealth);
            Debug.Log($"PlayerController: Gained {amount} core energy, current: {_stats.crystalCore.CurrentEnergy}");
        }

        /// <summary>
        /// Handle physical health death
        /// </summary>
        private void HandleDeath()
        {
            // Prevent multiple calls - only trigger if not already in death states
            if (_stateMachine?.IsDead() == true)
            {
                return;
            }
            
            Debug.Log("PlayerController: Physical health depleted - Player death");
            OnDeath?.Invoke();
            _stateMachine?.EnterDeath();
        }

        /// <summary>
        /// Handle core health death (core destroyed)
        /// </summary>
        private void HandleCoreDeath()
        {
            Debug.Log("PlayerController: Core health depleted - Core destroyed (player can continue)");
            // Core destroyed but player doesn't die
            // This could trigger special effects or UI updates
        }

        /// <summary>
        /// Restore all health to full
        /// </summary>
        public void RestoreToFullHealth()
        {
            _stats.FullRestore();
            _stats.crystalCore.FullRepairCoreHealth();
            OnHealthChanged?.Invoke(_stats.currentHealth, _stats.maxHealth);
            OnCoreEnergyChanged?.Invoke(_stats.crystalCore.CurrentEnergy, _stats.crystalCore.CurrentCoreHealth);
            Debug.Log("PlayerController: All health restored to full");
        }

        /// <summary>
        /// Restore only health health
        /// </summary>
        public void RestoreHealth()
        {
            _stats.FullRestore();
            OnHealthChanged?.Invoke(_stats.currentHealth, _stats.maxHealth);
            Debug.Log("PlayerController: Physical health restored to full");
        }

        /// <summary>
        /// Restore only core health
        /// </summary>
        public void RestoreCoreHealth()
        {
            _stats.crystalCore.FullRepairCoreHealth();
            OnCoreEnergyChanged?.Invoke(_stats.crystalCore.CurrentEnergy, _stats.crystalCore.CurrentCoreHealth);
            Debug.Log("PlayerController: Core health restored to full");
        }

        /// <summary>
        /// Play hit audio effect when player takes damage
        /// </summary>
        private void PlayHitAudio()
        {
            if (_audioService == null) return;

            // Use 3D audio if we have player GameObject, otherwise use 2D
            if (_playerGameObject != null)
            {
                _audioService.PlaySFX3D(AudioClipType.PlayerHit, _playerGameObject.transform.position, 0.8f, 1f);
            }
            else
            {
                _audioService.PlaySFX2D(AudioClipType.PlayerHit, 0.8f, 1f);
            }
        }

        #endregion

        #region State Management

        public void StartAiming()
        {
            _stateMachine?.StartAiming();
        }

        public void StopAiming()
        {
            _stateMachine?.StopAiming();
        }

        #endregion

        #region Combat System

        public bool CanShoot()
        {
            return IsAlive && 
                   !IsStunned && // Cannot shoot while stunned
                   _stateMachine.CanShoot() && 
                   Time.time >= _lastAttackTime && 
                   !(_actionController?.IsBlocking ?? false); // Actions can block shooting
        }

        public bool CanReload()
        {
            return IsAlive && 
                   !IsStunned && // Cannot reload while stunned
                   _stateMachine.CanReload() &&
                   !IsAiming &&
                   !(_actionController?.IsActive ?? false); // Cannot reload while another action is active
        }

        /// <summary>
        /// Perform shoot
        /// </summary>
        /// <param name="shootOrigin">Shoot origin</param>
        /// <returns>Shooting result</returns>
        public ShootingResult PerformShoot(Vector3 shootOrigin)
        {
            if (!CanShoot())
            {
                return new ShootingResult { success = false };
            }

            // Consume ammo
            if (!_weaponManager.ConsumeAmmo())
            {
                Debug.LogWarning("PlayerController: Failed to consume ammo");
                return new ShootingResult { success = false };
            }

            _lastAttackTime = Time.time;
            
            WeaponDataAsset currentWeapon = _weaponManager.CurrentWeapon;
            
            // Perform shoot
            ShootingResult result = new ShootingResult { success = false };
            if (_shootingSystem != null)
            {
                // Pass aiming state to ShootingSystem
                bool isAiming = _stateMachine?.IsInState("Aiming") ?? false;
                result = _shootingSystem.PerformShoot(shootOrigin, currentWeapon, isAiming);
                
                // Core energy gain: ONLY from actual physical health damage
                if (result.success && result.hasHit)
                {
                    float actualPhysicalDamage = result.GetActualDamage(DamageType.PhysicalHealth);
                    if (actualPhysicalDamage > 0)
                    {
                        float coreGain = actualPhysicalDamage * _stats.physicalDamageToCoreEnergyRatio;
                        GainCoreEnergy(coreGain);
                        Debug.Log($"PlayerController: Gained {coreGain:F1} core energy from dealing {actualPhysicalDamage:F1} actual physical health damage");
                    }
                }
            }
            
            // Trigger shooting event
            OnShoot?.Invoke();
            
            Debug.Log($"PlayerController: Mouse-based shot fired with {currentWeapon.weaponName}. " +
                     $"Target: {result.mouseTargetPoint}, Hit: {result.hasHit}, " +
                     $"Total Base: {result.GetTotalBaseDamage():F1}, Total Actual: {result.GetTotalActualDamage():F1}, " +
                     $"Remaining ammo: {currentWeapon.CurrentAmmo}");
            
            return result;
        }

        #endregion

        #region Save/Load System

        public void LoadFromSaveData(PlayerSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogError("PlayerController: Cannot load from null save data");
                return;
            }

            Debug.Log($"PlayerController: Loading save data from {saveData.saveID}");

            // Load stats
            _stats = saveData.stats;
            Debug.Log($"PlayerController: Loaded stats: Health {_stats.currentHealth}/{_stats.maxHealth}");

            // Load grid inventory system
            if (saveData.gridInventory != null)
            {
                Debug.Log($"PlayerController: Loading grid inventory data: {saveData.gridInventory.items.Count} items");
                _inventory.LoadFromSaveData(saveData.gridInventory);
                Debug.Log($"PlayerController: Grid inventory loaded successfully. Current inventory has {_inventory.UsedSlots} items");
            }
            else
            {
                Debug.LogWarning("PlayerController: No grid inventory data found in save data");
            }

            // Load weapon manager state
            if (saveData.weaponManager != null)
            {
                Debug.Log($"PlayerController: Loading weapon manager data: equipped weapon ID {saveData.weaponManager.equippedWeaponID}, weapon name: {saveData.weaponManager.weaponName}");
                _weaponManager.LoadFromSaveData(saveData.weaponManager);
            }
            else
            {
                Debug.LogWarning("PlayerController: No weapon manager data found in save data");
            }

            Debug.Log($"PlayerController: Loaded save data from {saveData.saveID}");

            // Notify UI of dual health changes
            OnHealthChanged?.Invoke(_stats.currentHealth, _stats.maxHealth);
            OnCoreEnergyChanged?.Invoke(_stats.crystalCore.CurrentEnergy, _stats.crystalCore.CurrentCoreHealth);
        }

        public PlayerSaveData CreateSaveData(string savePointID, Vector3 position, Vector3 rotation)
        {
            Debug.Log($"PlayerController: Creating save data for {savePointID} at position {position}");
            
            var saveData = new PlayerSaveData
            {
                saveID = savePointID,
                sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                savePosition = position,
                saveRotation = rotation,
                stats = _stats
            };

            // Save grid inventory system
            saveData.gridInventory = _inventory.GetSaveData();
            Debug.Log($"PlayerController: Grid inventory saved: {saveData.gridInventory.items.Count} items");
            
            // Save weapon manager state
            saveData.weaponManager = _weaponManager.GetSaveData();
            Debug.Log($"PlayerController: Weapon manager saved: equipped weapon ID {saveData.weaponManager.equippedWeaponID}, weapon name: {saveData.weaponManager.weaponName}");
            return saveData;
        }

        #endregion

        #region Action Management
        
        /// <summary>
        /// Register a new action with the PlayerActionController
        /// </summary>
        /// <param name="action">The action to register</param>
        public void RegisterAction(IPlayerAction action)
        {
            _actionController?.RegisterAction(action);
        }

        /// <summary>
        /// Try to start an action by name
        /// </summary>
        /// <param name="actionName">Name of the action to start</param>
        /// <returns>True if action was started successfully</returns>
        public bool TryStartAction(string actionName)
        {
            return _actionController?.TryStartAction(actionName) ?? false;
        }

        /// <summary>
        /// Cancel the currently running action
        /// </summary>
        public void CancelCurrentAction()
        {
            _actionController?.CancelCurrentAction();
        }

        /// <summary>
        /// Check if a specific action can start
        /// </summary>
        /// <param name="actionName">Name of the action to check</param>
        /// <returns>True if action can start</returns>
        public bool CanStartAction(string actionName)
        {
            return _actionController?.CanStartAction(actionName) ?? false;
        }

        /// <summary>
        /// Get the name of the currently running action
        /// </summary>
        /// <returns>Name of current action or "None"</returns>
        public string GetCurrentActionName()
        {
            return _actionController?.CurrentActionName ?? "None";
        }

        /// <summary>
        /// Check if an action is currently running
        /// </summary>
        /// <returns>True if an action is active</returns>
        public bool IsActionActive()
        {
            return _actionController?.IsActive ?? false;
        }

        /// <summary>
        /// Register all available player actions with the PlayerActionController
        /// </summary>
        private void RegisterPlayerActions()
        {
            RegisterAction(new PlayerWaveAttackAction());
            RegisterAction(new PlayerWaveDefenceAction());
            RegisterAction(new PlayerHealAction());
            RegisterAction(new PlayerInteractAction());
            RegisterAction(new PlayerReloadAction());

            Debug.Log("PlayerController: Registered player actions (WaveAttack, WaveDefence, Heal, Interact, Reload)");
        }

        #endregion

        #region Core Energy Slot Management
        
        /// <summary>
        /// Consume one core energy slot for actions
        /// </summary>
        /// <returns>True if successful, false if insufficient core energy</returns>
        public bool ConsumeSlot()
        {
            var oldCoreTier = _stats.crystalCore.EnergyTier;
            bool success = _stats.crystalCore.ConsumeEnergySlot();
            
            if (success)
            {
                // Fire events
                OnCoreEnergyChanged?.Invoke(_stats.crystalCore.CurrentEnergy, _stats.crystalCore.CurrentCoreHealth);
                
                if (oldCoreTier != _stats.crystalCore.EnergyTier)
                {
                    OnCoreTierChanged?.Invoke(_stats.crystalCore.EnergyTier);
                    Debug.Log($"PlayerController: Core tier changed from {oldCoreTier} to {_stats.crystalCore.EnergyTier} after slot consumption");
                }
                
                Debug.Log($"PlayerController: Consumed 1 slot ({_stats.crystalCore.EnergyPerSlot} core energy)." +
                $"Remaining: {_stats.crystalCore.CurrentEnergy}/{_stats.crystalCore.CurrentCoreHealth} ({_stats.crystalCore.GetEnergyInSlots():F1} slots)");
            }
            
            return success;
        }

        #endregion

        #region IPausable Implementation

        private bool _isPaused = false;

        public bool IsPaused => _isPaused;

        private void RegisterWithPauseService()
        {
            var pauseService = ServiceRegistry.Get<ISelectivePauseService>();
            if (pauseService != null)
            {
                pauseService.RegisterPausable(this);
                Debug.Log("PlayerController: Registered with SelectivePauseService");
            }
            else
            {
                Debug.LogWarning("PlayerController: SelectivePauseService not found, pause functionality will not work");
            }

            Debug.Log("PlayerController: Initialized with base stats, weapon manager, state machine, and action controller");
        }

        public void Pause()
        {
            if (_isPaused) return;
            
            _isPaused = true;
            Debug.Log("PlayerController: Paused");
        }

        public void Resume()
        {
            if (!_isPaused) return;
            
            _isPaused = false;
            Debug.Log("PlayerController: Resumed");
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleanup the player controller when it's being destroyed
        /// </summary>
        public void Cleanup()
        {
            // Cleanup action controller
            _actionController?.Cleanup();

            // Cleanup state machine
            _stateMachine?.Shutdown();
            
            // Cleanup inventory managers
            _consumableManager?.Cleanup();
            _gridOperationManager?.Cleanup();
            _weaponManager?.Cleanup();

            // Clear events
            OnHealthChanged = null;
            OnCoreEnergyChanged = null;
            OnDeath = null;
            OnCoreTierChanged = null;
            OnHealthTierChanged = null;
            OnStateChanged = null;
            OnShoot = null;

            // Unregister from SelectivePauseService
            var pauseService = ServiceRegistry.Get<ISelectivePauseService>();
            if (pauseService != null)
            {
                pauseService.UnregisterPausable(this);
                Debug.Log("PlayerController: Unregistered from SelectivePauseService");
            }

            Debug.Log("PlayerController: Cleaned up");
        }

        #endregion
    }
}
