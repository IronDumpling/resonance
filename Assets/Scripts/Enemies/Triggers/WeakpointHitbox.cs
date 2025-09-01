using UnityEngine;
using Resonance.Interfaces;

namespace Resonance.Enemies
{
    public enum WeakpointType 
    { 
        Physical, 
        Mental 
    }

    /// <summary>
    /// Weakpoint hitbox component - acts as a damage modifier and relay
    /// Implements IDamageable to receive damage from shooting system,
    /// modifies the damage based on weakpoint properties,
    /// then forwards to the enemy's main damage handler
    /// </summary>
    public class WeakpointHitbox : MonoBehaviour, IDamageable
    {
        public WeakpointType type;
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
                Debug.Log($"WeakpointHitbox: Initialized {type} weakpoint on {gameObject.name}");
            }
        }

        #endregion

        #region IDamageable Implementation

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!_isInitialized || _enemyMono == null)
            {
                if (_debugMode)
                {
                    Debug.LogWarning($"WeakpointHitbox: Cannot process damage - not initialized or no enemy reference");
                }
                return;
            }

            if (_debugMode)
            {
                Debug.Log($"WeakpointHitbox: Received {damageInfo.amount} {damageInfo.type} damage on {type} weakpoint");
            }

            // Modify damage based on weakpoint properties
            DamageInfo modifiedDamage = ModifyDamage(damageInfo, damageInfo.sourcePosition);
            
            // Play hit effects
            PlayHitFX(damageInfo.sourcePosition);
            
            // Forward modified damage to enemy
            _enemyMono.TakeDamage(modifiedDamage);
            
            if (_debugMode)
            {
                Debug.Log($"WeakpointHitbox: Forwarded {modifiedDamage.amount} {modifiedDamage.type} damage to enemy");
            }
        }

        public void TakePhysicalDamage(float damage, Vector3 damageSource)
        {
            DamageInfo damageInfo = new DamageInfo(damage, DamageType.Physical, damageSource, null, $"Weakpoint:{type}");
            TakeDamage(damageInfo);
        }

        public void TakeMentalDamage(float damage, Vector3 damageSource)
        {
            DamageInfo damageInfo = new DamageInfo(damage, DamageType.Mental, damageSource, null, $"Weakpoint:{type}");
            TakeDamage(damageInfo);
        }

        #endregion

        #region IDamageable Properties (Forward to Enemy)

        public bool IsPhysicallyAlive => _isInitialized && _enemyMono != null ? _enemyMono.IsPhysicallyAlive : true;
        public bool IsMentallyAlive => _isInitialized && _enemyMono != null ? _enemyMono.IsMentallyAlive : true;
        public bool IsInPhysicalDeathState => _isInitialized && _enemyMono != null ? _enemyMono.IsInPhysicalDeathState : false;
        public float CurrentPhysicalHealth => _isInitialized && _enemyMono != null ? _enemyMono.CurrentPhysicalHealth : 0f;
        public float MaxPhysicalHealth => _isInitialized && _enemyMono != null ? _enemyMono.MaxPhysicalHealth : 0f;
        public float CurrentMentalHealth => _isInitialized && _enemyMono != null ? _enemyMono.CurrentMentalHealth : 0f;
        public float MaxMentalHealth => _isInitialized && _enemyMono != null ? _enemyMono.MaxMentalHealth : 0f;

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
                    if (convertPhysicalToMental > 0f && type == WeakpointType.Mental) 
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
                Debug.Log($"WeakpointHitbox: Modified damage from {d.amount} to {modifiedDamage.amount} ({d.type} -> {modifiedDamage.type})");
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
                    Debug.Log($"WeakpointHitbox: Spawned hit VFX at {at}");
                }
            }
            
            if (hitSFX) 
            {
                AudioSource.PlayClipAtPoint(hitSFX, at);
                if (_debugMode)
                {
                    Debug.Log($"WeakpointHitbox: Played hit SFX at {at}");
                }
            }
        }

        #endregion
    }
}