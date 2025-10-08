using UnityEngine;

namespace Resonance.Utilities
{
    /// <summary>
    /// Damage type enumeration
    /// Defines different types of damage and their impact on the new attribute system
    /// </summary>
    public enum DamageType
    {
        /// <summary>
        /// Health damage - Directly affects health value
        /// For example: Gunshot, explosion, impact, etc.
        /// </summary>
        Health,
        
        /// <summary>
        /// Resilience damage - Affects resilience value (causes stuns/眩晕)
        /// For example: Heavy hit, shockwave, counter-attack, etc.
        /// </summary>
        Resilience,
        
        /// <summary>
        /// Core damage - Affects core capacity
        /// For example: Resonance attack, core direct hit, etc.
        /// </summary>
        Core,
        
        /// <summary>
        /// Mixed damage - Affects both health and resilience
        /// For example: Weapons, environmental damage, etc.
        /// </summary>
        Mixed
    }
    
    /// <summary>
    /// 伤害信息结构体
    /// 包含伤害的详细信息
    /// </summary>
    [System.Serializable]
    public struct DamageInfo
    {
        /// <summary>
        /// 伤害值
        /// </summary>
        public float amount;
        
        /// <summary>
        /// 伤害类型
        /// </summary>
        public DamageType type;
        
        /// <summary>
        /// 伤害来源位置
        /// </summary>
        public Vector3 sourcePosition;
        
        /// <summary>
        /// 伤害来源对象
        /// </summary>
        public GameObject sourceObject;
        
        /// <summary>
        /// 伤害描述（可选）
        /// </summary>
        public string description;
        
        /// <summary>
        /// 对于混合伤害，生命伤害的比例 (0-1)
        /// 1.0 = 全部生命伤害，0.0 = 全部韧性伤害
        /// </summary>
        public float healthRatio;
        
        /// <summary>
        /// 韧性伤害值（用于造成硬直/眩晕）
        /// </summary>
        public float resilienceDamage;

        public DamageInfo(float amount, DamageType type, Vector3 sourcePosition, GameObject sourceObject = null, string description = "")
        {
            this.amount = amount;
            this.type = type;
            this.sourcePosition = sourcePosition;
            this.sourceObject = sourceObject;
            this.description = description;
            this.healthRatio = type == DamageType.Health ? 1.0f : 0.0f;
            this.resilienceDamage = type == DamageType.Resilience ? amount : 0.0f;
        }
        
        public DamageInfo(float amount, DamageType type, Vector3 sourcePosition, float healthRatio, GameObject sourceObject = null, string description = "")
        {
            this.amount = amount;
            this.type = type;
            this.sourcePosition = sourcePosition;
            this.sourceObject = sourceObject;
            this.description = description;
            this.healthRatio = Mathf.Clamp01(healthRatio);
            this.resilienceDamage = type == DamageType.Resilience ? amount : 0.0f;
        }

        public DamageInfo(float amount, DamageType type, Vector3 sourcePosition, float healthRatio, float resilienceDamage, GameObject sourceObject = null, string description = "")
        {
            this.amount = amount;
            this.type = type;
            this.sourcePosition = sourcePosition;
            this.sourceObject = sourceObject;
            this.description = description;
            this.healthRatio = Mathf.Clamp01(healthRatio);
            this.resilienceDamage = Mathf.Max(0f, resilienceDamage);
        }
    }
}
