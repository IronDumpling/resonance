using UnityEngine;
using Resonance.Enemies.Data;
using Resonance.Enemies.Core;

namespace Resonance.Enemies.Movement
{
    /// <summary>
    /// Enemy movement system that handles movement in XZ plane only (no jumping or Y-axis movement)
    /// Similar to MovementSystem with centralized speed management based on current state and action
    /// 
    /// Movement speed is determined by:
    /// - Current enemy state (Normal, Reviving, TrueDeath, Stun)
    /// - Current action (Chase, Patrol, NormalAttack, WaveAttack, Revive)
    /// - Health tier modifiers (from EnemyBaseStats)
    /// </summary>
    public class MovementSystem
    {
        private EnemyRuntimeStats _stats;
        private Transform _transform;
        private EnemyController _enemyController; // Reference to get current state info
        
        // Movement state
        private Vector3 _targetPosition;
        private Vector3 _velocity;
        private bool _hasTarget = false;
        private float _movementSpeedModifier = 1f;
        
        // Movement configuration
        private const float ROTATION_SPEED = 10f; // How fast enemy turns
        
        // Events
        public System.Action OnTargetReached;
        
        // Properties
        public Vector3 TargetPosition => _targetPosition;
        public Vector3 Velocity => _velocity;
        public Vector3 CurrentPosition => _transform?.position ?? Vector3.zero;
        public bool HasTarget => _hasTarget;
        public bool IsMoving => _hasTarget && Vector3.Distance(_transform.position, _targetPosition) > GetArrivalThreshold();
        public float MovementSpeedModifier 
        { 
            get => _movementSpeedModifier; 
            set => _movementSpeedModifier = Mathf.Clamp01(value); 
        }

        public MovementSystem(EnemyRuntimeStats stats, Transform transform, EnemyController enemyController = null)
        {
            _stats = stats;
            _transform = transform;
            _enemyController = enemyController;
        }
        
        /// <summary>
        /// Set the enemy controller reference (can be called after construction if needed)
        /// </summary>
        public void SetEnemyController(EnemyController enemyController)
        {
            _enemyController = enemyController;
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
            
            float distanceToTarget = direction.magnitude;
            direction = direction.normalized;
            
            // Apply minimum distance constraint to prevent collision issues
            // Stop moving if too close to target to avoid physics conflicts
            float arrivalThreshold = GetArrivalThreshold();
            if (distanceToTarget <= arrivalThreshold)
            {
                _velocity = Vector3.zero;
                return;
            }
            
            // Calculate movement
            float actualSpeed = moveSpeed * _movementSpeedModifier;
            Vector3 movement = direction * actualSpeed * deltaTime;
            
            // Ensure we don't overshoot and get too close
            if (movement.magnitude > (distanceToTarget - arrivalThreshold))
            {
                movement = direction * (distanceToTarget - arrivalThreshold);
            }
            
            // Apply movement
            _transform.position += movement;
            _velocity = movement / deltaTime;
            
            // Check if we've reached the target
            if (distanceToTarget <= arrivalThreshold)
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
            
            // Determine move speed based on current enemy state and action
            float moveSpeed = GetCurrentMoveSpeed();
            
            // If speed is 0 (stunned, attacking, dead, etc.), stop movement immediately
            if (moveSpeed <= 0f)
            {
                _velocity = Vector3.zero;
                // Debug.Log($"MovementSystem: Movement blocked - speed is 0 (State: {_enemyController?.CurrentState}, Action: {_enemyController?.ActionController?.CurrentActionName})");
                return;
            }
            
            MoveToTarget(moveSpeed, deltaTime);
        }
        
        /// <summary>
        /// Get the arrival threshold from enemy stats configuration
        /// </summary>
        private float GetArrivalThreshold()
        {
            // Get the configured arrival threshold from enemy stats
            return _stats.arrivalThreshold;
        }
        
        /// <summary>
        /// Get the appropriate movement speed based on current enemy state and action.
        /// This is the single source of truth for enemy movement speed calculation.
        /// 
        /// Speed Rules:
        /// 1. Action-based speeds (higher priority):
        ///    - Chase action: use chaseMoveSpeed
        ///    - Patrol action: use moveSpeed
        ///    - NormalAttack/WaveAttack/Revive actions: cannot move (speed = 0)
        /// 2. State-based speeds (when no specific action rule):
        ///    - Normal state: depends on current action (handled above)
        ///    - Stun state: cannot move (speed = 0)
        ///    - TrueDeath state: cannot move (speed = 0)
        ///    - Reviving state: cannot move (speed = 0, handled by Revive action)
        /// </summary>
        private float GetCurrentMoveSpeed()
        {
            // If no enemy controller reference, fall back to basic move speed
            if (_enemyController == null)
            {
                return _stats.GetModifiedMoveSpeed();
            }
            
            // State-based movement restrictions using unified state enum
            // BehaviorTree manages behavior, MovementSystem just checks state for speed
            
            var currentState = _enemyController.CurrentState;
            
            switch (currentState)
            {
                case EnemyState.Dead:
                    // No movement when dead (core destroyed)
                    return 0f;
                
                case EnemyState.Stunned:
                    // No movement when stunned
                    return 0f;
                
                case EnemyState.Reviving:
                    // No movement during revival
                    return 0f;
                
                case EnemyState.Normal:
                    // Normal state: movement speed based on target status
                    
                    // If player is in attack range, use slower speed (preparing to attack)
                    if (_enemyController.IsPlayerInAttackRange())
                    {
                        return _stats.GetModifiedMoveSpeed() * 0.5f;
                    }
                    
                    // If has target, use chase speed
                    if (_enemyController.HasPlayerTarget)
                    {
                        return _stats.GetModifiedChaseMoveSpeed();
                    }
                    
                    // Default to normal patrol speed
                    return _stats.GetModifiedMoveSpeed();
                
                default:
                    // Fallback for any unexpected state
                    return _stats.GetModifiedMoveSpeed();
            }
        }
        
        private void UpdateRotation(float deltaTime)
        {
            if (!IsMoving) return;
            
            // Check if enemy can actually move (not stunned, attacking, etc.)
            float moveSpeed = GetCurrentMoveSpeed();
            if (moveSpeed <= 0f) return;
            
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
        
        /// <summary>
        /// Check if enemy can move in current state/action
        /// </summary>
        public bool CanMove()
        {
            return GetCurrentMoveSpeed() > 0f;
        }
        
        /// <summary>
        /// Get the current effective movement speed (for debugging/display)
        /// </summary>
        public float GetEffectiveMoveSpeed()
        {
            return GetCurrentMoveSpeed();
        }
        
        #endregion
    }
}