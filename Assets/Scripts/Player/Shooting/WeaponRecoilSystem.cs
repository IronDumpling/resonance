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
        /// <param name="isAiming">Is player currently aiming (reduces recoil)</param>
        public void ApplyRecoil(bool isAiming)
        {
            if (_config == null || _state == null) return;
            
            _state.consecutiveShots++;
            
            // Calculate recoil multiplier based on consecutive shots
            float multiplier = 1.0f;
            if (_config.recoilMultiplierCurve != null && _config.recoilMultiplierCurve.length > 0)
            {
                multiplier = _config.recoilMultiplierCurve.Evaluate(_state.consecutiveShots);
            }
            
            // Reduce recoil when aiming
            if (isAiming)
            {
                multiplier *= _config.aimingRecoilMultiplier;
            }
            
            // Calculate recoil offset
            Vector3 recoil = _config.recoilOffset * multiplier;
            
            // Add random variance
            recoil.x += Random.Range(-_config.recoilVariance.x, _config.recoilVariance.x);
            recoil.y += Random.Range(-_config.recoilVariance.y, _config.recoilVariance.y);
            recoil.z += Random.Range(-_config.recoilVariance.z, _config.recoilVariance.z);
            
            // Accumulate recoil
            _state.currentRecoilOffset += recoil;
            
            // Reset recovery timer
            _state.recoveryTimer = _config.recoveryDelay;
            
            Debug.Log($"WeaponRecoilSystem: Recoil applied (shot #{_state.consecutiveShots}), offset: {_state.currentRecoilOffset}, multiplier: {multiplier:F2}");
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

