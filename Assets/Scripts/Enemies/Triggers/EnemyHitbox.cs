using UnityEngine;
using Resonance.Interfaces;

namespace Resonance.Enemies
{
    public enum EnemyHitboxType 
    { 
        Head,
        Body, 
        Core, 
    }

    /// <summary>
    /// Weakpoint hitbox component - acts as a damage modifier
    /// Modifies damage when hit by shooting system,
    /// then forwards the modified damage to the enemy's main damage handler
    /// </summary>
    public class EnemyHitbox : MonoBehaviour
    {
        public EnemyHitboxType type;
        public float physicalMultiplier = 2f;     // 打到时对物理部分的倍率
        public float mentalMultiplier   = 2f;     // 打到时对精神部分的倍率
        
        [Range(0,1)] public float convertPhysicalToMental = 0f; // 例如核心被打时，把物理伤害按比例转为精神
        
        public float poiseBonus = 0f;             // 额外韧性值
        public GameObject hitVFX; 
        public AudioClip hitSFX;
        
        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;
        
        // References
        private EnemyMonoBehaviour _enemyMono;
        private bool _isInitialized = false;

        #region Initialization

        /// <summary>
        /// Initialize the weakpoint hitbox with enemy reference
        /// </summary>
        /// <param name="enemyMono">Enemy MonoBehaviour reference</param>
        public void Initialize(EnemyMonoBehaviour enemyMono)
        {
            _enemyMono = enemyMono;
            _isInitialized = true;
            
            if (_debugMode)
            {
                Debug.Log($"EnemyHitbox: Initialized {type} weakpoint on {gameObject.name}");
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Process damage hit on this weakpoint and apply to enemy
        /// Called by ShootingSystem when this collider is hit
        /// </summary>
        /// <param name="damageInfo">Original damage information</param>
        public void ProcessDamageHit(DamageInfo damageInfo)
        {
            if (!_isInitialized || _enemyMono == null)
            {
                if (_debugMode)
                {
                    Debug.LogWarning($"EnemyHitbox: Cannot process damage - not initialized or no enemy reference");
                }
                return;
            }

            if (_debugMode)
            {
                Debug.Log($"EnemyHitbox: Processing {damageInfo.amount} {damageInfo.type} damage on {type} weakpoint");
            }

            // Modify damage based on weakpoint properties
            DamageInfo modifiedDamage = ModifyDamage(damageInfo, damageInfo.sourcePosition);
            
            // Play hit effects
            PlayHitFX(damageInfo.sourcePosition);
            
            // Apply modified damage to enemy
            _enemyMono.TakeDamage(modifiedDamage);
            
            if (_debugMode)
            {
                Debug.Log($"EnemyHitbox: Applied modified damage {modifiedDamage.amount} {modifiedDamage.type} to {_enemyMono.name}");
            }
        }

        /// <summary>
        /// Check if this weakpoint is properly initialized
        /// </summary>
        public bool IsInitialized => _isInitialized && _enemyMono != null;

        #endregion

        #region Damage Modification

        /// <summary>
        /// Modify damage based on weakpoint properties
        /// </summary>
        /// <param name="d">Original damage info</param>
        /// <param name="hitPoint">Hit point position</param>
        /// <returns>Modified damage info</returns>
        public DamageInfo ModifyDamage(DamageInfo d, Vector3 hitPoint)
        {
            // Create a copy to avoid modifying the original
            DamageInfo modifiedDamage = new DamageInfo(
                d.amount,
                d.type,
                d.sourcePosition,
                d.physicalRatio,
                d.sourceObject,
                d.description
            );

            switch (modifiedDamage.type)
            {
                case DamageType.Physical:
                    if (convertPhysicalToMental > 0f && type == EnemyHitboxType.Core) 
                    {
                        // Convert some physical damage to mental
                        modifiedDamage.type = DamageType.Mixed;
                        modifiedDamage.physicalRatio = Mathf.Clamp01(1f - convertPhysicalToMental);
                        
                        // Apply multipliers to both parts
                        float physPart = modifiedDamage.amount * modifiedDamage.physicalRatio * physicalMultiplier;
                        float mentPart = modifiedDamage.amount * (1f - modifiedDamage.physicalRatio) * mentalMultiplier;
                        modifiedDamage.amount = physPart + mentPart;
                    } 
                    else 
                    {
                        modifiedDamage.amount *= physicalMultiplier;
                    }
                    break;
                    
                case DamageType.Mental:
                    modifiedDamage.amount *= mentalMultiplier;
                    break;
                    
                case DamageType.Mixed:
                    // Apply multipliers separately to physical and mental portions
                    float physDamage = modifiedDamage.amount * modifiedDamage.physicalRatio * physicalMultiplier;
                    float mentDamage = modifiedDamage.amount * (1f - modifiedDamage.physicalRatio) * mentalMultiplier;
                    modifiedDamage.amount = physDamage + mentDamage;
                    break;
            }

            // Update hit point and description
            modifiedDamage.sourcePosition = hitPoint;
            modifiedDamage.description = string.IsNullOrEmpty(modifiedDamage.description)
                ? $"Weakpoint:{type}"
                : $"{modifiedDamage.description}|Weakpoint:{type}";

            if (_debugMode)
            {
                Debug.Log($"EnemyHitbox: Modified damage from {d.amount} to {modifiedDamage.amount} ({d.type} -> {modifiedDamage.type})");
            }

            return modifiedDamage;
        }

        #endregion

        #region Visual and Audio Effects

        /// <summary>
        /// Play hit visual and audio effects
        /// </summary>
        /// <param name="at">Effect position</param>
        public void PlayHitFX(Vector3 at)
        {
            if (hitVFX) 
            {
                Instantiate(hitVFX, at, Quaternion.identity);
                if (_debugMode)
                {
                    Debug.Log($"EnemyHitbox: Spawned hit VFX at {at}");
                }
            }
            
            if (hitSFX) 
            {
                AudioSource.PlayClipAtPoint(hitSFX, at);
                if (_debugMode)
                {
                    Debug.Log($"EnemyHitbox: Played hit SFX at {at}");
                }
            }
        }

        #endregion
    }
}