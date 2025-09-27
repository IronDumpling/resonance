using UnityEngine;

namespace Resonance.Interfaces
{
    /// <summary>
    /// 伤害类型枚举
    /// 定义不同类型的伤害及其对新属性系统的影响
    /// </summary>
    public enum DamageType
    {
        /// <summary>
        /// 生命伤害 - 直接影响生命值
        /// 例如：枪击、爆炸、撞击等
        /// </summary>
        Health,
        
        /// <summary>
        /// 韧性伤害 - 影响韧性值（造成硬直/眩晕）
        /// 例如：重击、冲击波、弹反等
        /// </summary>
        Resilience,
        
        /// <summary>
        /// 晶核伤害 - 影响晶核容量
        /// 例如：共振攻击、晶核直击等
        /// </summary>
        Core,
        
        /// <summary>
        /// 混合伤害 - 同时影响生命和韧性
        /// 例如：特殊武器、环境伤害等
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
