using UnityEngine;
using Resonance.Player.Data;

namespace Resonance.Player.Core
{
    /// <summary>
    /// Player movement system that handles physics-based movement.
    /// Works with PlayerMonoBehaviour to control the player GameObject.
    /// </summary>
    public class PlayerMovement
    {
        private PlayerRuntimeStats _stats;
        private Vector2 _inputVector;
        private bool _isRunning;
        private bool _jumpRequested;
        private int _jumpsRemaining;
        private bool _isGrounded;

        // State
        private Vector3 _velocity;
        private bool _wasGroundedLastFrame;

        // Events
        public System.Action OnJump;
        public System.Action OnLand;

        // Properties
        public Vector2 InputVector => _inputVector;
        public bool IsRunning => _isRunning;
        public bool IsGrounded => _isGrounded;
        public Vector3 Velocity => _velocity;
        public bool IsMoving => _inputVector.sqrMagnitude > 0.01f;

        public PlayerMovement(PlayerRuntimeStats stats)
        {
            _stats = stats;
            _jumpsRemaining = _stats.maxJumps;
        }

        public void Update(float deltaTime)
        {
            UpdateGroundedState();
            UpdateJumpState();
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

        public void RequestJump()
        {
            _jumpRequested = true;
        }

        #endregion

        #region Movement Calculation

        public Vector3 CalculateMovement(float deltaTime)
        {
            Vector3 movement = Vector3.zero;

            // Horizontal movement
            if (IsMoving)
            {
                float speed = _stats.moveSpeed;
                if (_isRunning)
                {
                    speed *= _stats.runSpeedMultiplier;
                }

                movement.x = _inputVector.x * speed * deltaTime;
                movement.z = _inputVector.y * speed * deltaTime; // Y input maps to Z movement
            }

            return movement;
        }

        public Vector3 CalculateVelocity(Vector3 currentVelocity, float deltaTime)
        {
            _velocity = currentVelocity;

            // Handle jumping
            if (_jumpRequested && CanJump())
            {
                PerformJump();
            }

            _jumpRequested = false;
            return _velocity;
        }

        #endregion

        #region Jump System

        private void UpdateGroundedState()
        {
            _wasGroundedLastFrame = _isGrounded;
            // Grounded state should be set externally by PlayerMonoBehaviour
            // after checking collision with ground
        }

        public void SetGrounded(bool grounded)
        {
            _isGrounded = grounded;

            // Reset jumps when landing
            if (_isGrounded && !_wasGroundedLastFrame)
            {
                _jumpsRemaining = _stats.maxJumps;
                OnLand?.Invoke();
            }
        }

        private void UpdateJumpState()
        {
            // Handle landing detection and jump reset
            if (_isGrounded && !_wasGroundedLastFrame)
            {
                _jumpsRemaining = _stats.maxJumps;
            }
        }

        private bool CanJump()
        {
            return _jumpsRemaining > 0;
        }

        private void PerformJump()
        {
            _velocity.y = _stats.jumpForce;
            _jumpsRemaining--;
            OnJump?.Invoke();
            
            Debug.Log($"PlayerMovement: Jumped! Jumps remaining: {_jumpsRemaining}");
        }

        #endregion

        #region State Queries

        public bool IsFalling()
        {
            return !_isGrounded && _velocity.y < -0.1f;
        }

        public bool IsRising()
        {
            return !_isGrounded && _velocity.y > 0.1f;
        }

        public float GetMovementSpeed()
        {
            float speed = _stats.moveSpeed;
            if (_isRunning)
            {
                speed *= _stats.runSpeedMultiplier;
            }
            return speed;
        }

        #endregion
    }
}
