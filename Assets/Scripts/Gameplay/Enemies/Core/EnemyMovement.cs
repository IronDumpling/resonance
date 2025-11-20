using UnityEngine;
using UnityEngine.AI;
using Resonance.Gameplay.Enemies.Data;
using Resonance.Gameplay.Enemies.Core;

namespace Resonance.Gameplay.Enemies.Movement
{
    /// <summary>
    /// Enemy movement system using Unity NavMeshAgent
    /// Manages enemy movement with NavMeshAgent and applies configuration from EnemyRuntimeStats
    /// 
    /// Movement speed is determined by:
    /// - Current enemy state (Normal, Unbalanced, CoreExposed, Dead, Stagger)
    /// - Current action (Chase, Patrol, Attack)
    /// - Health tier modifiers (from EnemyBaseStats)
    /// </summary>
    public class EnemyMovement
    {
        private EnemyRuntimeStats _stats;
        private Transform _transform;
        private NavMeshAgent _navAgent;
        private EnemyController _enemyController;
        
        // Events
        public System.Action OnTargetReached;
        
        // Properties
        public Vector3 TargetPosition => _navAgent != null && _navAgent.hasPath ? _navAgent.destination : _transform.position;
        public Vector3 Velocity => _navAgent != null ? _navAgent.velocity : Vector3.zero;
        public Vector3 CurrentPosition => _transform?.position ?? Vector3.zero;
        public bool HasTarget => _navAgent != null && _navAgent.hasPath;
        public bool IsMoving => _navAgent != null && _navAgent.hasPath && _navAgent.remainingDistance > _navAgent.stoppingDistance && !_navAgent.isStopped;

        public EnemyMovement(EnemyRuntimeStats stats, Transform transform, EnemyController enemyController = null)
        {
            _stats = stats;
            _transform = transform;
            _enemyController = enemyController;
            
            // Get or add NavMeshAgent component
            _navAgent = _transform.GetComponent<NavMeshAgent>();
            if (_navAgent == null)
            {
                Debug.LogError($"EnemyMovement: No NavMeshAgent found on {_transform.name}! Please add NavMeshAgent component.");
            }
            else
            {
                InitializeNavAgent();
            }
        }
        
        /// <summary>
        /// Initialize NavMeshAgent with configuration from stats
        /// </summary>
        private void InitializeNavAgent()
        {
            if (_navAgent == null) return;
            
            // Apply movement configuration
            _navAgent.speed = _stats.moveSpeed;
            _navAgent.acceleration = _stats.acceleration;
            _navAgent.angularSpeed = _stats.angularSpeed;
            _navAgent.stoppingDistance = _stats.stoppingDistance;
            _navAgent.autoBraking = _stats.autoBraking;
            
            // Disable auto-update for position and rotation - we'll control this
            _navAgent.updateRotation = true;
            _navAgent.updateUpAxis = false;
            
            // Movement constraints: no jumping, no climbing
            _navAgent.autoTraverseOffMeshLink = false;
            _navAgent.height = 2f;
            _navAgent.baseOffset = _stats.baseOffset;
            
            Debug.Log($"EnemyMovement: NavMeshAgent initialized with speed={_navAgent.speed}, acceleration={_navAgent.acceleration}, angularSpeed={_navAgent.angularSpeed}");
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
            if (_navAgent == null || _transform == null) return;
            
            // Update speed based on current state
            UpdateNavAgentSpeed();
            
            // Check if reached destination
            if (_navAgent.hasPath && !_navAgent.pathPending)
            {
                if (_navAgent.remainingDistance <= _navAgent.stoppingDistance)
                {
                    if (!_navAgent.hasPath || _navAgent.velocity.sqrMagnitude == 0f)
                    {
                        OnTargetReached?.Invoke();
                    }
                }
            }
        }
        
        #region Movement Control
        
