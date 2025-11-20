using UnityEngine;

namespace Resonance.Gameplay.Player.Shooting
{
    /// <summary>
    /// Accuracy state - Runtime data for weapon accuracy system
    /// Tracks current crosshair size, aiming time, and player movement state
    /// </summary>
    public class AccuracyState
    {
        // Crosshair radius (world space units)
        public float currentRadius;        // Current crosshair radius
        public float targetRadius;         // Target crosshair radius for smooth transitions
        
        // Aiming time tracking
        public float aimingTime;           // Time spent aiming without moving
        public float timeSinceLastShot;    // Time since last shot fired
        
        // Player state tracking
        public bool isMoving;              // Is player moving
        public Vector2 lastMousePosition;  // Previous frame's mouse position for rotation detection
        public bool isRotating;            // Is mouse moving rapidly (player rotating aim)
        
        /// <summary>
        /// Initialize state with base radius
        /// </summary>
        public AccuracyState(float baseRadius)
        {
            currentRadius = baseRadius;
            targetRadius = baseRadius;
            aimingTime = 0f;
            timeSinceLastShot = 0f;
            isMoving = false;
            lastMousePosition = Vector2.zero;
            isRotating = false;
        }
        
        /// <summary>
        /// Reset state to base configuration
        /// </summary>
        public void Reset(float baseRadius)
        {
            currentRadius = baseRadius;
            targetRadius = baseRadius;
            aimingTime = 0f;
            timeSinceLastShot = 0f;
            isMoving = false;
            isRotating = false;
        }
    }
}

