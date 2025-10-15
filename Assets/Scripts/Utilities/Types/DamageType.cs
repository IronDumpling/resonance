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
        /// Example: Gunshot, explosion, physical impact
        /// </summary>
        PhysicalHealth,
        
        /// <summary>
        /// Core Health damage - Affects crystal core health/capacity
        /// Example: Core direct hit, resonance shattering attack
        /// </summary>
        CoreHealth,
        
        /// <summary>
        /// Chaos/Disorder damage - Affects core wave chaos value (replaces resilience)
        /// Example: Heavy hit, shockwave, disruptive attacks
        /// </summary>
        Chaos
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
        public Dictionary<DamageType, float> damages;
        
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
        public DamageInfo(DamageType type, float amount, Vector3 sourcePosition, GameObject sourceObject = null, string description = "")
        {
            this.damages = new Dictionary<DamageType, float> { { type, amount } };
            this.sourcePosition = sourcePosition;
            this.sourceObject = sourceObject;
            this.description = description;
        }
        
        /// <summary>
        /// Constructor - two damage types
        /// </summary>
        public DamageInfo(DamageType type1, float amount1, DamageType type2, float amount2, 
                         Vector3 sourcePosition, GameObject sourceObject = null, string description = "")
        {
            this.damages = new Dictionary<DamageType, float> 
            { 
                { type1, amount1 },
                { type2, amount2 }
            };
            this.sourcePosition = sourcePosition;
            this.sourceObject = sourceObject;
            this.description = description;
        }
        
        /// <summary>
        /// Constructor - three damage types
        /// </summary>
        public DamageInfo(DamageType type1, float amount1, DamageType type2, float amount2, 
                         DamageType type3, float amount3, Vector3 sourcePosition, 
                         GameObject sourceObject = null, string description = "")
        {
            this.damages = new Dictionary<DamageType, float> 
            { 
                { type1, amount1 },
                { type2, amount2 },
                { type3, amount3 }
            };
            this.sourcePosition = sourcePosition;
            this.sourceObject = sourceObject;
            this.description = description;
        }
        
        /// <summary>
        /// Constructor - from dictionary
        /// </summary>
        public DamageInfo(Dictionary<DamageType, float> damages, Vector3 sourcePosition, 
                         GameObject sourceObject = null, string description = "")
        {
            this.damages = new Dictionary<DamageType, float>(damages);
            this.sourcePosition = sourcePosition;
            this.sourceObject = sourceObject;
            this.description = description;
        }
        
        /// <summary>
        /// Get damage amount for specific type
        /// </summary>
        public float GetDamage(DamageType type)
        {
            return damages != null && damages.ContainsKey(type) ? damages[type] : 0f;
        }
        
        /// <summary>
        /// Check if contains specific damage type
        /// </summary>
        public bool HasDamageType(DamageType type)
        {
            return damages != null && damages.ContainsKey(type);
        }
        
        /// <summary>
        /// Get total damage amount (sum of all types)
        /// </summary>
        public float GetTotalDamage()
        {
            if (damages == null) return 0f;
            
            float total = 0f;
            foreach (var damage in damages.Values)
            {
                total += damage;
            }
            return total;
        }
        
        /// <summary>
        /// Get damage types count
        /// </summary>
        public int GetDamageTypesCount()
        {
            return damages?.Count ?? 0;
        }
        
        /// <summary>
        /// Get debug string
        /// </summary>
        public override string ToString()
        {
            if (damages == null || damages.Count == 0)
                return "No damage";
            
            string result = "Damage: ";
            foreach (var kvp in damages)
            {
                result += $"{kvp.Key}={kvp.Value:F1} ";
            }
            return result.TrimEnd();
        }
    }
}
