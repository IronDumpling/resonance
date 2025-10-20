using UnityEngine;
using System.Collections.Generic;

namespace Resonance.Utilities
{
    /// <summary>
    /// Damage type enumeration
    /// Defines different types of damage in the new system
    /// </summary>
    public enum DamageType
    {
        /// <summary>
        /// Physical Health damage - Directly affects physical health value
        /// Example: Weaponshot, explosion, physical impact
        /// </summary>
        PhysicalHealth,
        
        /// <summary>
        /// Core Health damage - Affects crystal core health/capacity
        /// Example: Core direct hit, wave shattering attack
        /// </summary>
        CoreHealth,
        
        /// <summary>
        /// Chaos/Disorder damage - Affects core wave chaos value (replaces resilience)
        /// Example: Heavy hit, shockwave, disruptive attacks
        /// </summary>
        Chaos
    }

    [System.Serializable]
    public class Damages
    {
        [SerializeField] private float physicalDamage = 0f;
        [SerializeField] private float coreDamage = 0f;
        [SerializeField] private float chaosDamage = 0f;

        public Damages()
        {
            physicalDamage = 0f;
            coreDamage = 0f;
            chaosDamage = 0f;
        }

        public Damages(List<KeyValuePair<DamageType, float>> damages)
        {
            foreach (var damage in damages)
            {
                switch (damage.Key)
                {
                    case DamageType.PhysicalHealth:
                        physicalDamage = damage.Value;
                        break;
                    case DamageType.CoreHealth:
                        coreDamage = damage.Value;
                        break;
                    case DamageType.Chaos:
                        chaosDamage = damage.Value;
                        break;
                    default:
                        break;
                }
            }
        }

        public float GetDamage(DamageType type)
        {
            switch (type)
            {
                case DamageType.PhysicalHealth:
                    return physicalDamage;
                case DamageType.CoreHealth:
                    return coreDamage;
                case DamageType.Chaos:
                    return chaosDamage;
                default:
                    return 0f;
            }
        }

        public int GetCount()
        {
            return (physicalDamage > 0 ? 1 : 0) + (coreDamage > 0 ? 1 : 0) + (chaosDamage > 0 ? 1 : 0);
        }

        public bool HasDamage(DamageType type)
        {
            return GetDamage(type) > 0f;
        }

        public float GetTotalDamage()
        {
            return physicalDamage + coreDamage + chaosDamage;
        }

        public override string ToString()
        {
            return $"Physical: {physicalDamage:F1}, Core: {coreDamage:F1}, Chaos: {chaosDamage:F1}";
        }

        public void SetDamage(DamageType type, float damage)
        {
            switch (type)
            {
                case DamageType.PhysicalHealth:
                    physicalDamage = damage;
                    break;
                case DamageType.CoreHealth:
                    coreDamage = damage;
                    break;
                case DamageType.Chaos:
                    chaosDamage = damage;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Create a deep copy of this Damages instance
        /// </summary>
        /// <returns>A new Damages instance with the same values</returns>
        public Damages Clone()
        {
            var copy = new Damages();
            copy.physicalDamage = this.physicalDamage;
            copy.coreDamage = this.coreDamage;
            copy.chaosDamage = this.chaosDamage;
            return copy;
        }
    }

    /// <summary>
    /// Damage information structure
    /// Uses dictionary to support multiple damage types in a single attack
    /// </summary>
    [System.Serializable]
    public struct DamageInfo
    {
        /// <summary>
        /// Dictionary of damage types and their values
        /// Supports up to 3 different damage types per attack
        /// </summary>
        public Damages damages;
        
        /// <summary>
        /// Damage source position
        /// </summary>
        public Vector3 sourcePosition;
        
        /// <summary>
        /// Damage source object
        /// </summary>
        public GameObject sourceObject;
        
        /// <summary>
        /// Optional description
        /// </summary>
        public string description;
        
        /// <summary>
        /// Constructor - single damage type
        /// </summary>
        public DamageInfo(List<KeyValuePair<DamageType, float>> damages, Vector3 sourcePosition, 
                        GameObject sourceObject = null, string description = "")
        {
            this.damages = new Damages(damages);
            this.sourcePosition = sourcePosition;
            this.sourceObject = sourceObject;
            this.description = description;
        }

        /// <summary>
        /// Constructor - from damages
        /// </summary>
        public DamageInfo(Damages damages, Vector3 sourcePosition, 
                        GameObject sourceObject = null, string description = "")
        {
            this.damages = damages;
            this.sourcePosition = sourcePosition;
            this.sourceObject = sourceObject;
            this.description = description;
        }
        
        /// <summary>
        /// Get damage amount for specific type
        /// </summary>
        public float GetDamage(DamageType type)
        {
            if (damages == null) return 0f;
            return damages.GetDamage(type);
        }
        
        /// <summary>
        /// Check if contains specific damage type
        /// </summary>
        public bool HasDamageType(DamageType type)
        {
            return damages != null && damages.HasDamage(type);
        }
        
        /// <summary>
        /// Get total damage amount (sum of all types)
        /// </summary>
        public float GetTotalDamage()
        {
            if (damages == null) return 0f;
            
            return damages.GetTotalDamage();
        }
        
        /// <summary>
        /// Get debug string
        /// </summary>
        public override string ToString()
        {
            if (damages == null || damages.GetCount() == 0)
                return "No damage";
            
            string result = "Damage: ";
            result += damages.ToString();
            return result.TrimEnd();
        }
    }
}
