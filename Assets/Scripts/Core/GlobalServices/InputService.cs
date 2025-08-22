using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Resonance.Core;

namespace Resonance.Core.GlobalServices
{
    public class InputService : IInputService
    {
        private InputActionAsset _inputActions;
        private InputActionMap _playerMap;
        private InputActionMap _uiMap;
        private bool _isEnabled = true;

        public int Priority => 10;
        public SystemState State { get; private set; } = SystemState.Uninitialized;
        public bool IsEnabled 
        { 
            get => _isEnabled; 
            set 
            { 
                _isEnabled = value;
                if (_isEnabled) EnablePlayerInput();
                else DisablePlayerInput();
            } 
        }

        public InputService(ServiceConfiguration configuration)
        {
            _inputActions = configuration.inputActions;
        }

        // Input events
        public event Action<Vector2> OnMove;
        public event Action<Vector2> OnLook;
        public event Action OnAttack;
        public event Action OnJump;
        public event Action OnCrouch;
        public event Action OnSprint;
        public event Action OnInteract;
        public event Action OnNext;
        public event Action OnPrevious;

        public void Initialize()
        {
            if (State != SystemState.Uninitialized)
            {
                Debug.LogWarning("InputService already initialized");
                return;
            }

            State = SystemState.Initializing;
            Debug.Log("InputService: Initializing");

            if (_inputActions == null)
            {
                Debug.LogError("InputService: InputActionAsset is null. Make sure ServiceConfiguration is properly set up.");
                return;
            }

            _playerMap = _inputActions.FindActionMap("Player");
            _uiMap = _inputActions.FindActionMap("UI");
            
            SetupInputCallbacks();
            EnablePlayerInput();
            
            State = SystemState.Running;
            Debug.Log("InputService: Initialized successfully");
        }

        private void SetupInputCallbacks()
        {
            // Player input callbacks
            _playerMap["Move"].performed += OnMovePerformed;
            _playerMap["Move"].canceled += OnMoveCanceled;
            
            _playerMap["Look"].performed += OnLookPerformed;
            _playerMap["Look"].canceled += OnLookCanceled;
            
            _playerMap["Attack"].performed += OnAttackPerformed;
            _playerMap["Jump"].performed += OnJumpPerformed;
            _playerMap["Crouch"].performed += OnCrouchPerformed;
            _playerMap["Sprint"].performed += OnSprintPerformed;
            _playerMap["Interact"].performed += OnInteractPerformed;
            _playerMap["Next"].performed += OnNextPerformed;
            _playerMap["Previous"].performed += OnPreviousPerformed;
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            Vector2 moveInput = context.ReadValue<Vector2>();
            OnMove?.Invoke(moveInput);
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            OnMove?.Invoke(Vector2.zero);
        }

        private void OnLookPerformed(InputAction.CallbackContext context)
        {
            Vector2 lookInput = context.ReadValue<Vector2>();
            OnLook?.Invoke(lookInput);
        }

        private void OnLookCanceled(InputAction.CallbackContext context)
        {
            OnLook?.Invoke(Vector2.zero);
        }

        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            OnAttack?.Invoke();
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            OnJump?.Invoke();
        }

        private void OnCrouchPerformed(InputAction.CallbackContext context)
        {
            OnCrouch?.Invoke();
        }

        private void OnSprintPerformed(InputAction.CallbackContext context)
        {
            OnSprint?.Invoke();
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            OnInteract?.Invoke();
        }

        private void OnNextPerformed(InputAction.CallbackContext context)
        {
            OnNext?.Invoke();
        }

        private void OnPreviousPerformed(InputAction.CallbackContext context)
        {
            OnPrevious?.Invoke();
        }

        public void EnablePlayerInput()
        {
            if (_playerMap != null)
            {
                _playerMap.Enable();
                Debug.Log("InputService: Player input enabled");
            }
        }

        public void DisablePlayerInput()
        {
            if (_playerMap != null)
            {
                _playerMap.Disable();
                Debug.Log("InputService: Player input disabled");
            }
        }

        public void EnableUIInput()
        {
            if (_uiMap != null)
            {
                _uiMap.Enable();
                Debug.Log("InputService: UI input enabled");
            }
        }

        public void DisableUIInput()
        {
            if (_uiMap != null)
            {
                _uiMap.Disable();
                Debug.Log("InputService: UI input disabled");
            }
        }

        public void Shutdown()
        {
            if (State == SystemState.Shutdown)
                return;

            Debug.Log("InputService: Shutting down");
            
            if (_inputActions != null)
            {
                _inputActions.Disable();
                _inputActions.Dispose();
                _inputActions = null;
            }

            // Clear all event listeners
            OnMove = null;
            OnLook = null;
            OnAttack = null;
            OnJump = null;
            OnCrouch = null;
            OnSprint = null;
            OnInteract = null;
            OnNext = null;
            OnPrevious = null;

            State = SystemState.Shutdown;
        }

        // Note: Since this is no longer a MonoBehaviour, cleanup is handled through Shutdown()
    }
}
