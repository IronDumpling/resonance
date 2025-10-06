using UnityEngine;
using System.Collections.Generic;
using Resonance.Player.Data;
using Resonance.Player.States;
using Resonance.Player.Actions;
using Resonance.Player.Inventory;
using Resonance.Core;
using Resonance.Core.Data;
using Resonance.Utilities;
using Resonance.Items;
using Resonance.Interfaces.Objects;
using Resonance.Interfaces.Services;

namespace Resonance.Player.Core
{
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
        private GridOperationManager _gridOperationManager;

        // Player State Management
        private PlayerStateMachine _stateMachine;
        private ActionController _actionController;

        // Services
        private IAudioService _audioService;
        private GameObject _playerGameObject; // For 3D audio positioning

        // Combat State
        private bool _isInvulnerable = false;
        private float _invulnerabilityTimer = 0f;
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
        public GridOperationManager GridOperationManager => _gridOperationManager;
        public bool IsInvulnerable => _isInvulnerable;
        
        // Dual Health Properties
        public bool IsAlive => _stats.IsAlive;
        public bool IsCoreAlive => _stats.crystalCore.IsIntact;
        public bool IsInDeathState => _stats.IsDead;
        
        // Health Tier Properties
        public CrystalEnergyTier CoreTier => _stats.crystalCore.EnergyTier;
        public HealthTier HealthTier => _stats.healthTier;
        public float SlotValue => _stats.crystalCore.EnergyPerSlot;
        public float CoreHealthInSlots => _stats.crystalCore.GetEnergyInSlots();
        public bool CanConsumeSlot => _stats.crystalCore.CanConsumeSlot();
        
        public string CurrentState => _stateMachine?.CurrentStateName ?? "None";
        public bool IsAiming => CurrentState == "Aiming";
        public bool IsStunned => CurrentState == "Stun";
        public bool HasEquippedWeapon => _weaponManager?.HasEquippedWeapon ?? false;
        public PlayerStateMachine StateMachine => _stateMachine;
        public ActionController ActionController => _actionController;

        public PlayerController(PlayerBaseStats baseStats)
        {
            Initialize(baseStats, null);
        }

        /// <summary>
        /// 初始化PlayerController，需要PlayerMonoBehaviour传入GameObject引用
        /// </summary>
        /// <param name="baseStats">基础属性</param>
        /// <param name="playerGameObject">玩家GameObject（用于射击系统和音频定位）</param>
        public void Initialize(PlayerBaseStats baseStats, GameObject playerGameObject)
        {
            Initialize(baseStats);
            
            // 获取音频服务
            _audioService = ServiceRegistry.Get<IAudioService>();
            if (_audioService == null)
            {
                Debug.LogWarning("PlayerController: AudioService not found. Audio effects will be disabled.");
            }
            
            // 如果有GameObject引用，初始化射击系统
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
            
            // Initialize inventory managers
            _consumableManager = new ConsumableManager(_inventory);
            _gridOperationManager = new GridOperationManager(_inventory, _consumableManager);
            _weaponManager = new WeaponManager(_inventory);
            
            // Set PlayerController reference for PlayerMovement (for state-based speed calculation)
            _movement.SetPlayerController(this);

            // Initialize state machine
            _stateMachine = new PlayerStateMachine(this);
            _stateMachine.OnStateChanged += (stateName) => OnStateChanged?.Invoke(stateName);
            _stateMachine.Initialize();

            // Initialize action controller
            _actionController = new ActionController(this);
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
            UpdateInvulnerability(deltaTime);
            UpdateResilience(deltaTime);
            _movement.Update(deltaTime);
            _stateMachine?.Update();
            _actionController.Update(deltaTime);
        }

        #region Health System

        /// <summary>
        /// Update invulnerability timer
        /// </summary>
        private void UpdateInvulnerability(float deltaTime)
        {
            if (_isInvulnerable)
            {
                _invulnerabilityTimer -= deltaTime;
                if (_invulnerabilityTimer <= 0f)
                {
                    _isInvulnerable = false;
                }
            }
        }

