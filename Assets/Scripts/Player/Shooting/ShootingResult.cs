using UnityEngine;
using System.Collections.Generic;
using Resonance.Utilities.Types;

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
        public Damages baseDamages;
        
        // Damage tracking - actual damage dealt (after all multipliers/modifiers)
        public Damages actualDamages;
        
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
            
            return baseDamages.GetTotalDamage();
        }
        
        /// <summary>
        /// Get total actual damage (sum of all damage types)
        /// </summary>
        public float GetTotalActualDamage()
        {
            if (actualDamages == null) return 0f;
            
            return actualDamages.GetTotalDamage();
        }
        
        /// <summary>
        /// Get base damage for specific type
        /// </summary>
        public float GetBaseDamage(DamageType type)
        {
            return baseDamages != null && baseDamages.HasDamage(type) ? baseDamages.GetDamage(type) : 0f;
        }
        
        /// <summary>
        /// Get actual damage for specific type
        /// </summary>
        public float GetActualDamage(DamageType type)
        {
            return actualDamages != null && actualDamages.HasDamage(type) ? actualDamages.GetDamage(type) : 0f;
        }
        
        /// <summary>
        /// Get damage breakdown string for debugging
        /// </summary>
        public string GetDamageBreakdown()
        {
            if (actualDamages == null || actualDamages.GetCount() == 0)
                return "No damage";
            return actualDamages.ToString();
        }
    }
}