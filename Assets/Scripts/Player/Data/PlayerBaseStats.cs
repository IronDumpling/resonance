using UnityEngine;
using System.Collections.Generic;
using Resonance.Core.Data;

namespace Resonance.Player.Data
{
    /// <summary>
    /// 玩家基础属性配置
    /// 定义玩家角色的基准属性数据
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerBaseStats", menuName = "Resonance/Player/Base Stats")]
    public class PlayerBaseStats : ScriptableObject
    {
        [Header("生存属性 - Survival Attributes")]
        [Tooltip("最大生命值")]
        [SerializeField] private float _maxHealth = 100f;
        [Tooltip("最大韧性值")]
        [SerializeField] private float _maxResilience = 50f;
        [Tooltip("眩晕阈值")]
        [SerializeField] private float _stunThreshold = 10f;
        [Tooltip("无敌时间")]
        [SerializeField] private float _invulnerabilityTime = 1f;

        [Header("晶核属性 - Crystal Core Attributes")]
        [Tooltip("晶核配置")]
        [SerializeField] private CrystalCoreConfig _crystalCoreConfig;
        [Tooltip("每格晶核能量恢复的生命值")]
        [SerializeField] private float _healthRestoreValue = 30f;

        [Header("移动属性 - Movement Attributes")]
        [Tooltip("行走速度")]
        [SerializeField] private float _walkSpeed = 3f;
        [Tooltip("奔跑速度")]
        [SerializeField] private float _runSpeed = 4f;
        [Tooltip("瞄准移动速度")]
        [SerializeField] private float _aimMoveSpeed = 1f;
        [Tooltip("换弹移动速度")]
        [SerializeField] private float _reloadMoveSpeed = 2f;

        [Header("装备属性 - Equipment Attributes")]
        [Tooltip("背包初始格子数（宽）")]
        [SerializeField] private int _inventoryGridWidth = 5;
        [Tooltip("背包初始格子数（高）")]
        [SerializeField] private int _inventoryGridHeight = 5;
        [Tooltip("模块槽位数量")]
        [SerializeField] private int _moduleSlots = 3;

        [Header("交互属性 - Interaction Attributes")]
        [Tooltip("交互范围")]
        [SerializeField] private float _interactionRange = 1.5f;
        [Tooltip("交互层级")]
        [SerializeField] private LayerMask _interactionLayerMask = 1 << 7; // Layer 7 (Interactable)
        [Tooltip("晶核交互层级")]
        [SerializeField] private LayerMask _coreInteractionLayerMask = 1 << 8; // Layer 8 (Core Interactable)
        
        // 生存属性访问器
        public float MaxHealth => _maxHealth;
        public float MaxResilience => _maxResilience;
        public float StunThreshold => _stunThreshold;
        public float InvulnerabilityTime => _invulnerabilityTime;

        // 晶核属性访问器
        public CrystalCoreConfig CrystalCoreConfig => _crystalCoreConfig;
        public float HealthRestoreValue => _healthRestoreValue;

        // 移动属性访问器
        public float WalkSpeed => _walkSpeed;
        public float RunSpeed => _runSpeed;
        public float AimMoveSpeed => _aimMoveSpeed;
        public float ReloadMoveSpeed => _reloadMoveSpeed;

        // 装备属性访问器
        public int InventoryGridWidth => _inventoryGridWidth;
        public int InventoryGridHeight => _inventoryGridHeight;
        public int ModuleSlots => _moduleSlots;

        // 交互属性访问器
        public float InteractionRange => _interactionRange;
        public LayerMask InteractionLayerMask => _interactionLayerMask;
        public LayerMask CoreInteractionLayerMask => _coreInteractionLayerMask;

        /// <summary>
        /// 创建运行时属性实例
        /// </summary>
        /// <returns>可修改的运行时属性</returns>
        public PlayerRuntimeStats CreateRuntimeStats()
        {
            return new PlayerRuntimeStats(this);
        }

