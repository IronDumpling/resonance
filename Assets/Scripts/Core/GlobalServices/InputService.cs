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
        private InputActionMap _inventoryMap;
        private bool _isEnabled = true;
        
        // Wave mode control (Risk mitigation: Input conflict resolution)
        public bool IsWaveMode { get; set; } = false;
        
        // Inventory mode control
        public bool IsInventoryMode { get; set; } = false;

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
        public event Action OnWave; // Short press F (WaveAction)
        public event Action<bool> OnHeal; // F key press/release (HealAction) - true for press, false for release
        public event Action<bool> OnRun; // true when starting to run, false when stopping
        public event Action<bool> OnAim; // true when starting to aim, false when stopping
        public event Action OnShoot;
        public event Action<Vector2> OnLook;
        public event Action OnQTE; // QTE input (F key during Wave mode)
        public event Action OnReload; // Reload input (R key)
        
        // Inventory events
        public event Action OnOpenInventory; // Open inventory (Player map Tab key)
        public event Action OnCloseInventory; // Close inventory (Inventory map Tab key)
        public event Action<Vector2> OnMoveItem; // Move selected item (WASD in inventory mode)
        public event Action OnRotateItemLeft; // Rotate item left (Q key in inventory mode)
        public event Action OnRotateItemRight; // Rotate item right (E key in inventory mode)

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
            _inventoryMap = _inputActions.FindActionMap("Inventory");
            
            SetupInputCallbacks();
            EnablePlayerInput();
            
            State = SystemState.Running;
            Debug.Log("InputService: Initialized successfully");
        }

        private void SetupInputCallbacks()
        {
            if (_playerMap == null || _inventoryMap == null) return;

            // Player input callbacks
            _playerMap["Move"].performed += OnMovePerformed;
            _playerMap["Move"].canceled += OnMoveCanceled;
            
            _playerMap["Interact"].performed += OnInteractPerformed;
            
            _playerMap["Wave"].performed += OnWavePerformed;
            _playerMap["Heal"].started += OnHealStarted;
            _playerMap["Heal"].canceled += OnHealCanceled;
            
            _playerMap["Run"].started += OnRunStarted;
            _playerMap["Run"].canceled += OnRunCanceled;
            
            _playerMap["Aim"].started += OnAimStarted;
            _playerMap["Aim"].canceled += OnAimCanceled;
            
            _playerMap["Shoot"].performed += OnShootPerformed;
            _playerMap["Look"].performed += OnLookPerformed;

            _playerMap["OpenInventory"].performed += OnPlayerOpenInventoryPerformed;
            
            _playerMap["QTE"].performed += OnQTEPerformed;
            _playerMap["Reload"].performed += OnReloadPerformed;
            
            // Inventory input callbacks
            _inventoryMap["CloseInventory"].performed += OnInventoryCloseInventoryPerformed;
            _inventoryMap["MoveItem"].performed += OnMoveItemPerformed;
            _inventoryMap["MoveItem"].canceled += OnMoveItemCanceled;
            _inventoryMap["RotateItemLeft"].performed += OnRotateItemLeftPerformed;
            _inventoryMap["RotateItemRight"].performed += OnRotateItemRightPerformed;
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

        private void OnWavePerformed(InputAction.CallbackContext context)
        {
            // Risk mitigation: Input conflict resolution - only trigger if not in Wave mode
            if (IsWaveMode) return;
            
            OnWave?.Invoke();
            Debug.Log("InputService: Wave press performed"); 
        }

        private void OnHealStarted(InputAction.CallbackContext context)
        {
            // Risk mitigation: Input conflict resolution - only trigger if not in Wave mode
            if (IsWaveMode) return;
            
            OnHeal?.Invoke(true);
            Debug.Log("InputService: Heal key pressed (started)");
        }

        private void OnHealCanceled(InputAction.CallbackContext context)
        {
            // Risk mitigation: Input conflict resolution - only trigger if not in Wave mode
            if (IsWaveMode) return;
            
            OnHeal?.Invoke(false);
            Debug.Log("InputService: Heal key released (canceled)");
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

        private void OnQTEPerformed(InputAction.CallbackContext context)
        {
            // Risk mitigation: Input conflict resolution - only trigger if in Wave mode
            if (!IsWaveMode) return;
            
            OnQTE?.Invoke();
            Debug.Log("InputService: QTE press performed");
        }

        private void OnReloadPerformed(InputAction.CallbackContext context)
        {
            // Risk mitigation: Input conflict resolution - only trigger if not in Wave mode
            if (IsWaveMode) return;
            
            OnReload?.Invoke();
            Debug.Log("InputService: Reload press performed");
        }
        
        private void OnPlayerOpenInventoryPerformed(InputAction.CallbackContext context)
        {
            // Risk mitigation: Input conflict resolution - only trigger if not in Wave mode
            if (IsWaveMode) return;
            
            OnOpenInventory?.Invoke();
            Debug.Log("InputService: Player map - Open inventory press performed");
        }
        
        private void OnInventoryCloseInventoryPerformed(InputAction.CallbackContext context)
        {
            // Only trigger if in inventory mode
            if (!IsInventoryMode) return;
            
            OnCloseInventory?.Invoke();
            Debug.Log("InputService: Inventory map - Close inventory press performed");
        }
        
        private void OnMoveItemPerformed(InputAction.CallbackContext context)
        {
            // Only trigger if in inventory mode
            if (!IsInventoryMode) return;
            
            Vector2 moveInput = context.ReadValue<Vector2>();
            OnMoveItem?.Invoke(moveInput);
        }
        
        private void OnMoveItemCanceled(InputAction.CallbackContext context)
        {
            // Only trigger if in inventory mode
            if (!IsInventoryMode) return;
            
            OnMoveItem?.Invoke(Vector2.zero);
        }
        
        private void OnRotateItemLeftPerformed(InputAction.CallbackContext context)
        {
            // Only trigger if in inventory mode
            if (!IsInventoryMode) return;
            
            OnRotateItemLeft?.Invoke();
            Debug.Log("InputService: Rotate item left press performed");
        }
        
        private void OnRotateItemRightPerformed(InputAction.CallbackContext context)
        {
            // Only trigger if in inventory mode
            if (!IsInventoryMode) return;
            
            OnRotateItemRight?.Invoke();
            Debug.Log("InputService: Rotate item right press performed");
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

        public void EnableInventoryInput()
        {
            if (_inventoryMap != null)
            {
                _inventoryMap.Enable();
                Debug.Log("InputService: Inventory input enabled");
            }
        }

        public void DisableInventoryInput()
        {
            if (_inventoryMap != null)
            {
                _inventoryMap.Disable();
                Debug.Log("InputService: Inventory input disabled");
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
            OnWave = null;
            OnHeal = null;
            OnRun = null;
            OnAim = null;
            OnShoot = null;
            OnLook = null;
            OnQTE = null;
            OnReload = null;
            OnOpenInventory = null;
            OnCloseInventory = null;
            OnMoveItem = null;
            OnRotateItemLeft = null;
            OnRotateItemRight = null;

            State = SystemState.Shutdown;
        }

        // Note: Since this is no longer a MonoBehaviour, cleanup is handled through Shutdown()
    }
}
