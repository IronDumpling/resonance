using UnityEngine;

namespace Resonance.Player.Shooting
{
    /// <summary>
    /// Shooting result data structure
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
        public float damage;          // Weapon base damage value
        public float actualDamage;    // Actual damage value
        public Vector3 mouseTargetPoint; // Mouse pointing target point (stage 1 result)
        
        // Hit information (if hasHit is true)
        public GameObject hitObject;  // Hit object
        public Vector3 hitPoint;      // Hit point
        public Vector3 hitNormal;     // Hit normal
        public float hitDistance;     // Hit distance
    }
}