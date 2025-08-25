using UnityEngine;
using System.Collections.Generic;
using Resonance.Player.Data;
using Resonance.Player.States;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Items;
using Resonance.Interfaces.Services;
using Resonance.Interfaces.Objects;
using Resonance.Player.Actions;

namespace Resonance.Player.Core
{
    /// <summary>
    /// Core player controller that manages player state and behavior.
    /// This is a Non-MonoBehaviour class that handles the player logic.
    /// </summary>
    public class PlayerController
    {
        // Core Data
        private PlayerRuntimeStats _stats;
        private PlayerInventory _inventory;
        private PlayerMovement _movement;
        private WeaponManager _weaponManager;
        private ShootingSystem _shootingSystem;

        // Player State Management
        private PlayerStateMachine _stateMachine;
        private ActionController _actionController;

        // Services
        private IAudioService _audioService;
        private GameObject _playerGameObject; // For 3D audio positioning

        // Progression
        private int _level = 1;
        private float _experience = 0f;
        private List<string> _unlockedAbilities;
        private Dictionary<string, bool> _levelFlags;
        private Dictionary<string, float> _gameVariables;

        // Combat State
        private bool _isInvulnerable = false;
        private float _invulnerabilityTimer = 0f;
        private float _lastAttackTime = 0f;

        // Dual Health Events
        public System.Action<float, float> OnPhysicalHealthChanged; // current, max
        public System.Action<float, float> OnMentalHealthChanged; // current, max
        public System.Action OnPhysicalDeath; // Physical health reaches 0
        public System.Action OnTrueDeath; // Mental health reaches 0
        
        // Health Tier Events
        public System.Action<MentalHealthTier> OnMentalTierChanged;
        public System.Action<PhysicalHealthTier> OnPhysicalTierChanged;
        
        // Other Events
        public System.Action<int> OnLevelChanged;
        public System.Action<float> OnExperienceChanged;
        public System.Action<string> OnStateChanged; // Changed to string for state name
        public System.Action OnShoot;

        // Properties
        public PlayerRuntimeStats Stats => _stats;
        public PlayerInventory Inventory => _inventory;
        public PlayerMovement Movement => _movement;
        public WeaponManager WeaponManager => _weaponManager;
        public ShootingSystem ShootingSystem => _shootingSystem;
        public int Level => _level;
        public float Experience => _experience;
        public List<string> UnlockedAbilities => _unlockedAbilities;
        public Dictionary<string, bool> LevelFlags => _levelFlags;
        public Dictionary<string, float> GameVariables => _gameVariables;
        public bool IsInvulnerable => _isInvulnerable;
        
        // Dual Health Properties
        public bool IsPhysicallyAlive => _stats.IsPhysicallyAlive;
        public bool IsMentallyAlive => _stats.IsMentallyAlive;
        public bool IsInPhysicalDeathState => _stats.IsInPhysicalDeathState;
        
        // Health Tier Properties
        public MentalHealthTier MentalTier => _stats.mentalTier;
        public PhysicalHealthTier PhysicalTier => _stats.physicalTier;
        public float SlotValue => _stats.slotValue;
        public float MentalHealthInSlots => _stats.GetMentalHealthInSlots();
        public bool CanConsumeSlot => _stats.CanConsumeSlot();
        
        public string CurrentState => _stateMachine?.CurrentStateName ?? "None";
        public bool IsAiming => CurrentState == "Aiming";
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
            _inventory = new PlayerInventory(_stats.maxInventorySlots, _stats.maxCarryWeight);
            _movement = new PlayerMovement(_stats);
            _weaponManager = new WeaponManager();

            _unlockedAbilities = new List<string>();
            _levelFlags = new Dictionary<string, bool>();
            _gameVariables = new Dictionary<string, float>();

            // Initialize state machine
            _stateMachine = new PlayerStateMachine(this);
            _stateMachine.OnStateChanged += (stateName) => OnStateChanged?.Invoke(stateName);
            _stateMachine.Initialize();

            // Initialize action controller
            _actionController = new ActionController(this);
            _actionController.Initialize();
            
            // Register available actions
            RegisterPlayerActions();

            Debug.Log("PlayerController: Initialized with base stats, weapon manager, state machine, and action controller");
        }

        /// <summary>
        /// Update player controller (called from MonoBehaviour)
        /// </summary>
        public void Update(float deltaTime)
        {
            UpdateInvulnerability(deltaTime);
            UpdateHealthRegeneration(deltaTime);
            _movement.Update(deltaTime);
            _stateMachine?.Update();
            _actionController?.Update(deltaTime);
        }

