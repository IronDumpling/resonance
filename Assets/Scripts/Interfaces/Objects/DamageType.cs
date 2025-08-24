using UnityEngine;

namespace Resonance.Interfaces
{
    /// <summary>
    /// 伤害类型枚举
    /// 定义不同类型的伤害及其对双血量系统的影响
    /// </summary>
    public enum DamageType
    {
        /// <summary>
        /// 物理伤害 - 影响物理血量
        /// 例如：枪击、爆炸、撞击等
        /// </summary>
        Physical,
        
        /// <summary>
        /// 精神伤害 - 影响精神血量
        /// 例如：共振攻击、心理攻击等
        /// </summary>
        Mental,
        
        /// <summary>
        /// 混合伤害 - 同时影响物理和精神血量
        /// 例如：特殊武器、环境伤害等
        /// </summary>
        Mixed,
        
        /// <summary>
        /// 真实伤害 - 直接影响核心生命值（绕过双血量系统）
        /// 例如：即死攻击、特殊机制伤害等
        /// </summary>
        True
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
        /// 对于混合伤害，物理伤害的比例 (0-1)
        /// 1.0 = 全部物理伤害，0.0 = 全部精神伤害
        /// </summary>
        public float physicalRatio;
        
        public DamageInfo(float amount, DamageType type, Vector3 sourcePosition, GameObject sourceObject = null, string description = "")
        {
            this.amount = amount;
            this.type = type;
            this.sourcePosition = sourcePosition;
            this.sourceObject = sourceObject;
            this.description = description;
            this.physicalRatio = type == DamageType.Physical ? 1.0f : 0.0f;
        }
        
        public DamageInfo(float amount, DamageType type, Vector3 sourcePosition, float physicalRatio, GameObject sourceObject = null, string description = "")
        {
            this.amount = amount;
            this.type = type;
            this.sourcePosition = sourcePosition;
            this.sourceObject = sourceObject;
            this.description = description;
            this.physicalRatio = Mathf.Clamp01(physicalRatio);
        }
    }
}
