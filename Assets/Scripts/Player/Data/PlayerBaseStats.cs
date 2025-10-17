using UnityEngine;
using System.Collections.Generic;
using Resonance.Utilities;
using Resonance.Utilities.CrystalCore;

namespace Resonance.Player.Data
{
    /// <summary>
    /// 玩家基础属性配置
    /// 定义玩家角色的基准属性数据
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerBaseStats", menuName = "Resonance/Player/Base Stats")]
    public class PlayerBaseStats : ScriptableObject
    {
        [Header("Survival Attributes")]
        [Tooltip("最大生命值")]
        [SerializeField] private float _maxHealth = 100f;
        [Tooltip("无敌时间")]
        [SerializeField] private float _invulnerabilityTime = 1f;

        [Header("Crystal Core Attributes")]
        [Tooltip("晶核配置")]
        [SerializeField] private CrystalCoreConfig _crystalCoreConfig;
        [Tooltip("每格晶核能量恢复的生命值")]
        [SerializeField] private float _healthRestoreValue = 30f;
        [Tooltip("物理伤害转化为晶核能量的比例")]
        [SerializeField] private float _physicalDamageToCoreEnergyRatio = 0.4f;

        [Header("Movement Attributes")]
        [Tooltip("行走速度")]
        [SerializeField] private float _walkSpeed = 3f;
        [Tooltip("奔跑速度")]
        [SerializeField] private float _runSpeed = 5f;
        [Tooltip("瞄准移动速度")]
        [SerializeField] private float _aimMoveSpeed = 1.5f;
        [Tooltip("换弹移动速度")]
        [SerializeField] private float _reloadMoveSpeed = 2.5f;

        [Header("Equipment Attributes")]
        [Tooltip("背包初始格子数(宽)")]
        [SerializeField] private int _inventoryGridWidth = 3;
        [Tooltip("背包初始格子数(高)")]
        [SerializeField] private int _inventoryGridHeight = 3;
        [Tooltip("模块槽位数量")]
        [SerializeField] private int _moduleSlots = 2;

        [Header("Interaction Attributes")]
        [Tooltip("交互范围")]
        [SerializeField] private float _interactionRange = 1.5f;
        [Tooltip("交互层级")]
        [SerializeField] private LayerMask _interactionLayerMask = 1 << 7; // Layer 7 (Interactable)
        [Tooltip("晶核交互层级")]
        [SerializeField] private LayerMask _coreInteractionLayerMask = 1 << 8; // Layer 8 (Core Interactable)

        [Header("Visual Effects")]
        [Tooltip("正常状态材质路径")]
        [SerializeField] private string _normalMaterialPath = "Art/Materials/Player/Player_Body";
        [Tooltip("受伤状态材质路径")]
        [SerializeField] private string _damageMaterialPath = "Art/Materials/Damage_Body";
        [Tooltip("受伤闪烁持续时间")]
        [SerializeField] private float _damageFlashDuration = 0.2f;

        // Survival attributes accessors
        public float MaxHealth => _maxHealth;
        public float InvulnerabilityTime => _invulnerabilityTime;

        // Crystal core attributes accessors
        public CrystalCoreConfig CrystalCoreConfig => _crystalCoreConfig;
        public float HealthRestoreValue => _healthRestoreValue;
        public float PhysicalDamageToCoreEnergyRatio => _physicalDamageToCoreEnergyRatio;

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
        public LayerMask CoreInteractionLayerMask => _coreInteractionLayerMask;

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
            // 确保数值在合理范围内
            _maxHealth = Mathf.Max(1f, _maxHealth);
            _invulnerabilityTime = Mathf.Max(0f, _invulnerabilityTime);
            _healthRestoreValue = Mathf.Max(0f, _healthRestoreValue);

            // 移动速度验证
            _walkSpeed = Mathf.Max(0.1f, _walkSpeed);
            _runSpeed = Mathf.Max(_walkSpeed, _runSpeed);
            _aimMoveSpeed = Mathf.Max(0.1f, _aimMoveSpeed);
            _reloadMoveSpeed = Mathf.Max(0.1f, _reloadMoveSpeed);

            // 背包格子验证
            _inventoryGridWidth = Mathf.Max(1, _inventoryGridWidth);
            _inventoryGridHeight = Mathf.Max(1, _inventoryGridHeight);
            _moduleSlots = Mathf.Max(0, _moduleSlots);

            // 交互范围验证
            _interactionRange = Mathf.Max(0.1f, _interactionRange);
        }

        #endregion
    }
}