        /// <summary>
        /// 验证配置数据
        /// </summary>
        /// <returns>是否有效</returns>
        public bool ValidateConfig()
        {
            if (_maxHealth <= 0f)
            {
                Debug.LogError($"PlayerBaseStats: {name} has invalid maxHealth: {_maxHealth}");
                return false;
            }

            if (_maxResilience <= 0f)
            {
                Debug.LogError($"PlayerBaseStats: {name} has invalid maxResilience: {_maxResilience}");
                return false;
            }

            if (_stunThreshold < 0f || _stunThreshold >= _maxResilience)
            {
                Debug.LogError($"PlayerBaseStats: {name} has invalid stunThreshold: {_stunThreshold} (should be 0 <= stunThreshold < maxResilience)");
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
            _maxResilience = Mathf.Max(1f, _maxResilience);
            _stunThreshold = Mathf.Clamp(_stunThreshold, 0f, _maxResilience - 1f);
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

    /// <summary>
    /// 玩家运行时属性
    /// 游戏过程中可修改的实际属性值，会受到装备、增益等影响
    /// </summary>
    [System.Serializable]
    public class PlayerRuntimeStats
    {
        [Header("生存属性 - Survival Attributes")]
        public float currentHealth;
        public float maxHealth;
        public float currentResilience;
        public float maxResilience;
        public float stunThreshold;
        public float resilienceRegenRate;
        public float invulnerabilityTime;

        [Header("晶核属性 - Crystal Core Attributes")]
        public CrystalCore crystalCore;
        public float healthRestoreValue;

        [Header("移动属性 - Movement Attributes")]
        public float walkSpeed;
        public float runSpeed;
        public float aimMoveSpeed;
        public float reloadMoveSpeed;

        [Header("装备属性 - Equipment Attributes")]
        public int inventoryGridWidth;
        public int inventoryGridHeight;
        public int moduleSlots;

        [Header("交互属性 - Interaction Attributes")]
        public float interactionRange;
        public LayerMask interactionLayerMask;
        public LayerMask coreInteractionLayerMask;

        [Header("状态等级 - Status Tiers")]
        public HealthTier healthTier;
        public ResilienceState resilienceState;

        // 事件系统
        public System.Action<float, float> OnHealthChanged; // current, max
        public System.Action<float, float> OnResilienceChanged; // current, max
        public System.Action<HealthTier> OnHealthTierChanged;
        public System.Action<ResilienceState> OnResilienceStateChanged;

        // 属性访问器
        public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
        public float ResiliencePercentage => maxResilience > 0 ? currentResilience / maxResilience : 0f;
        public bool IsAlive => currentHealth > 0f && crystalCore.IsIntact;
        public bool IsDead => currentHealth <= 0f && crystalCore.IsDestroyed;
        public bool IsStunned => resilienceState == ResilienceState.Stunned;
        public bool CanUseHealthRestore => crystalCore != null && crystalCore.CanConsumeSlot();

        public PlayerRuntimeStats(PlayerBaseStats baseStats)
        {
            // 复制生存属性
            maxHealth = baseStats.MaxHealth;
            currentHealth = maxHealth; // 开始时满生命值
            maxResilience = baseStats.MaxResilience;
            currentResilience = maxResilience; // 开始时满韧性值
            stunThreshold = baseStats.StunThreshold;
            invulnerabilityTime = baseStats.InvulnerabilityTime;

            // 复制晶核属性
            crystalCore = new CrystalCore(baseStats.CrystalCoreConfig);
            healthRestoreValue = baseStats.HealthRestoreValue;
            
            // 复制移动属性
            walkSpeed = baseStats.WalkSpeed;
            runSpeed = baseStats.RunSpeed;
            aimMoveSpeed = baseStats.AimMoveSpeed;
            reloadMoveSpeed = baseStats.ReloadMoveSpeed;
            
            // 复制装备属性
            inventoryGridWidth = baseStats.InventoryGridWidth;
            inventoryGridHeight = baseStats.InventoryGridHeight;
            moduleSlots = baseStats.ModuleSlots;
            
            // 复制交互属性
            interactionRange = baseStats.InteractionRange;
            interactionLayerMask = baseStats.InteractionLayerMask;
            coreInteractionLayerMask = baseStats.CoreInteractionLayerMask;

            // 初始化状态等级
            UpdateHealthTier();
            UpdateResilienceState();
        }

        /// <summary>
        /// 更新生命等级
        /// </summary>
        public void UpdateHealthTier()
        {
            var previousTier = healthTier;
            healthTier = HealthTierHelper.CalculateHealthTier(HealthPercentage);
            resilienceRegenRate = HealthTierHelper.GetResilienceRegenRate(healthTier);

            if (previousTier != healthTier)
            {
                OnHealthTierChanged?.Invoke(healthTier);
                Debug.Log($"PlayerRuntimeStats: Health tier changed to {healthTier}");
            }
        }

        /// <summary>
        /// 更新韧性状态
        /// </summary>
        public void UpdateResilienceState()
        {
            var previousState = resilienceState;
            resilienceState = HealthTierHelper.CalculateResilienceState(currentResilience, stunThreshold);

            if (previousState != resilienceState)
            {
                OnResilienceStateChanged?.Invoke(resilienceState);
                Debug.Log($"PlayerRuntimeStats: Resilience state changed to {resilienceState}");
            }
        }

        /// <summary>
        /// 受到生命伤害
        /// </summary>
        /// <param name="damage">伤害值</param>
        /// <returns>实际造成的伤害</returns>
        public float TakeHealthDamage(float damage)
        {
            if (damage <= 0f || !IsAlive) return 0f;

            float previousHealth = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - damage);
            float actualDamage = previousHealth - currentHealth;

            if (actualDamage > 0f)
            {
                UpdateHealthTier();
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
                Debug.Log($"PlayerRuntimeStats: Took {actualDamage} health damage. Current: {currentHealth}/{maxHealth}");
            }

            return actualDamage;
        }

        /// <summary>
        /// 受到韧性伤害（硬直）
        /// </summary>
        /// <param name="damage">韧性伤害值</param>
        /// <returns>实际造成的韧性损失</returns>
        public float TakeResilienceDamage(float damage)
        {
            if (damage <= 0f) return 0f;

            float previousResilience = currentResilience;
            currentResilience = Mathf.Max(0f, currentResilience - damage);
            float actualDamage = previousResilience - currentResilience;

            if (actualDamage > 0f)
            {
                UpdateResilienceState();
                OnResilienceChanged?.Invoke(currentResilience, maxResilience);
                Debug.Log($"PlayerRuntimeStats: Took {actualDamage} resilience damage. Current: {currentResilience}/{maxResilience}");
            }

            return actualDamage;
        }

        /// <summary>
        /// 恢复生命值
        /// </summary>
        /// <param name="amount">恢复量</param>
        /// <returns>实际恢复的量</returns>
        public float RestoreHealth(float amount)
        {
            if (amount <= 0f) return 0f;

            float previousHealth = currentHealth;
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            float actualRestore = currentHealth - previousHealth;

            if (actualRestore > 0f)
            {
                UpdateHealthTier();
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
                Debug.Log($"PlayerRuntimeStats: Restored {actualRestore} health. Current: {currentHealth}/{maxHealth}");
            }

            return actualRestore;
        }

        /// <summary>
        /// 使用晶核能量恢复生命值
        /// </summary>
        /// <returns>是否成功使用</returns>
        public bool UseHealthRestore()
        {
            if (crystalCore == null || !crystalCore.CanConsumeSlot()) return false;

            if (crystalCore.ConsumeEnergySlot())
            {
                RestoreHealth(healthRestoreValue);
                Debug.Log($"PlayerRuntimeStats: Used crystal core energy to restore {healthRestoreValue} health");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 更新韧性值（自然恢复）
        /// </summary>
        /// <param name="deltaTime">时间间隔</param>
        public void UpdateResilience(float deltaTime)
        {
            if (resilienceRegenRate <= 0f || currentResilience >= maxResilience) return;

            float previousResilience = currentResilience;
            currentResilience = Mathf.Min(currentResilience + resilienceRegenRate * deltaTime, maxResilience);
            
            if (currentResilience != previousResilience)
            {
                UpdateResilienceState();
                OnResilienceChanged?.Invoke(currentResilience, maxResilience);
            }
        }

        /// <summary>
        /// 完全恢复生命和韧性（存档点使用）
        /// </summary>
        public void FullRestore()
        {
            currentHealth = maxHealth;
            currentResilience = maxResilience;
            crystalCore?.FullRepair();

            UpdateHealthTier();
            UpdateResilienceState();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnResilienceChanged?.Invoke(currentResilience, maxResilience);

            Debug.Log("PlayerRuntimeStats: Full restore completed");
        }

        /// <summary>
        /// 获取保存数据
        /// </summary>
        /// <returns>运行时属性保存数据</returns>
        public PlayerRuntimeStatsSaveData GetSaveData()
        {
            return new PlayerRuntimeStatsSaveData
            {
                currentHealth = this.currentHealth,
                currentResilience = this.currentResilience,
                crystalCoreSaveData = crystalCore?.GetSaveData(),
                inventoryGridWidth = this.inventoryGridWidth,
                inventoryGridHeight = this.inventoryGridHeight
            };
        }

        /// <summary>
        /// 从保存数据加载
        /// </summary>
        /// <param name="saveData">保存数据</param>
        public void LoadFromSaveData(PlayerRuntimeStatsSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogWarning("PlayerRuntimeStats: Cannot load from null save data");
                return;
            }

            currentHealth = Mathf.Clamp(saveData.currentHealth, 0f, maxHealth);
            currentResilience = Mathf.Clamp(saveData.currentResilience, 0f, maxResilience);

            if (saveData.crystalCoreSaveData != null && crystalCore != null)
            {
                crystalCore.LoadFromSaveData(saveData.crystalCoreSaveData);
            }

            // 可升级属性
            if (saveData.inventoryGridWidth > 0) inventoryGridWidth = saveData.inventoryGridWidth;
            if (saveData.inventoryGridHeight > 0) inventoryGridHeight = saveData.inventoryGridHeight;

            UpdateHealthTier();
            UpdateResilienceState();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnResilienceChanged?.Invoke(currentResilience, maxResilience);

            Debug.Log($"PlayerRuntimeStats: Loaded from save data. Health: {currentHealth}/{maxHealth}, Resilience: {currentResilience}/{maxResilience}");
        }

        /// <summary>
        /// 清理事件订阅
        /// </summary>
        public void Cleanup()
        {
            OnHealthChanged = null;
            OnResilienceChanged = null;
            OnHealthTierChanged = null;
            OnResilienceStateChanged = null;
            crystalCore?.Cleanup();
        }
    }

    /// <summary>
    /// 玩家运行时属性保存数据结构
    /// </summary>
    [System.Serializable]
    public class PlayerRuntimeStatsSaveData
    {
        public float currentHealth;
        public float currentResilience;
        public CrystalCoreSaveData crystalCoreSaveData;
        public int inventoryGridWidth;
        public int inventoryGridHeight;

        public PlayerRuntimeStatsSaveData()
        {
            currentHealth = 100f;
            currentResilience = 100f;
            crystalCoreSaveData = null;
            inventoryGridWidth = 5;
            inventoryGridHeight = 5;
        }
    }
}
