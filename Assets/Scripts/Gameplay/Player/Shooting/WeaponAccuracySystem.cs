using UnityEngine;
using Resonance.Gameplay.Items;

namespace Resonance.Gameplay.Player.Shooting
{
    /// <summary>
    /// Weapon Accuracy System - Manages crosshair size and shooting accuracy
    /// Controls dynamic crosshair shrinking/expanding based on player actions
    /// Provides shooting target offset and damage multiplier based on accuracy
    /// </summary>
    public class WeaponAccuracySystem
    {
        private AccuracyState _state;
        private WeaponAccuracyConfig _config;
        
        /// <summary>
        /// Initialize accuracy system with weapon configuration
        /// </summary>
        public void Initialize(WeaponAccuracyConfig config)
        {
            if (config == null)
            {
                Debug.LogError("WeaponAccuracySystem: Cannot initialize with null config");
                return;
            }
            
            _config = config;
            _state = new AccuracyState(config.baseRadius);
            
            Debug.Log($"WeaponAccuracySystem: Initialized with base radius: {config.baseRadius}");
        }
        
        /// <summary>
        /// Update accuracy system each frame while aiming
        /// </summary>
        /// <param name="deltaTime">Time since last frame</param>
        /// <param name="isAiming">Is player in aiming state</param>
        /// <param name="isMoving">Is player moving</param>
        /// <param name="currentMousePosition">Current mouse position in screen space</param>
        public void UpdateAccuracy(float deltaTime, bool isAiming, bool isMoving, Vector2 currentMousePosition)
        {
            if (_config == null || _state == null) return;
            
            _state.isMoving = isMoving;
            _state.timeSinceLastShot += deltaTime;
            
            // Rotation Detection - based on mouse movement, not aim point
            if (_state.lastMousePosition != Vector2.zero)
            {
                float mouseDelta = Vector2.Distance(currentMousePosition, _state.lastMousePosition);
                _state.isRotating = mouseDelta > _config.rotationThreshold * deltaTime;
            }
            _state.lastMousePosition = currentMousePosition;
            
            // Shrinking/Expanding Detection
            bool isShootRecoveryDelay = _state.timeSinceLastShot > _config.shootRecoveryDelay;
            
            // Target Radius Calculation
            if (isAiming && !_state.isMoving && !_state.isRotating && isShootRecoveryDelay)
            {
                // Stationary aiming: shrink crosshair
                _state.aimingTime += deltaTime;
                
                // Calculate target radius based on aiming time and shrink speed
                float shrinkTime = 1.0f / _config.shrinkLerpSpeed; // Time to reach min radius
                float progress = Mathf.Clamp01(_state.aimingTime / shrinkTime);
                _state.targetRadius = Mathf.Lerp(_config.baseRadius, _config.minRadius, progress);
                
                // Direct assignment for shrinking
                _state.currentRadius = _state.targetRadius;
            }
            else
            {
                // Moving/rotating while aiming: expand crosshair
                _state.aimingTime = 0f;
                _state.targetRadius = _config.baseRadius;
                
                if (_state.isMoving)
                {
                    _state.targetRadius += _config.movementRadiusPenalty;
                }
                
                if (_state.isRotating)
                {
                    _state.targetRadius += _config.rotationRadiusPenalty;
                }
                
                // Clamp to max radius
                _state.targetRadius = Mathf.Clamp(_state.targetRadius, _config.minRadius, _config.maxRadius);
                
                // Smooth transition to target radius for expanding
                _state.currentRadius = Mathf.Lerp(
                    _state.currentRadius, 
                    _state.targetRadius, 
                    _config.expandLerpSpeed * deltaTime
                );
            }
        }
        
        /// <summary>
        /// Called when player shoots - resets accuracy and increases crosshair size
        /// </summary>
        public void OnShoot()
        {
            if (_config == null || _state == null) return;
            
            // Reset aiming time
            _state.aimingTime = 0f;
            _state.timeSinceLastShot = 0f;
            
            // Increase crosshair radius
            _state.currentRadius += _config.shootRadiusIncrease;
            _state.currentRadius = Mathf.Clamp(_state.currentRadius, _config.minRadius, _config.maxRadius);
            
            Debug.Log($"WeaponAccuracySystem: Shot fired, radius increased to {_state.currentRadius:F2}");
        }
        
        /// <summary>
        /// Get damage multiplier based on current accuracy
        /// Perfect aim (min radius) = higher damage, poor aim (max radius) = base damage
        /// </summary>
        /// <returns>Damage multiplier (1.0 - perfectAimDamageMultiplier)</returns>
        public float GetDamageMultiplier()
        {
            if (_config == null || _state == null)
            {
                return 1.0f;
            }
            
            // Calculate accuracy ratio (0 = worst, 1 = perfect)
            float radiusRange = _config.baseRadius - _config.minRadius;
            if (radiusRange <= 0f)
            {
                return _config.baseAimDamageMultiplier;
            }
            
            float radiusRatio = (_state.currentRadius - _config.minRadius) / radiusRange;
            float accuracyRatio = Mathf.Clamp01(1.0f - radiusRatio); // Invert: 1 = perfect
            
            // Use curve if available, otherwise linear interpolation
            float damageMultiplier;
            if (_config.damageMultiplierCurve != null && _config.damageMultiplierCurve.length > 0)
            {
                damageMultiplier = _config.damageMultiplierCurve.Evaluate(accuracyRatio);
            }
            else
            {
                damageMultiplier = Mathf.Lerp(_config.baseAimDamageMultiplier, _config.perfectAimDamageMultiplier, accuracyRatio);
            }
            
            return damageMultiplier;
        }
        
        /// <summary>
        /// Get current crosshair radius in world space
        /// </summary>
        /// <returns>Current radius</returns>
        public float GetCurrentRadius()
        {
            return _state?.currentRadius ?? 0f;
        }
        
        /// <summary>
        /// Get accuracy percentage (0-1, where 1 is perfect accuracy)
        /// </summary>
        /// <returns>Accuracy percentage</returns>
        public float GetAccuracyPercentage()
        {
            if (_config == null || _state == null)
            {
                return 0f;
            }
            
            float radiusRange = _config.baseRadius - _config.minRadius;
            if (radiusRange <= 0f)
            {
                return 1.0f;
            }
            
            float radiusRatio = (_state.currentRadius - _config.minRadius) / radiusRange;
            return Mathf.Clamp01(1.0f - radiusRatio);
        }
        
        /// <summary>
        /// Reset accuracy to base state
        /// </summary>
        public void ResetToBase()
        {
            if (_config == null || _state == null) return;
            
            _state.Reset(_config.baseRadius);
            Debug.Log("WeaponAccuracySystem: Reset to base state");
        }
        
        /// <summary>
        /// Check if system is initialized
        /// </summary>
        public bool IsInitialized()
        {
            return _config != null && _state != null;
        }
    }
}