        /// <summary>
        /// Set a target position to move towards
        /// </summary>
        public void SetTarget(Vector3 targetPosition)
        {
            if (_navAgent == null || !_navAgent.isOnNavMesh) return;
            
            // Update speed before setting destination
            UpdateNavAgentSpeed();
            
            // Set destination
            _navAgent.isStopped = false;
            _navAgent.SetDestination(targetPosition);
        }
        
        /// <summary>
        /// Stop movement
        /// </summary>
        public void Stop()
        {
            if (_navAgent == null || !_navAgent.isOnNavMesh) return;
            
            _navAgent.isStopped = true;
            _navAgent.ResetPath();
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Update NavMeshAgent speed based on current enemy state and action
        /// </summary>
        private void UpdateNavAgentSpeed()
        {
            if (_navAgent == null) return;
            
            float targetSpeed = GetCurrentMoveSpeed();
            _navAgent.speed = targetSpeed;
            
            // If speed is 0, stop the agent
            if (targetSpeed <= 0f && _navAgent.isOnNavMesh)
            {
                _navAgent.isStopped = true;
            }
        }
        
        /// <summary>
        /// Get the appropriate movement speed based on current enemy state and action.
        /// This is the single source of truth for enemy movement speed calculation.
        /// 
        /// Speed Rules:
        /// 1. State-based speeds (highest priority):
        ///    - Dead state: cannot move (speed = 0)
        ///    - Staggered state: cannot move (speed = 0)
        ///    - Reviving state: cannot move (speed = 0)
        /// 2. Action-based speeds (when in Normal state):
        ///    - Has target: use chase speed
        ///    - Default: use normal patrol speed
        /// 3. Health tier modifier is automatically applied by GetModifiedMoveSpeed() and GetModifiedChaseMoveSpeed()
        /// </summary>
        private float GetCurrentMoveSpeed()
        {
            // If no enemy controller reference, fall back to basic move speed
            if (_enemyController == null)
            {
                return _stats.GetModifiedMoveSpeed();
            }
            
            var currentState = _enemyController.CurrentState;
            
            switch (currentState)
            {
                case EnemyState.Dead:
                    // No movement when dead (core destroyed)
                    return 0f;
                
                case EnemyState.Staggered:
                    // No movement when staggerned
                    return 0f;
                
                case EnemyState.Unbalanced:
                    // No movement when unbalanced
                    return 0f;
                
                case EnemyState.CoreExposed:
                    // No movement during core exposed
                    return 0f;
                
                case EnemyState.Normal:
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
        
        #endregion
        
        #region Public Utilities
        
        /// <summary>
        /// Get distance to current target
        /// </summary>
        public float GetDistanceToTarget()
        {
            if (_navAgent == null || !_navAgent.hasPath) return float.MaxValue;
            return _navAgent.remainingDistance;
        }
        
        /// <summary>
        /// Get direction to current target (normalized, XZ plane only)
        /// </summary>
        public Vector3 GetDirectionToTarget()
        {
            if (_navAgent == null || !_navAgent.hasPath) return Vector3.zero;
            
            Vector3 direction = (_navAgent.destination - _transform.position);
            direction.y = 0f;
            return direction.normalized;
        }
        
        /// <summary>
        /// Check if enemy can reach target position
        /// </summary>
        public bool CanReachTarget(Vector3 target)
        {
            if (_navAgent == null) return false;
            
            NavMeshPath path = new NavMeshPath();
            if (_navAgent.CalculatePath(target, path))
            {
                return path.status == NavMeshPathStatus.PathComplete;
            }
            return false;
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
        
        /// <summary>
        /// Get NavMeshAgent reference (for direct access if needed)
        /// </summary>
        public NavMeshAgent GetNavAgent()
        {
            return _navAgent;
        }
        
        /// <summary>
        /// Check if NavMeshAgent is ready and on NavMesh
        /// </summary>
        public bool IsNavAgentReady()
        {
            return _navAgent != null && _navAgent.isOnNavMesh;
        }
        
        #endregion
    }
}
