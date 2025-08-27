using UnityEngine;
using Resonance.Player.Data;

namespace Resonance.Player.Core
{
    /// <summary>
    /// Player movement system that handles 2D platform movement.
    /// Works with PlayerMonoBehaviour to control the player GameObject.
    /// </summary>
    public class PlayerMovement
    {
        private PlayerRuntimeStats _stats;
        private Vector2 _inputVector;
        private bool _isRunning;
        private bool _isGrounded;

        // State
        private Vector3 _velocity;

        // Movement modifier for aiming state
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
                float speed = _stats.moveSpeed;
                if (_isRunning)
                {
                    speed *= _stats.runSpeedMultiplier;
                }

                // Apply movement speed modifier (for aiming state)
                speed *= _movementSpeedModifier;

                movement.x = _inputVector.x * speed * deltaTime;
                movement.z = _inputVector.y * speed * deltaTime; // Y input maps to Z movement
            }

            return movement;
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

        public float GetMovementSpeed()
        {
            float speed = _stats.moveSpeed;
            if (_isRunning)
            {
                speed *= _stats.runSpeedMultiplier;
            }
            speed *= _movementSpeedModifier;
            return speed;
        }

        #endregion
    }
}
