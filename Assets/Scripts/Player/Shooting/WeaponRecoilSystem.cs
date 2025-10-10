using UnityEngine;
using Resonance.Items;

namespace Resonance.Player.Shooting
{
    /// <summary>
    /// Weapon Recoil System - Manages recoil offset and recovery
    /// Applies recoil to aim point after shooting and handles smooth recovery
    /// </summary>
    public class WeaponRecoilSystem
    {
        private RecoilState _state;
        private WeaponRecoilConfig _config;
        
        /// <summary>
        /// Initialize recoil system with weapon configuration
        /// </summary>
        public void Initialize(WeaponRecoilConfig config)
        {
            if (config == null)
            {
                Debug.LogError("WeaponRecoilSystem: Cannot initialize with null config");
                return;
            }
            
            _config = config;
            _state = new RecoilState();
            
            Debug.Log($"WeaponRecoilSystem: Initialized with recoil offset: {config.recoilOffset}");
        }
        
        /// <summary>
        /// Update recoil system each frame - handles recoil recovery
        /// </summary>
        /// <param name="deltaTime">Time since last frame</param>
        public void UpdateRecoil(float deltaTime)
        {
            if (_config == null || _state == null) return;
            
            // Update recovery timer
            if (_state.recoveryTimer > 0f)
            {
                _state.recoveryTimer -= deltaTime;
                return;
            }
            
            // Smooth recovery towards zero
            if (_state.currentRecoilOffset.magnitude > 0.01f)
            {
                _state.currentRecoilOffset = Vector3.Lerp(
                    _state.currentRecoilOffset,
                    Vector3.zero,
                    _config.recoverySpeed * deltaTime
                );
            }
            else
            {
                // Fully recovered - reset state
                _state.currentRecoilOffset = Vector3.zero;
                _state.consecutiveShots = 0;
            }
        }
        
        /// <summary>
        /// Apply recoil when shooting
        /// </summary>
        /// <param name="accuracyPercentage">Current accuracy percentage (0-1, where 1 is perfect aim)</param>
        public void ApplyRecoil(float accuracyPercentage = 1.0f)
        {
            if (_config == null || _state == null) return;
            
            _state.consecutiveShots++;
            
            // === RECOIL OFFSET CALCULATION ===
            // Consecutive shots affect the base recoil offset (predictable recoil pattern)
            float consecutiveShotMultiplier = 1.0f;
            if (_config.recoilMultiplierCurve != null && _config.recoilMultiplierCurve.length > 0)
            {
                consecutiveShotMultiplier = _config.recoilMultiplierCurve.Evaluate(_state.consecutiveShots);
            }
            
            // Calculate base recoil offset (predictable component)
            Vector3 baseRecoilOffset = _config.recoilOffset * consecutiveShotMultiplier;
            
            // === RECOIL VARIANCE CALCULATION ===
            // Accuracy affects only the random variance (unpredictable component)
            // Perfect aim (1.0) → variance * 0.1 (very stable)
            // Worst aim (0.0) → variance * 1.0 (full randomness)
            float accuracyVarianceMultiplier = Mathf.Lerp(1.0f, 0.1f, accuracyPercentage);
            Vector3 adjustedRecoilVariance = _config.recoilVariance * accuracyVarianceMultiplier;
            
            // Apply random variance to base recoil
            Vector3 randomVariance = new Vector3(
                Random.Range(-adjustedRecoilVariance.x, adjustedRecoilVariance.x),
                Random.Range(-adjustedRecoilVariance.y, adjustedRecoilVariance.y),
                Random.Range(-adjustedRecoilVariance.z, adjustedRecoilVariance.z)
            );
            
            // Final recoil = predictable base + random variance
            Vector3 finalRecoil = baseRecoilOffset + randomVariance;
            
            // Accumulate total recoil
            _state.currentRecoilOffset += finalRecoil;
            
            // Reset recovery timer
            _state.recoveryTimer = _config.recoveryDelay;
            
            Debug.Log($"WeaponRecoilSystem: Recoil applied (shot #{_state.consecutiveShots}) - " +
                     $"Base: {baseRecoilOffset}, Variance: {randomVariance}, " +
                     $"Accuracy: {accuracyPercentage:P0}, Consecutive: {consecutiveShotMultiplier:F2}");
        }
        
        /// <summary>
        /// Get current recoil offset to apply to aim point
        /// </summary>
        /// <returns>Recoil offset in world space</returns>
        public Vector3 GetRecoilOffset()
        {
            return _state?.currentRecoilOffset ?? Vector3.zero;
        }
        
        /// <summary>
        /// Reset recoil to zero
        /// </summary>
        public void ResetRecoil()
        {
            if (_state == null) return;
            
            _state.Reset();
            Debug.Log("WeaponRecoilSystem: Reset recoil");
        }
        
        /// <summary>
        /// Get number of consecutive shots fired
        /// </summary>
        public int GetConsecutiveShots()
        {
            return _state?.consecutiveShots ?? 0;
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