        /// <summary>
        /// Update resilience
        /// </summary>
        private void UpdateResilience(float deltaTime)
        {
            _stats.UpdateResilience(deltaTime);

            // Empty resilience -> enter stun
            if (_stats.currentResilience <= 0.1f && CurrentState != "Stun" && CurrentState != "Death")
            {
                HandleStun();
            }
            
            // Resilience > stun threshold -> exit stun
            if (_stats.currentResilience > _stats.stunThreshold && CurrentState == "Stun" && CurrentState != "Death")
            {
                HandleStunEnd();
            }
        }

        /// <summary>
        /// Take health damage (affects health health)
        /// </summary>
        public void TakeHealthDamage(float damage)
        {
            if (_isInvulnerable || !IsCoreAlive) return;

            // Check Action system invulnerability
            if (_actionController?.IsInvulnerable == true) return;

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

            // Notify ActionController of damage taken (for interruption logic)
            _actionController?.OnPlayerDamageTaken();

            // Play hit audio effect
            PlayHitAudio();

            if (_stats.currentHealth <= 0f)
            {
                HandleDeath();
            }
            else
            {
                // Start invulnerability period for health damage
                _isInvulnerable = true;
                _invulnerabilityTimer = _stats.invulnerabilityTime;
                Debug.Log($"PlayerController: Took {damage} health damage, health health: {_stats.currentHealth}");
            }
        }

        /// <summary>
        /// Take core damage (affects core capacity)
        /// </summary>
        public void TakeCoreDamage(float damage)
        {
            if (!IsCoreAlive) return;

            // Store old tier for comparison
            var oldCoreTier = _stats.crystalCore.EnergyTier;

            _stats.crystalCore.DamageCapacity(damage);
            _stats.crystalCore.UpdateCalculatedValues();
            
            // Fire tier change event if tier changed
            if (oldCoreTier != _stats.crystalCore.EnergyTier)
            {
                OnCoreTierChanged?.Invoke(_stats.crystalCore.EnergyTier);
                Debug.Log($"PlayerController: Core tier changed from {oldCoreTier} to {_stats.crystalCore.EnergyTier}");
            }

            OnCoreEnergyChanged?.Invoke(_stats.crystalCore.CurrentEnergy, _stats.crystalCore.CurrentEnergyCapacity);

            if (_stats.crystalCore.CurrentEnergyCapacity <= 0f)
            {
                HandleDeath();
            }
            else
            {
                Debug.Log($"PlayerController: Took {damage} core damage, core capacity: {_stats.crystalCore.CurrentEnergyCapacity}");
            }
        }

        /// <summary>
        /// Take resilience damage
        /// </summary>
        public void TakeResilienceDamage(float damage)
        {
            if (!IsCoreAlive) return;

            _stats.TakeResilienceDamage(damage);

            Debug.Log($"PlayerController: Took {damage} resilience damage, resilience: {_stats.currentResilience}");
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

            OnCoreEnergyChanged?.Invoke(_stats.crystalCore.CurrentEnergy, _stats.crystalCore.CurrentEnergyCapacity);
            Debug.Log($"PlayerController: Gained {amount} core energy, current: {_stats.crystalCore.CurrentEnergy}");
        }

        /// <summary>
        /// Handle death (health reaches 0)
        /// </summary>
        private void HandleDeath()
        {
            // Prevent multiple calls - only trigger if not already in death states
            if (_stateMachine?.IsDead() == true)
            {
                return;
            }
            
            Debug.Log("PlayerController: Death - game over");
            OnDeath?.Invoke();
            _stateMachine?.EnterDeath();
        }

        /// <summary>
        /// Handle stun (resilience reaches stun threshold)
        /// </summary>
        private void HandleStun()
        {
            _stateMachine?.EnterStun();
        }

