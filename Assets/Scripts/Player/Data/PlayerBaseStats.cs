using UnityEngine;
using System.Collections.Generic;
using Resonance.Utilities;
using Resonance.Utilities.Types;
using Resonance.Utilities.CrystalCore;

namespace Resonance.Player.Data
{
    /// <summary>
    /// Player base stats configuration
    /// Defines the base attribute data for the player character
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerBaseStats", menuName = "Resonance/Player/Base Stats")]
    public class PlayerBaseStats : ScriptableObject
    {
        [Header("Survival Attributes")]
        [Tooltip("Maximum health")]
        [SerializeField] private float _maxHealth = 100f;
        [Tooltip("Invulnerability time")]
        [SerializeField] private float _invulnerabilityTime = 1f;

        [Header("Crystal Core Attributes")]
        [Tooltip("Crystal core configuration")]
        [SerializeField] private CrystalCoreConfig _crystalCoreConfig;
        [Tooltip("Health restore value per crystal core energy")]
        [SerializeField] private float _healthRestoreValue = 30f;
        [Tooltip("Physical damage to crystal core energy ratio")]
        [SerializeField] private float _physicalDamageToCoreEnergyRatio = 0.4f;

        [Header("Wave Attack Attributes")]
        [Tooltip("Wave attack damages")]
        [SerializeField] private Damages _waveAttackDamages;

        [Header("Movement Attributes")]
        [Tooltip("Walk speed")]
        [SerializeField] private float _walkSpeed = 3f;
        [Tooltip("Run speed")]
        [SerializeField] private float _runSpeed = 4.5f;
        [Tooltip("Aim move speed")]
        [SerializeField] private float _aimMoveSpeed = 1.5f;
        [Tooltip("Reload move speed")]
        [SerializeField] private float _reloadMoveSpeed = 2.5f;

        [Header("Equipment Attributes")]
        [Tooltip("Inventory grid width")]
        [SerializeField] private int _inventoryGridWidth = 3;
        [Tooltip("Inventory grid height")]
        [SerializeField] private int _inventoryGridHeight = 3;
        [Tooltip("Module slots")]
        [SerializeField] private int _moduleSlots = 2;

        [Header("Interaction Attributes")]
        [Tooltip("Interaction range")]
        [SerializeField] private float _interactionRange = 1.5f;
        [Tooltip("Interaction layer")]
        [SerializeField] private LayerMask _interactionLayerMask = LayerDict.GetLayer("Interactable");
        [Tooltip("Wave interaction layer")]
        [SerializeField] private LayerMask _waveInteractionLayerMask = LayerDict.GetLayer("Enemy");

        [Header("Visual Effects")]
        [Tooltip("Normal state material path")]
        [SerializeField] private string _normalMaterialPath = "Art/Materials/Player/Player_Body";
        [Tooltip("Damage state material path")]
        [SerializeField] private string _damageMaterialPath = "Art/Materials/Damage_Body";
        [Tooltip("Damage flash duration")]
        [SerializeField] private float _damageFlashDuration = 0.2f;

        // Survival attributes accessors
        public float MaxHealth => _maxHealth;
        public float InvulnerabilityTime => _invulnerabilityTime;

        // Crystal core attributes accessors
        public CrystalCoreConfig CrystalCoreConfig => _crystalCoreConfig;
        public float HealthRestoreValue => _healthRestoreValue;
        public float PhysicalDamageToCoreEnergyRatio => _physicalDamageToCoreEnergyRatio;

        // Wave attack attributes accessors
        public Damages WaveAttackDamages => _waveAttackDamages;

        // Movement attributes accessors
        public float WalkSpeed => _walkSpeed;
        public float RunSpeed => _runSpeed;
        public float AimMoveSpeed => _aimMoveSpeed;
        public float ReloadMoveSpeed => _reloadMoveSpeed;

        // Equipment attributes accessors
        public int InventoryGridWidth => _inventoryGridWidth;
        public int InventoryGridHeight => _inventoryGridHeight;
        public int ModuleSlots => _moduleSlots;

        // Interaction attributes accessors
        public float InteractionRange => _interactionRange;
        public LayerMask InteractionLayerMask => _interactionLayerMask;
        public LayerMask WaveInteractionLayerMask => _waveInteractionLayerMask;

        // Visual effects accessors
        public string NormalMaterialPath => _normalMaterialPath;
        public string DamageMaterialPath => _damageMaterialPath;
        public float DamageFlashDuration => _damageFlashDuration;

        /// <summary>
        /// Create runtime stats instance
        /// </summary>
        public PlayerRuntimeStats CreateRuntimeStats()
        {
            return new PlayerRuntimeStats(this);
        }

        /// <summary>
        /// Validate config data
        /// </summary>
        public bool ValidateConfig()
        {
            if (_maxHealth <= 0f)
            {
                Debug.LogError($"PlayerBaseStats: {name} has invalid maxHealth: {_maxHealth}");
                return false;
            }

            if (_crystalCoreConfig == null)
            {
                Debug.LogError($"PlayerBaseStats: {name} has no crystal core config assigned");
                return false;
            }

            if (!_crystalCoreConfig.ValidateConfig())
            {
                Debug.LogError($"PlayerBaseStats: {name} has invalid crystal core config");
                return false;
            }

            return true;
        }

        #region Unity Editor

        void OnValidate()
        {
            // Ensure values are within reasonable ranges
            _maxHealth = Mathf.Max(1f, _maxHealth);
            _invulnerabilityTime = Mathf.Max(0f, _invulnerabilityTime);
            _healthRestoreValue = Mathf.Max(0f, _healthRestoreValue);

            // Validate movement speeds
            _walkSpeed = Mathf.Max(0.1f, _walkSpeed);
            _runSpeed = Mathf.Max(_walkSpeed, _runSpeed);
            _aimMoveSpeed = Mathf.Max(0.1f, _aimMoveSpeed);
            _reloadMoveSpeed = Mathf.Max(0.1f, _reloadMoveSpeed);

            // Validate inventory grid
            _inventoryGridWidth = Mathf.Max(1, _inventoryGridWidth);
            _inventoryGridHeight = Mathf.Max(1, _inventoryGridHeight);
            _moduleSlots = Mathf.Max(0, _moduleSlots);

            // Validate interaction range
            _interactionRange = Mathf.Max(0.1f, _interactionRange);
        }

        #endregion
    }
}
