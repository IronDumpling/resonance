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
        private InputActionMap _informationMap;
        private InputActionMap _waveMap;

        public int Priority => 10;
        public SystemState State { get; private set; } = SystemState.Uninitialized;

        public InputService(ServiceConfiguration configuration)
        {
            _inputActions = configuration.inputActions;
        }

        // Player Map events
        public event Action<Vector2> OnMove;
        public event Action OnInteract;   // Interact input (E key)
        public event Action OnWaveAttack; // Short press F (WaveAttackAction)
        public event Action<bool> OnHeal; // F key press/release (HealAction) - true for press, false for release
        public event Action<bool> OnRun;  // true when starting to run, false when stopping
        public event Action<bool> OnAim;  // true when starting to aim, false when stopping
        public event Action OnShoot;      // Shoot input (Mouse left button)
        public event Action<Vector2> OnLook;
        public event Action OnReload;     // Reload input (R key)
        
        // Inventory Map events
        public event Action OnOpenInventory;     // Open inventory (Player map Tab key)
        public event Action OnCloseInventory;    // Close inventory (Inventory map Tab key)
        public event Action<Vector2> OnMoveItem; // Move selected item (WASD in inventory mode)
        public event Action OnRotateItemLeft;    // Rotate item left (Q key in inventory mode)
        public event Action OnRotateItemRight;   // Rotate item right (E key in inventory mode)

        // Information Map events
        public event Action OnInformationClose; // Close information (E key)

        // Wave Map events
        public event Action OnQTE; // QTE input (F key during Wave mode)

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
            _informationMap = _inputActions.FindActionMap("Information");
            _waveMap = _inputActions.FindActionMap("Wave");
            
            SetupInputCallbacks();
            EnablePlayerInput();
            
            State = SystemState.Running;
            Debug.Log("InputService: Initialized successfully");
        }

        private void SetupInputCallbacks()
        {
            if (_playerMap == null || _inventoryMap == null || 
                _informationMap == null || _waveMap == null) return;

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

            _playerMap["OpenInventory"].performed += OnInventoryOpenPerformed;
            
            _playerMap["Reload"].performed += OnReloadPerformed;
            
            // Inventory input callbacks
            _inventoryMap["CloseInventory"].performed += OnInventoryClosePerformed;
            _inventoryMap["MoveItem"].performed += OnMoveItemPerformed;
            _inventoryMap["MoveItem"].canceled += OnMoveItemCanceled;
            _inventoryMap["RotateItemLeft"].performed += OnRotateItemLeftPerformed;
            _inventoryMap["RotateItemRight"].performed += OnRotateItemRightPerformed;

            // Information input callbacks
            _informationMap["CloseInformation"].performed += OnInformationClosePerformed;

            // Wave input callbacks
            _waveMap["QTE"].performed += OnQTEPerformed;
        }

        #region Player Map Input Callbacks
        
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
            OnWaveAttack?.Invoke();
            Debug.Log("InputService: Wave press performed"); 
        }

        private void OnHealStarted(InputAction.CallbackContext context)
        {
            OnHeal?.Invoke(true);
            Debug.Log("InputService: Heal key pressed (started)");
        }

        private void OnHealCanceled(InputAction.CallbackContext context)
        {
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

        private void OnReloadPerformed(InputAction.CallbackContext context)
        {
            OnReload?.Invoke();
            Debug.Log("InputService: Reload press performed");
        }
        
        private void OnInventoryOpenPerformed(InputAction.CallbackContext context)
        {
            OnOpenInventory?.Invoke();
            Debug.Log("InputService: Player map - Open inventory press performed");
        }

        #endregion

        #region Inventory Map Input Callbacks
        
        private void OnInventoryClosePerformed(InputAction.CallbackContext context)
        {
            OnCloseInventory?.Invoke();
            Debug.Log("InputService: Inventory map - Close inventory press performed");
        }
        
        private void OnMoveItemPerformed(InputAction.CallbackContext context)
        {
            Vector2 moveInput = context.ReadValue<Vector2>();
            OnMoveItem?.Invoke(moveInput);
        }
        
        private void OnMoveItemCanceled(InputAction.CallbackContext context)
        {
            OnMoveItem?.Invoke(Vector2.zero);
        }
        
        private void OnRotateItemLeftPerformed(InputAction.CallbackContext context)
        {
            OnRotateItemLeft?.Invoke();
            Debug.Log("InputService: Rotate item left press performed");
        }
        
        private void OnRotateItemRightPerformed(InputAction.CallbackContext context)
        {
            OnRotateItemRight?.Invoke();
            Debug.Log("InputService: Rotate item right press performed");
        }

        #endregion

        #region Information Map Input Callbacks

        private void OnInformationClosePerformed(InputAction.CallbackContext context)
        {
            OnInformationClose?.Invoke();
            Debug.Log("InputService: Information close press performed");
        }

        #endregion

        #region Wave Map Input Callbacks

        private void OnQTEPerformed(InputAction.CallbackContext context)
        {
            OnQTE?.Invoke();
            Debug.Log("InputService: QTE press performed");
        }

        #endregion

        #region Input Enabling/Disabling

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

        public void EnableInformationInput()
        {
            if (_informationMap != null)
            {
                _informationMap.Enable();
                Debug.Log("InputService: Information input enabled");
            }
        }

        public void DisableInformationInput()
        {
            if (_informationMap != null)
            {
                _informationMap.Disable();
                Debug.Log("InputService: Information input disabled");
            }
        }

        public void EnableWaveInput()
        {
            if (_waveMap != null)
            {
                _waveMap.Enable();
                Debug.Log("InputService: Wave input enabled");
            }
        }

        public void DisableWaveInput()
        {
            if (_waveMap != null)
            {
                _waveMap.Disable();
                Debug.Log("InputService: Wave input disabled");
            }
        }
        
        #endregion

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
            OnWaveAttack = null;
            OnHeal = null;
            OnRun = null;
            OnAim = null;
            OnShoot = null;
            OnLook = null;
            OnReload = null;
            OnOpenInventory = null;
            OnCloseInventory = null;
            OnMoveItem = null;
            OnRotateItemLeft = null;
            OnRotateItemRight = null;
            OnInformationClose = null;
            OnQTE = null;

            State = SystemState.Shutdown;
        }

        // Note: Since this is no longer a MonoBehaviour, cleanup is handled through Shutdown()
    }
}