        #region Health System

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

        private void UpdateHealthRegeneration(float deltaTime)
        {
            bool healthChanged = false;
            
            // Physical health regeneration (only when physically alive)
            if (_stats.physicalHealthRegenRate > 0f && _stats.currentPhysicalHealth < _stats.maxPhysicalHealth && IsPhysicallyAlive)
            {
                _stats.currentPhysicalHealth = Mathf.Min(_stats.maxPhysicalHealth, 
                    _stats.currentPhysicalHealth + _stats.physicalHealthRegenRate * deltaTime);
                OnPhysicalHealthChanged?.Invoke(_stats.currentPhysicalHealth, _stats.maxPhysicalHealth);
                healthChanged = true;
            }
            
            // Mental health regeneration (only in normal state) or decay (in core state)
            if (IsInPhysicalDeathState)
            {
                // Mental health decays when in physical death state (core mode)
                if (_stats.mentalHealthDecayRate > 0f && _stats.currentMentalHealth > 0f)
                {
                    _stats.currentMentalHealth = Mathf.Max(0f, _stats.currentMentalHealth - _stats.mentalHealthDecayRate * deltaTime);
                    OnMentalHealthChanged?.Invoke(_stats.currentMentalHealth, _stats.maxMentalHealth);
                    healthChanged = true;
                    
                    // Check for true death
                    if (_stats.currentMentalHealth <= 0f)
                    {
                        HandleTrueDeath();
                    }
                }
            }
            else if (_stats.mentalHealthRegenRate > 0f && _stats.currentMentalHealth < _stats.maxMentalHealth)
            {
                // Mental health regenerates in normal state
                _stats.currentMentalHealth = Mathf.Min(_stats.maxMentalHealth, 
                    _stats.currentMentalHealth + _stats.mentalHealthRegenRate * deltaTime);
                OnMentalHealthChanged?.Invoke(_stats.currentMentalHealth, _stats.maxMentalHealth);
                healthChanged = true;
            }
        }

        /// <summary>
        /// Take physical damage (affects physical health)
        /// </summary>
        public void TakePhysicalDamage(float damage)
        {
            if (_isInvulnerable || !IsMentallyAlive) return;

            // Check Action system invulnerability
            if (_actionController?.IsInvulnerable == true) return;

            // Store old tier for comparison
            var oldPhysicalTier = _stats.physicalTier;

            _stats.currentPhysicalHealth = Mathf.Max(0f, _stats.currentPhysicalHealth - damage);
            
            // Update tiers after health change
            _stats.UpdateHealthTiers();
            
            // Fire tier change event if tier changed
            if (oldPhysicalTier != _stats.physicalTier)
            {
                OnPhysicalTierChanged?.Invoke(_stats.physicalTier);
                Debug.Log($"PlayerController: Physical tier changed from {oldPhysicalTier} to {_stats.physicalTier}");
            }

            OnPhysicalHealthChanged?.Invoke(_stats.currentPhysicalHealth, _stats.maxPhysicalHealth);

            // Notify ActionController of damage taken (for interruption logic)
            _actionController?.OnPlayerDamageTaken();

            // Play hit audio effect
            PlayHitAudio();

            if (_stats.currentPhysicalHealth <= 0f)
            {
                HandlePhysicalDeath();
            }
            else
            {
                // Start invulnerability period for physical damage
                _isInvulnerable = true;
                _invulnerabilityTimer = _stats.invulnerabilityTime;
                Debug.Log($"PlayerController: Took {damage} physical damage, physical health: {_stats.currentPhysicalHealth}");
            }
        }

        /// <summary>
        /// Take mental damage (affects mental health)
        /// </summary>
        public void TakeMentalDamage(float damage)
        {
            if (!IsMentallyAlive) return;

            // Store old tier for comparison
            var oldMentalTier = _stats.mentalTier;

            _stats.currentMentalHealth = Mathf.Max(0f, _stats.currentMentalHealth - damage);
            
            // Update tiers after health change
            _stats.UpdateHealthTiers();
            
            // Fire tier change event if tier changed
            if (oldMentalTier != _stats.mentalTier)
            {
                OnMentalTierChanged?.Invoke(_stats.mentalTier);
                Debug.Log($"PlayerController: Mental tier changed from {oldMentalTier} to {_stats.mentalTier}");
            }

            OnMentalHealthChanged?.Invoke(_stats.currentMentalHealth, _stats.maxMentalHealth);

            if (_stats.currentMentalHealth <= 0f && _stats.currentPhysicalHealth <= 0f)
            {
                HandleTrueDeath();
            }
            else
            {
                Debug.Log($"PlayerController: Took {damage} mental damage, mental health: {_stats.currentMentalHealth}");
            }
        }

