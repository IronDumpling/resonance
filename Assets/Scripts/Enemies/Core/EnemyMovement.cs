using UnityEngine;
using Resonance.Enemies.Data;

namespace Resonance.Enemies.Core
{
    /// <summary>
    /// Enemy movement system that handles movement in XZ plane only (no jumping or Y-axis movement)
    /// Similar to PlayerMovement but simplified for AI-controlled enemies
    /// </summary>
    public class EnemyMovement
    {
        private EnemyRuntimeStats _stats;
        private Transform _transform;
        
        // Movement state
        private Vector3 _targetPosition;
        private Vector3 _velocity;
        private bool _hasTarget = false;
        private float _movementSpeedModifier = 1f;
        
        // Movement configuration
        private const float ARRIVAL_THRESHOLD = 0.1f;
        private const float ROTATION_SPEED = 10f; // How fast enemy turns
        
        // Events
        public System.Action OnTargetReached;
        
        // Properties
        public Vector3 TargetPosition => _targetPosition;
        public Vector3 Velocity => _velocity;
        public bool HasTarget => _hasTarget;
        public bool IsMoving => _hasTarget && Vector3.Distance(_transform.position, _targetPosition) > ARRIVAL_THRESHOLD;
        public float MovementSpeedModifier 
        { 
            get => _movementSpeedModifier; 
            set => _movementSpeedModifier = Mathf.Clamp01(value); 
        }

        public EnemyMovement(EnemyRuntimeStats stats, Transform transform)
        {
            _stats = stats;
            _transform = transform;
        }

        public void Update(float deltaTime)
        {
            if (!_hasTarget || _transform == null) return;
            
            UpdateMovement(deltaTime);
            UpdateRotation(deltaTime);
        }
        
        #region Movement Control
        
        /// <summary>
        /// Set a target position to move towards
        /// </summary>
        public void SetTarget(Vector3 targetPosition)
        {
            _targetPosition = targetPosition;
            _hasTarget = true;
        }
        
        /// <summary>
        /// Stop movement
        /// </summary>
        public void Stop()
        {
            _hasTarget = false;
            _velocity = Vector3.zero;
        }
        
        /// <summary>
        /// Move towards target using specified speed
        /// </summary>
        public void MoveToTarget(float moveSpeed, float deltaTime)
        {
            if (!_hasTarget || _transform == null) return;
            
            Vector3 currentPosition = _transform.position;
            Vector3 direction = (_targetPosition - currentPosition);
            
            // Only move in XZ plane
            direction.y = 0f;
            direction = direction.normalized;
            
            // Calculate movement
            float actualSpeed = moveSpeed * _movementSpeedModifier;
            Vector3 movement = direction * actualSpeed * deltaTime;
            
            // Apply movement
            _transform.position += movement;
            _velocity = movement / deltaTime;
            
            // Check if we've reached the target
            if (Vector3.Distance(currentPosition, _targetPosition) <= ARRIVAL_THRESHOLD)
            {
                OnTargetReached?.Invoke();
                Stop();
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void UpdateMovement(float deltaTime)
        {
            if (!IsMoving) return;
            
            // Use default move speed - specific actions will call MoveToTarget with their own speed
            float moveSpeed = _stats.GetModifiedMoveSpeed();
            MoveToTarget(moveSpeed, deltaTime);
        }
        
        private void UpdateRotation(float deltaTime)
        {
            if (!IsMoving) return;
            
            // Rotate to face movement direction
            Vector3 direction = (_targetPosition - _transform.position).normalized;
            direction.y = 0f; // Keep rotation in XZ plane only
            
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                _transform.rotation = Quaternion.Slerp(
                    _transform.rotation, 
                    targetRotation, 
                    ROTATION_SPEED * deltaTime
                );
            }
        }
        
        #endregion
        
        #region Public Utilities
        
        /// <summary>
        /// Get distance to current target
        /// </summary>
        public float GetDistanceToTarget()
        {
            if (!_hasTarget || _transform == null) return float.MaxValue;
            return Vector3.Distance(_transform.position, _targetPosition);
        }
        
        /// <summary>
        /// Get direction to current target (normalized, XZ plane only)
        /// </summary>
        public Vector3 GetDirectionToTarget()
        {
            if (!_hasTarget || _transform == null) return Vector3.zero;
            
            Vector3 direction = (_targetPosition - _transform.position);
            direction.y = 0f;
            return direction.normalized;
        }
        
        /// <summary>
        /// Check if enemy can reach target position
        /// </summary>
        public bool CanReachTarget(Vector3 target)
        {
            // For now, assume all positions are reachable
            // In a more complex system, this could check for obstacles, NavMesh, etc.
            return true;
        }
        
        #endregion
    }
}