using UnityEngine;
using System.Collections.Generic;
using Resonance.Utilities;

namespace Resonance.Player.Shooting
{
    /// <summary>
    /// Shooting result data structure
    /// Supports multiple damage types with separate base and actual damage tracking
    /// </summary>
    [System.Serializable]
    public struct ShootingResult
    {
        public bool success;          // Shooting is successful
        public bool hasHit;           // Whether the target is hit
        public Vector3 startPosition; // Shooting start position
        public Vector3 endPosition;   // Shooting end position
        public Vector3 direction;     // Shooting direction
        public float range;           // Shooting distance
        
        // Damage tracking - base damage (before multipliers/modifiers)
        public Dictionary<DamageType, float> baseDamages;
        
        // Damage tracking - actual damage dealt (after all multipliers/modifiers)
        public Dictionary<DamageType, float> actualDamages;
        
        public Vector3 mouseTargetPoint; // Mouse pointing target point (stage 1 result)
        
        // Hit information (if hasHit is true)
        public GameObject hitObject;  // Hit object
        public Vector3 hitPoint;      // Hit point
        public Vector3 hitNormal;     // Hit normal
        public float hitDistance;     // Hit distance
        
        /// <summary>
        /// Get total base damage (sum of all damage types)
        /// </summary>
        public float GetTotalBaseDamage()
        {
            if (baseDamages == null) return 0f;
            
            float total = 0f;
            foreach (var damage in baseDamages.Values)
            {
                total += damage;
            }
            return total;
        }
        
        /// <summary>
        /// Get total actual damage (sum of all damage types)
        /// </summary>
        public float GetTotalActualDamage()
        {
            if (actualDamages == null) return 0f;
            
            float total = 0f;
            foreach (var damage in actualDamages.Values)
            {
                total += damage;
            }
            return total;
        }
        
        /// <summary>
        /// Get base damage for specific type
        /// </summary>
        public float GetBaseDamage(DamageType type)
        {
            return baseDamages != null && baseDamages.ContainsKey(type) ? baseDamages[type] : 0f;
        }
        
        /// <summary>
        /// Get actual damage for specific type
        /// </summary>
        public float GetActualDamage(DamageType type)
        {
            return actualDamages != null && actualDamages.ContainsKey(type) ? actualDamages[type] : 0f;
        }
        
        /// <summary>
        /// Get damage breakdown string for debugging
        /// </summary>
        public string GetDamageBreakdown()
        {
            if (actualDamages == null || actualDamages.Count == 0)
                return "No damage";
            
            string result = "Actual: ";
            foreach (var kvp in actualDamages)
            {
                result += $"{kvp.Key}={kvp.Value:F1} ";
            }
            return result.TrimEnd();
        }
    }
}