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

        // Properties
        public Vector2 InputVector => _inputVector;
        public bool IsRunning => _isRunning;
        public bool IsGrounded => _isGrounded;
        public Vector3 Velocity => _velocity;
        public bool IsMoving => _inputVector.sqrMagnitude > 0.01f;

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
                // Get the appropriate movement speed based on current state, action, and health tier
                // This is the single source of truth for player movement speed
                float speed = GetCurrentMoveSpeed();

                movement.x = _inputVector.x * speed * deltaTime;
                movement.z = _inputVector.y * speed * deltaTime; // Y input maps to Z movement
            }

            return movement;
        }
        
        /// <summary>
        /// Get the appropriate movement speed based on current player state, action, and health tier.
        /// This is the single source of truth for player movement speed calculation.
        /// 
        /// Speed Rules:
        /// 1. Normal State: can walk or run
        /// 2. Aiming State: can only walk at aimMoveSpeed
        /// 3. Reload Action: can only walk at reloadMoveSpeed
        /// 4. Interact/Heal Actions, Stun/Death States: cannot move (speed = 0)
        /// 5. All speeds are multiplied by health tier multiplier at the end
        /// </summary>
        private float GetCurrentMoveSpeed()
        {
            // If no player controller reference, fall back to basic walk/run speed
            if (_playerController == null)
            {
                return _isRunning ? _stats.runSpeed : _stats.walkSpeed;
            }
            
            // Get health tier speed multiplier (Healthy/Injured: 1.0x, Wounded: 0.7x, Critical: 0.4x)
            float healthTierMultiplier = HealthTierHelper.GetSpeedMultiplier(_playerController.HealthTier);
            
            // Check current player state
            string currentState = _playerController.CurrentState;
            
            // Rule 4: Death state - no movement
            if (currentState == "Death")
            {
                return 0f;
            }
            
            // Rule 4: Stun state - no movement
            if (currentState == "Stun")
            {
                return 0f;
            }
            
            // Check current action (actions have higher priority than state for speed)
            string currentAction = _playerController.GetCurrentActionName();
            
            // Rule 4: Interact action - no movement (BlocksMovement = true)
            if (currentAction == "Interact")
            {
                return 0f;
            }
            
            // Rule 4: Heal action - no movement (BlocksMovement = true)
            if (currentAction == "Heal")
            {
                return 0f;
            }
            
            // Rule 3: Reload action - can only walk at reloadMoveSpeed
            if (currentAction == "Reload")
            {
                return _stats.reloadMoveSpeed * healthTierMultiplier;
            }
            
            // Rule 2: Aiming state - can only walk at aimMoveSpeed
            if (currentState == "Aiming")
            {
                return _stats.aimMoveSpeed * healthTierMultiplier;
            }
            
            // Rule 1: Normal state - can walk or run
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
        /// Get current effective movement speed (includes state, action, and health tier)
        /// Used for animation and external queries
        /// </summary>
        public float GetMovementSpeed()
        {
            return GetCurrentMoveSpeed();
        }

        #endregion
    }
}