        /// <summary>
        /// Heal physical health
        /// </summary>
        public void HealPhysical(float amount)
        {
            if (!IsMentallyAlive) return;

            // Store old tier for comparison
            var oldPhysicalTier = _stats.physicalTier;

            _stats.currentPhysicalHealth = Mathf.Min(_stats.maxPhysicalHealth, _stats.currentPhysicalHealth + amount);
            
            // Update tiers after health change
            _stats.UpdateHealthTiers();
            
            // Fire tier change event if tier changed
            if (oldPhysicalTier != _stats.physicalTier)
            {
                OnPhysicalTierChanged?.Invoke(_stats.physicalTier);
                Debug.Log($"PlayerController: Physical tier changed from {oldPhysicalTier} to {_stats.physicalTier}");
            }

            OnPhysicalHealthChanged?.Invoke(_stats.currentPhysicalHealth, _stats.maxPhysicalHealth);
            Debug.Log($"PlayerController: Healed {amount} physical health, current: {_stats.currentPhysicalHealth}");
        }

        /// <summary>
        /// Heal mental health
        /// </summary>
        public void HealMental(float amount)
        {
            if (!IsMentallyAlive) return;

            // Store old tier for comparison
            var oldMentalTier = _stats.mentalTier;

            _stats.currentMentalHealth = Mathf.Min(_stats.maxMentalHealth, _stats.currentMentalHealth + amount);
            
            // Update tiers after health change
            _stats.UpdateHealthTiers();
            
            // Fire tier change event if tier changed
            if (oldMentalTier != _stats.mentalTier)
            {
                OnMentalTierChanged?.Invoke(_stats.mentalTier);
                Debug.Log($"PlayerController: Mental tier changed from {oldMentalTier} to {_stats.mentalTier}");
            }

            OnMentalHealthChanged?.Invoke(_stats.currentMentalHealth, _stats.maxMentalHealth);
            Debug.Log($"PlayerController: Healed {amount} mental health, current: {_stats.currentMentalHealth}");
        }

        /// <summary>
        /// Handle physical death (physical health reaches 0)
        /// </summary>
        private void HandlePhysicalDeath()
        {
            // Prevent multiple calls - only trigger if not already in death states
            if (_stateMachine?.IsPhysicallyDead() == true || _stateMachine?.IsMentallyDead() == true)
            {
                return;
            }
            
            Debug.Log("PlayerController: Physical death - entering core mode");
            OnPhysicalDeath?.Invoke();
            _stateMachine?.EnterPhysicalDeath();
        }

        /// <summary>
        /// Handle true death (mental health reaches 0)
        /// </summary>
        private void HandleTrueDeath()
        {
            Debug.Log("PlayerController: True death - game over");
            OnTrueDeath?.Invoke();
            _stateMachine?.EnterTrueDeath();
        }

        /// <summary>
        /// Restore all health to full
        /// </summary>
        public void RestoreToFullHealth()
        {
            _stats.RestoreToFullHealth();
            OnPhysicalHealthChanged?.Invoke(_stats.currentPhysicalHealth, _stats.maxPhysicalHealth);
            OnMentalHealthChanged?.Invoke(_stats.currentMentalHealth, _stats.maxMentalHealth);
            Debug.Log("PlayerController: All health restored to full");
        }

        /// <summary>
        /// Restore only physical health
        /// </summary>
        public void RestorePhysicalHealth()
        {
            _stats.RestorePhysicalHealth();
            OnPhysicalHealthChanged?.Invoke(_stats.currentPhysicalHealth, _stats.maxPhysicalHealth);
            Debug.Log("PlayerController: Physical health restored to full");
        }

