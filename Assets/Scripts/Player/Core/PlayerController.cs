using UnityEngine;
using System.Collections.Generic;
using Resonance.Player.Data;
using Resonance.Player.States;
using Resonance.Core;
using Resonance.Utilities;

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

        // Player State Management
        private PlayerStateMachine _stateMachine;
        private Vector2 _aimDirection = Vector2.right;

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

        // Events
        public System.Action<float> OnHealthChanged;
        public System.Action OnPlayerDied;
        public System.Action<int> OnLevelChanged;
        public System.Action<float> OnExperienceChanged;
        public System.Action<string> OnStateChanged; // Changed to string for state name
        public System.Action<Vector2> OnAimDirectionChanged;
        public System.Action OnShoot;

        // Properties
        public PlayerRuntimeStats Stats => _stats;
        public PlayerInventory Inventory => _inventory;
        public PlayerMovement Movement => _movement;
        public int Level => _level;
        public float Experience => _experience;
        public List<string> UnlockedAbilities => _unlockedAbilities;
        public Dictionary<string, bool> LevelFlags => _levelFlags;
        public Dictionary<string, float> GameVariables => _gameVariables;
        public bool IsInvulnerable => _isInvulnerable;
        public bool IsAlive => _stats.IsAlive;
        public string CurrentState => _stateMachine?.CurrentStateName ?? "None";
        public Vector2 AimDirection => _aimDirection;
        public bool IsAiming => CurrentState == "Aiming";
        public PlayerStateMachine StateMachine => _stateMachine;

        public PlayerController(PlayerBaseStats baseStats)
        {
            Initialize(baseStats);
        }

        private void Initialize(PlayerBaseStats baseStats)
        {
            _stats = baseStats.CreateRuntimeStats();
            _inventory = new PlayerInventory(_stats.maxInventorySlots, _stats.maxCarryWeight);
            _movement = new PlayerMovement(_stats);

            _unlockedAbilities = new List<string>();
            _levelFlags = new Dictionary<string, bool>();
            _gameVariables = new Dictionary<string, float>();

            // Initialize state machine
            _stateMachine = new PlayerStateMachine(this);
            _stateMachine.OnStateChanged += (stateName) => OnStateChanged?.Invoke(stateName);
            _stateMachine.Initialize();

            Debug.Log("PlayerController: Initialized with base stats and state machine");
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
            if (_stats.healthRegenRate > 0f && _stats.currentHealth < _stats.maxHealth)
            {
                _stats.currentHealth = Mathf.Min(_stats.maxHealth, 
                    _stats.currentHealth + _stats.healthRegenRate * deltaTime);
                OnHealthChanged?.Invoke(_stats.currentHealth);
            }
        }

        public void TakeDamage(float damage)
        {
            if (_isInvulnerable || !IsAlive) return;

            _stats.currentHealth = Mathf.Max(0f, _stats.currentHealth - damage);
            OnHealthChanged?.Invoke(_stats.currentHealth);

            if (_stats.currentHealth <= 0f)
            {
                _stateMachine?.Die();
                OnPlayerDied?.Invoke();
                Debug.Log("PlayerController: Player died");
            }
            else
            {
                // Start invulnerability period
                _isInvulnerable = true;
                _invulnerabilityTimer = _stats.invulnerabilityTime;
                Debug.Log($"PlayerController: Took {damage} damage, health: {_stats.currentHealth}");
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;

            _stats.currentHealth = Mathf.Min(_stats.maxHealth, _stats.currentHealth + amount);
            OnHealthChanged?.Invoke(_stats.currentHealth);
            Debug.Log($"PlayerController: Healed {amount}, health: {_stats.currentHealth}");
        }

        public void RestoreToFullHealth()
        {
            _stats.RestoreToFullHealth();
            OnHealthChanged?.Invoke(_stats.currentHealth);
            Debug.Log("PlayerController: Health restored to full");
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

        public void SetAimDirection(Vector2 direction)
        {
            if (!IsAiming) return;

            _aimDirection = direction.normalized;
            OnAimDirectionChanged?.Invoke(_aimDirection);
        }

        #endregion

        #region Combat System

        public bool CanShoot()
        {
            return IsAlive && _stateMachine.CanShoot() && Time.time >= _lastAttackTime + _stats.attackCooldown;
        }

        public void PerformShoot()
        {
            if (!CanShoot()) return;

            _lastAttackTime = Time.time;
            OnShoot?.Invoke();
            Debug.Log($"PlayerController: Shot fired in direction {_aimDirection} with {_stats.attackDamage} damage");
            
            // Shooting logic would be implemented here
            // This could involve spawning bullets, raycasting, etc.
        }

        #endregion

        #region Progression System

        public void AddExperience(float amount)
        {
            _experience += amount;
            OnExperienceChanged?.Invoke(_experience);

            // Simple level up calculation (can be made more complex)
            int newLevel = Mathf.FloorToInt(_experience / 100f) + 1;
            if (newLevel > _level)
            {
                LevelUp(newLevel);
            }
        }

        private void LevelUp(int newLevel)
        {
            _level = newLevel;
            OnLevelChanged?.Invoke(_level);

            // Restore health on level up
            RestoreToFullHealth();

            Debug.Log($"PlayerController: Level up! New level: {_level}");
        }

        public void UnlockAbility(string abilityName)
        {
            if (!_unlockedAbilities.Contains(abilityName))
            {
                _unlockedAbilities.Add(abilityName);
                Debug.Log($"PlayerController: Unlocked ability: {abilityName}");
            }
        }

        public bool HasAbility(string abilityName)
        {
            return _unlockedAbilities.Contains(abilityName);
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

            // Notify UI of changes
            OnHealthChanged?.Invoke(_stats.currentHealth);
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
    }
}