        /// <summary>
        /// Handle stun end (resilience reaches normal threshold)
        /// </summary>
        private void HandleStunEnd()
        {
            _stateMachine?.ExitStun();
        }

        /// <summary>
        /// Restore all health to full
        /// </summary>
        public void RestoreToFullHealth()
        {
            _stats.FullRestore();
            _stats.crystalCore.FullRepair();
            OnHealthChanged?.Invoke(_stats.currentHealth, _stats.maxHealth);
            OnCoreEnergyChanged?.Invoke(_stats.crystalCore.CurrentEnergy, _stats.crystalCore.CurrentEnergyCapacity);
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
            _stats.crystalCore.FullRepair();
            OnCoreEnergyChanged?.Invoke(_stats.crystalCore.CurrentEnergy, _stats.crystalCore.CurrentEnergyCapacity);
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
        /// 执行基于鼠标的射击
        /// </summary>
        /// <param name="shootOrigin">射击起始位置</param>
        /// <returns>射击结果</returns>
        public ShootingResult PerformShoot(Vector3 shootOrigin)
        {
            if (!CanShoot())
            {
                return new ShootingResult { success = false };
            }

            // 消耗弹药
            if (!_weaponManager.ConsumeAmmo())
            {
                Debug.LogWarning("PlayerController: Failed to consume ammo");
                return new ShootingResult { success = false };
            }

            _lastAttackTime = Time.time;
            
            GunDataAsset currentGun = _weaponManager.CurrentGun;
            
            // 执行基于鼠标的两阶段射击
            ShootingResult result = new ShootingResult { success = false };
            if (_shootingSystem != null)
            {
                result = _shootingSystem.PerformMouseBasedShoot(shootOrigin, currentGun);
                
                // Core energy gain: 10 health damage = 2 core energy gain
                if (result.success && result.hasHit && result.actualDamage > 0)
                {
                    float coreGain = result.actualDamage * 0.2f; // 10 damage = 2 gain
                    GainCoreEnergy(coreGain);
                    Debug.Log($"PlayerController: Gained {coreGain} core energy from dealing {result.actualDamage} actual damage (base: {result.damage})");
                }
            }
            
            // 触发射击事件
            OnShoot?.Invoke();
            
            Debug.Log($"PlayerController: Mouse-based shot fired with {currentGun.weaponName}. " +
                     $"Target: {result.mouseTargetPoint}, Hit: {result.hasHit}, " +
                     $"Damage: {currentGun.damage}, Remaining ammo: {currentGun.CurrentAmmo}");
            
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
            OnCoreEnergyChanged?.Invoke(_stats.crystalCore.CurrentEnergy, _stats.crystalCore.CurrentEnergyCapacity);
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
        /// Register a new action with the ActionController
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
        /// Register all available player actions with the ActionController
        /// </summary>
        private void RegisterPlayerActions()
        {
            // Register core actions
            RegisterAction(new PlayerWaveAction());
            RegisterAction(new PlayerHealAction());
            RegisterAction(new PlayerInteractAction());
            RegisterAction(new PlayerReloadAction());

            Debug.Log("PlayerController: Registered player actions (Wave, Heal, Interact, Reload)");
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
                OnCoreEnergyChanged?.Invoke(_stats.crystalCore.CurrentEnergy, _stats.crystalCore.CurrentEnergyCapacity);
                
                if (oldCoreTier != _stats.crystalCore.EnergyTier)
                {
                    OnCoreTierChanged?.Invoke(_stats.crystalCore.EnergyTier);
                    Debug.Log($"PlayerController: Core tier changed from {oldCoreTier} to {_stats.crystalCore.EnergyTier} after slot consumption");
                }
                
                Debug.Log($"PlayerController: Consumed 1 slot ({_stats.crystalCore.EnergyPerSlot} core energy)." +
                $"Remaining: {_stats.crystalCore.CurrentEnergy}/{_stats.crystalCore.CurrentEnergyCapacity} ({_stats.crystalCore.GetEnergyInSlots():F1} slots)");
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