        /// <summary>
        /// Restore only mental health
        /// </summary>
        public void RestoreMentalHealth()
        {
            _stats.RestoreMentalHealth();
            OnMentalHealthChanged?.Invoke(_stats.currentMentalHealth, _stats.maxMentalHealth);
            Debug.Log("PlayerController: Mental health restored to full");
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

        public void StartInteracting()
        {
            _stateMachine?.StartInteracting();
        }

        public void StopInteracting()
        {
            _stateMachine?.StopInteracting();
        }



        #endregion

        #region Combat System

        public bool CanShoot()
        {
            return IsPhysicallyAlive && 
                   _stateMachine.CanShoot() && 
                   Time.time >= _lastAttackTime + _stats.attackCooldown &&
                   !(_actionController?.IsBlocking ?? false); // Actions can block shooting
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
                
                // Mental health recovery: 10 physical damage = 2 mental health recovery
                if (result.success && result.hasHit && result.damage > 0)
                {
                    float mentalRecovery = result.damage * 0.2f; // 10 damage = 2 recovery
                    HealMental(mentalRecovery);
                    Debug.Log($"PlayerController: Recovered {mentalRecovery} mental health from dealing {result.damage} damage");
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
            // Load stats
            _stats = saveData.stats;

            // Load progression
            _level = saveData.playerLevel;
            _experience = saveData.experience;
            _unlockedAbilities = new List<string>(saveData.unlockedAbilities);
            _levelFlags = new Dictionary<string, bool>(saveData.levelFlags);
            _gameVariables = new Dictionary<string, float>(saveData.gameVariables);

            // Load inventory
            _inventory.LoadFromSaveData(saveData.inventory, saveData.equippedItemIDs);

            Debug.Log($"PlayerController: Loaded save data from {saveData.saveID}");

            // Notify UI of dual health changes
            OnPhysicalHealthChanged?.Invoke(_stats.currentPhysicalHealth, _stats.maxPhysicalHealth);
            OnMentalHealthChanged?.Invoke(_stats.currentMentalHealth, _stats.maxMentalHealth);
            
            // Notify UI of other changes
            OnLevelChanged?.Invoke(_level);
            OnExperienceChanged?.Invoke(_experience);
        }

        public PlayerSaveData CreateSaveData(string savePointID, Vector3 position, Vector3 rotation)
        {
            var saveData = new PlayerSaveData
            {
                saveID = savePointID,
                sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                savePosition = position,
                saveRotation = rotation,
                stats = _stats,
                playerLevel = _level,
                experience = _experience,
                unlockedAbilities = new List<string>(_unlockedAbilities),
                levelFlags = new Dictionary<string, bool>(_levelFlags),
                gameVariables = new Dictionary<string, float>(_gameVariables)
            };

            // Save inventory
            saveData.inventory = _inventory.GetSaveData();
            saveData.equippedItemIDs = _inventory.GetEquippedItemIDs();

            return saveData;
        }

        #endregion

        #region Flags and Variables

        public void SetLevelFlag(string flagName, bool value)
        {
            _levelFlags[flagName] = value;
        }

        public bool GetLevelFlag(string flagName)
        {
            return _levelFlags.TryGetValue(flagName, out bool value) && value;
        }

        public void SetGameVariable(string varName, float value)
        {
            _gameVariables[varName] = value;
        }

        public float GetGameVariable(string varName)
        {
            return _gameVariables.TryGetValue(varName, out float value) ? value : 0f;
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

        #endregion

        #region Mental Health Slot Management
        
        /// <summary>
        /// Consume one mental health slot for actions
        /// </summary>
        /// <returns>True if successful, false if insufficient mental health</returns>
        public bool ConsumeSlot()
        {
            var oldMentalTier = _stats.mentalTier;
            bool success = _stats.ConsumeSlot();
            
            if (success)
            {
                // Fire events
                OnMentalHealthChanged?.Invoke(_stats.currentMentalHealth, _stats.maxMentalHealth);
                
                if (oldMentalTier != _stats.mentalTier)
                {
                    OnMentalTierChanged?.Invoke(_stats.mentalTier);
                    Debug.Log($"PlayerController: Mental tier changed from {oldMentalTier} to {_stats.mentalTier} after slot consumption");
                }
                
                Debug.Log($"PlayerController: Consumed 1 slot ({_stats.slotValue} mental health). Remaining: {_stats.currentMentalHealth}/{_stats.maxMentalHealth} ({_stats.GetMentalHealthInSlots():F1} slots)");
            }
            
            return success;
        }

        #endregion

        #region Action Registration

        /// <summary>
        /// Register all available player actions with the ActionController
        /// </summary>
        private void RegisterPlayerActions()
        {
            // Register core actions
            RegisterAction(new PlayerResonanceAction());
            RegisterAction(new PlayerRecoverAction());
            RegisterAction(new PlayerInteractAction());

            Debug.Log("PlayerController: Registered player actions (Resonance, Recover, Interact)");
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

            // Clear events
            OnPhysicalHealthChanged = null;
            OnMentalHealthChanged = null;
            OnPhysicalDeath = null;
            OnTrueDeath = null;
            OnMentalTierChanged = null;
            OnPhysicalTierChanged = null;
            OnLevelChanged = null;
            OnExperienceChanged = null;
            OnStateChanged = null;
            OnShoot = null;

            Debug.Log("PlayerController: Cleaned up");
        }

        #endregion
    }
}
