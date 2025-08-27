using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Resonance.Core;   
using Resonance.Interfaces.Services;

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
        public event Action OnInteract;
        public event Action OnResonance; // Short press F (ResonanceAction)
        public event Action OnRecover; // Long press F (RecoverAction)
        public event Action<bool> OnRun; // true when starting to run, false when stopping
        public event Action<bool> OnAim; // true when starting to aim, false when stopping
        public event Action OnShoot;
        public event Action<Vector2> OnLook;

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
            if (_playerMap == null) return;

            // Player input callbacks
            _playerMap["Move"].performed += OnMovePerformed;
            _playerMap["Move"].canceled += OnMoveCanceled;
            
            _playerMap["Interact"].performed += OnInteractPerformed;
            
            _playerMap["Resonance"].performed += OnResonancePerformed;
            _playerMap["Recover"].performed += OnRecoverPerformed;
            
            _playerMap["Run"].started += OnRunStarted;
            _playerMap["Run"].canceled += OnRunCanceled;
            
            _playerMap["Aim"].started += OnAimStarted;
            _playerMap["Aim"].canceled += OnAimCanceled;
            
            _playerMap["Shoot"].performed += OnShootPerformed;
            
            _playerMap["Look"].performed += OnLookPerformed;
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

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            OnInteract?.Invoke();
        }

        private void OnResonancePerformed(InputAction.CallbackContext context)
        {
            OnResonance?.Invoke();
            Debug.Log("InputService: Resonance press performed");
        }

        private void OnRecoverPerformed(InputAction.CallbackContext context)
        {
            OnRecover?.Invoke();
            Debug.Log("InputService: Recover press performed");
        }

        private void OnRunStarted(InputAction.CallbackContext context)
        {
            OnRun?.Invoke(true);
        }

        private void OnRunCanceled(InputAction.CallbackContext context)
        {
            OnRun?.Invoke(false);
        }

        private void OnAimStarted(InputAction.CallbackContext context)
        {
            OnAim?.Invoke(true);
        }

        private void OnAimCanceled(InputAction.CallbackContext context)
        {
            OnAim?.Invoke(false);
        }

        private void OnShootPerformed(InputAction.CallbackContext context)
        {
            OnShoot?.Invoke();
        }

        private void OnLookPerformed(InputAction.CallbackContext context)
        {
            Vector2 lookInput = context.ReadValue<Vector2>();
            OnLook?.Invoke(lookInput);
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
                _inputActions = null;
            }

            // Clear all event listeners
            OnMove = null;
            OnInteract = null;
            OnResonance = null;
            OnRecover = null;
            OnRun = null;
            OnAim = null;
            OnShoot = null;
            OnLook = null;

            State = SystemState.Shutdown;
        }

        // Note: Since this is no longer a MonoBehaviour, cleanup is handled through Shutdown()
    }
}
