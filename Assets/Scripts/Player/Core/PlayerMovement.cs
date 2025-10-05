using UnityEngine;
using Resonance.Player.Data;

namespace Resonance.Player.Core
{
    /// <summary>
    /// Player movement system that handles 2D platform movement.
    /// Works with PlayerMonoBehaviour to control the player GameObject.
    /// References PlayerController to access current state and apply state-based speed modifiers.
    /// </summary>
    public class PlayerMovement
    {
        private PlayerRuntimeStats _stats;
        private PlayerController _playerController; // Reference to get current state info
        private Vector2 _inputVector;
        private bool _isRunning;
        private bool _isGrounded;

        // State
        private Vector3 _velocity;

        // Movement modifier for external speed adjustments (e.g., from PlayerDeathState)
        private float _movementSpeedModifier = 1f;

        // Properties
        public Vector2 InputVector => _inputVector;
        public bool IsRunning => _isRunning;
        public bool IsGrounded => _isGrounded;
        public Vector3 Velocity => _velocity;
        public bool IsMoving => _inputVector.sqrMagnitude > 0.01f;
        public float MovementSpeedModifier 
        { 
            get => _movementSpeedModifier; 
            set => _movementSpeedModifier = Mathf.Clamp01(value); 
        }

        public PlayerMovement(PlayerRuntimeStats stats)
        {
            _stats = stats;
        }
        
        /// <summary>
        /// Set the player controller reference (called after construction)
        /// </summary>
        public void SetPlayerController(PlayerController playerController)
        {
            _playerController = playerController;
        }

        public void Update(float deltaTime)
        {

        }

        #region Input Handling

        public void SetMovementInput(Vector2 input)
        {
            _inputVector = input;
        }

        public void SetRunning(bool isRunning)
        {
            _isRunning = isRunning;
        }

        #endregion

        #region Movement Calculation

        public Vector3 CalculateMovement(float deltaTime)
        {
            Vector3 movement = Vector3.zero;

            // Horizontal movement (2D platform style)
            if (IsMoving)
            {
                // Get the appropriate movement speed based on current state and health tier
                float speed = GetCurrentMoveSpeed();

                // Apply external movement speed modifier (e.g., from PlayerDeathState setting it to 0)
                speed *= _movementSpeedModifier;

                movement.x = _inputVector.x * speed * deltaTime;
                movement.z = _inputVector.y * speed * deltaTime; // Y input maps to Z movement
            }

            return movement;
        }
        
        /// <summary>
        /// Get the appropriate movement speed based on current player state, action, and health tier
        /// Similar to EnemyMovement.GetCurrentMoveSpeed()
        /// </summary>
        private float GetCurrentMoveSpeed()
        {
            // If no player controller reference, fall back to basic walk/run speed
            if (_playerController == null)
            {
                return _isRunning ? _stats.runSpeed : _stats.walkSpeed;
            }
            
            // Get health tier speed multiplier (Wounded: 0.7x, Critical: 0.4x)
            float healthTierMultiplier = HealthTierHelper.GetSpeedMultiplier(_playerController.HealthTier);
            
            // Check current player state and apply appropriate base speed
            string currentState = _playerController.CurrentState;
            
            // Death state - no movement
            if (currentState == "Death")
            {
                return 0f;
            }
            
            // Aiming state - use aim move speed
            if (currentState == "Aiming")
            {
                return _stats.aimMoveSpeed * healthTierMultiplier;
            }
            
            // Check if player is performing Reload action
            string currentAction = _playerController.GetCurrentActionName();
            if (currentAction == "Reload")
            {
                return _stats.reloadMoveSpeed * healthTierMultiplier;
            }
            
            // Normal state - use walk/run speed
            float baseSpeed = _isRunning ? _stats.runSpeed : _stats.walkSpeed;
            return baseSpeed * healthTierMultiplier;
        }

        public Vector3 CalculateVelocity(Vector3 currentVelocity, float deltaTime)
        {
            _velocity = currentVelocity;
            // For 2D platform games, we primarily use CharacterController.Move()
            // Velocity is mainly used for gravity
            return _velocity;
        }

        #endregion

        #region Ground System

        public void SetGrounded(bool grounded)
        {
            _isGrounded = grounded;
        }

        #endregion

        #region State Queries

        /// <summary>
        /// Get current effective movement speed (includes state, health tier, and modifier)
        /// Used for animation and external queries
        /// </summary>
        public float GetMovementSpeed()
        {
            float speed = GetCurrentMoveSpeed();
            speed *= _movementSpeedModifier;
            return speed;
        }

        #endregion
    }
}
