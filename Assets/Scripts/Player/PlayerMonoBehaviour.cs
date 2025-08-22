using UnityEngine;
using UnityEngine.InputSystem;
using Resonance.Player.Core;
using Resonance.Player.Data;
using Resonance.Core;
using Resonance.Utilities;

namespace Resonance.Player
{
    /// <summary>
    /// MonoBehaviour component that handles Unity-specific player functionality.
    /// Acts as a bridge between Unity's GameObject system and the player logic.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMonoBehaviour : MonoBehaviour
    {
        [Header("Player Configuration")]
        [SerializeField] private PlayerBaseStats _baseStats;

        [Header("Physics")]
        [SerializeField] private LayerMask _groundLayerMask = 1;
        

        
        [Header("Edge Protection")]
        [SerializeField] private bool _enableEdgeProtection = true;
        [SerializeField] private float _edgeDetectionDistance = 1f;
        [SerializeField] private LayerMask _edgeDetectionLayerMask = 1;
        [SerializeField] private float _edgeRaycastHeight = 0.5f;

        [Header("Right Arm Animation")]
        [SerializeField] private Transform _rightArm;
        [SerializeField] private float _armRotationSpeed = 5f;
        [SerializeField] private Camera _playerCamera;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;

        // Components
        private CharacterController _characterController;
        private PlayerController _playerController;
        private IInputService _inputService;

        // Physics
        private bool _isGrounded;
        
        // Edge Protection
        private bool _canMoveForward = true;
        private bool _canMoveBackward = true;
        private bool _canMoveLeft = true;
        private bool _canMoveRight = true;

        // Events
        public System.Action<PlayerController> OnPlayerInitialized;

        // Properties
        public PlayerController Controller => _playerController;
        public bool IsInitialized => _playerController != null;

        #region Unity Lifecycle

        void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            
            if (_baseStats == null)
            {
                Debug.LogError("PlayerMonoBehaviour: BaseStats not assigned!");
                return;
            }

            InitializePlayer();
        }

        void Start()
        {
            // Get input service
            _inputService = ServiceRegistry.Get<IInputService>();
            if (_inputService == null)
            {
                Debug.LogError("PlayerMonoBehaviour: InputService not found!");
                return;
            }

            // Auto-detect camera if not assigned
            if (_playerCamera == null)
            {
                _playerCamera = Camera.main;
                if (_playerCamera == null)
                {
                    _playerCamera = FindAnyObjectByType<Camera>();
                }
            }

            // Subscribe to input events
            SubscribeToInput();

            // Register with PlayerService if it exists
            var playerService = ServiceRegistry.Get<IPlayerService>();
            playerService?.RegisterPlayer(this);

            Debug.Log("PlayerMonoBehaviour: Initialized and registered");
        }

        void Update()
        {
            if (!IsInitialized) return;

            HandlePhysics();
            UpdateRightArmAnimation();
            _playerController.Update(Time.deltaTime);

            // Update debug info less frequently to avoid performance issues
            if (_showDebugInfo && Time.frameCount % 10 == 0)
            {
                UpdateEdgeDebugInfo();
                DrawDebugInfo();
            }
        }

        void OnDestroy()
        {
            UnsubscribeFromInput();
            
            // Unregister from PlayerService
            var playerService = ServiceRegistry.Get<IPlayerService>();
            playerService?.UnregisterPlayer();
        }

        #endregion

        #region Player Initialization

        private void InitializePlayer()
        {
            _playerController = new PlayerController(_baseStats);
            
            // Subscribe to player events
            _playerController.OnPlayerDied += HandlePlayerDeath;
            _playerController.OnHealthChanged += HandleHealthChanged;

            OnPlayerInitialized?.Invoke(_playerController);
            Debug.Log("PlayerMonoBehaviour: Player controller initialized");
        }

        #endregion

        #region Input Handling

        private void SubscribeToInput()
        {
            if (_inputService == null) return;

            _inputService.OnMove += HandleMoveInput;
            _inputService.OnInteract += HandleInteractInput;
            _inputService.OnRun += HandleRunInput;
            _inputService.OnAim += HandleAimInput;
            _inputService.OnShoot += HandleShootInput;
            _inputService.OnLook += HandleLookInput;
        }

        private void UnsubscribeFromInput()
        {
            if (_inputService == null) return;

            _inputService.OnMove -= HandleMoveInput;
            _inputService.OnInteract -= HandleInteractInput;
            _inputService.OnRun -= HandleRunInput;
            _inputService.OnAim -= HandleAimInput;
            _inputService.OnShoot -= HandleShootInput;
            _inputService.OnLook -= HandleLookInput;
        }

        private void HandleMoveInput(Vector2 input)
        {
            if (!IsInitialized) return;
            
            // Apply edge protection to movement input
            Vector2 filteredInput = ApplyEdgeProtection(input);
            _playerController.Movement.SetMovementInput(filteredInput);
        }

        private void HandleInteractInput()
        {
            if (!IsInitialized) return;
            
            // Only allow interaction in Normal state
            if (_playerController.StateMachine.CanInteract())
            {
                // Handle interaction logic
                Debug.Log("PlayerMonoBehaviour: Interact input received");
            }
        }

        private void HandleRunInput(bool isRunning)
        {
            if (!IsInitialized) return;
            
            _playerController.Movement.SetRunning(isRunning);
        }

        private void HandleAimInput(bool isAiming)
        {
            if (!IsInitialized) return;
            
            if (isAiming)
            {
                _playerController.StartAiming();
            }
            else
            {
                _playerController.StopAiming();
            }
        }

        private void HandleShootInput()
        {
            if (!IsInitialized) return;
            
            _playerController.PerformShoot();
        }

        private void HandleLookInput(Vector2 lookDelta)
        {
            if (!IsInitialized) return;
            
            // Only process look input when aiming
            if (_playerController.IsAiming)
            {
                // Convert mouse delta to aim direction
                // This is a simple implementation - you might want to make this more sophisticated
                Vector2 newAimDirection = _playerController.AimDirection + lookDelta * 0.01f;
                _playerController.SetAimDirection(newAimDirection);
            }
        }

        #endregion

        #region Physics

        private void HandlePhysics()
        {
            // Calculate movement (XZ plane only for 2D game)
            Vector3 movement = _playerController.Movement.CalculateMovement(Time.deltaTime);

            // Apply edge protection to movement before moving
            if (_enableEdgeProtection)
            {
                movement = ApplyEdgeProtectionToMovement(movement);
            }

            // Apply movement - no gravity or Y-axis movement for 2D game
            _characterController.Move(movement);
        }

        private void CheckGrounded()
        {
            // For 2D games, always consider grounded (no jumping/falling)
            _isGrounded = true;
            _playerController.Movement.SetGrounded(_isGrounded);
        }

        #endregion

        #region Edge Protection

        private Vector3 ApplyEdgeProtectionToMovement(Vector3 movement)
        {
            if (!_enableEdgeProtection || movement.magnitude < 0.001f) return movement;

            Vector3 currentPosition = transform.position;
            Vector3 intendedPosition = currentPosition + movement;
            Vector3 safeMovement = movement;

            // Check X movement (left/right)
            if (Mathf.Abs(movement.x) > 0.001f)
            {
                Vector3 directionX = movement.x > 0 ? Vector3.right : Vector3.left;
                if (!IsPositionSafe(currentPosition, directionX, Mathf.Abs(movement.x)))
                {
                    safeMovement.x = 0f;
                }
            }

            // Check Z movement (forward/backward)
            if (Mathf.Abs(movement.z) > 0.001f)
            {
                Vector3 directionZ = movement.z > 0 ? Vector3.forward : Vector3.back;
                if (!IsPositionSafe(currentPosition, directionZ, Mathf.Abs(movement.z)))
                {
                    safeMovement.z = 0f;
                }
            }

            return safeMovement;
        }

        private bool IsPositionSafe(Vector3 fromPosition, Vector3 direction, float distance)
        {
            // Calculate the position we want to check
            Vector3 checkPosition = fromPosition + direction * (distance + _edgeDetectionDistance);
            checkPosition.y = fromPosition.y + _edgeRaycastHeight;

            // Cast ray downward from the intended position to check for ground
            bool hasGround = Physics.Raycast(checkPosition, Vector3.down, 2f, _edgeDetectionLayerMask);

            return hasGround;
        }

        private Vector2 ApplyEdgeProtection(Vector2 input)
        {
            // This method is kept for backward compatibility but now uses real-time checking
            if (!_enableEdgeProtection) return input;

            Vector2 filteredInput = input;
            Vector3 currentPosition = transform.position;

            // Check each direction in real-time
            if (input.x > 0 && !IsPositionSafe(currentPosition, Vector3.right, 0.1f))
                filteredInput.x = 0;
            if (input.x < 0 && !IsPositionSafe(currentPosition, Vector3.left, 0.1f))
                filteredInput.x = 0;
            if (input.y > 0 && !IsPositionSafe(currentPosition, Vector3.forward, 0.1f))
                filteredInput.y = 0;
            if (input.y < 0 && !IsPositionSafe(currentPosition, Vector3.back, 0.1f))
                filteredInput.y = 0;

            return filteredInput;
        }

        // Update edge state for debug display (optional, called less frequently)
        private void UpdateEdgeDebugInfo()
        {
            if (!_enableEdgeProtection) return;

            Vector3 currentPosition = transform.position;
            _canMoveForward = IsPositionSafe(currentPosition, Vector3.forward, 0.1f);
            _canMoveBackward = IsPositionSafe(currentPosition, Vector3.back, 0.1f);
            _canMoveLeft = IsPositionSafe(currentPosition, Vector3.left, 0.1f);
            _canMoveRight = IsPositionSafe(currentPosition, Vector3.right, 0.1f);
        }

        #endregion

        #region Right Arm Animation

        private void UpdateRightArmAnimation()
        {
            if (_rightArm == null || _playerCamera == null) return;

            if (_playerController.IsAiming)
            {
                // Get mouse position in world space
                Vector3 mouseWorldPosition = GetMouseWorldPosition();
                if (mouseWorldPosition != Vector3.zero)
                {
                    // Calculate direction from arm to mouse position
                    Vector3 directionToMouse = (mouseWorldPosition - _rightArm.position).normalized;
                    
                    // Calculate target rotation
                    Quaternion targetRotation = Quaternion.LookRotation(directionToMouse, Vector3.up);
                    
                    // Smoothly rotate towards target
                    _rightArm.rotation = Quaternion.Slerp(_rightArm.rotation, targetRotation, 
                        _armRotationSpeed * Time.deltaTime);
                }
            }
            // When not aiming, the arm could return to a default position
            // You can add this logic if needed
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (_playerCamera == null) return Vector3.zero;

            // Get mouse position using new Input System
            Vector2 mousePosition = Mouse.current?.position.ReadValue() ?? Vector2.zero;
            
            if (mousePosition == Vector2.zero)
            {
                // Fallback: This could happen if mouse is not connected or Input System is not properly set up
                return Vector3.zero;
            }

            // Cast ray from camera through mouse position
            Ray ray = _playerCamera.ScreenPointToRay(mousePosition);
            
            // For 2D platform games, we'll intersect with a plane at the player's Z position
            Plane targetPlane = new Plane(Vector3.forward, transform.position);
            
            if (targetPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }
            
            return Vector3.zero;
        }

        #endregion

        #region Event Handlers

        private void HandlePlayerDeath()
        {
            Debug.Log("PlayerMonoBehaviour: Player died - triggering death sequence");
            
            // Disable input
            if (_inputService != null)
            {
                _inputService.IsEnabled = false;
            }

            // Trigger death animation/effects
            // This could trigger a death screen, respawn logic, etc.
            
            // For now, just load the last save
            var saveSystem = ServiceRegistry.Get<ISaveService>();
            saveSystem?.LoadLastSave();
        }

        private void HandleHealthChanged(float newHealth)
        {
            Debug.Log($"PlayerMonoBehaviour: Health changed to {newHealth}");
            // Update UI, play effects, etc.
        }



        #endregion

        #region Save/Load Integration

        public void LoadFromSaveData(PlayerSaveData saveData)
        {
            if (!IsInitialized) return;

            // Load player controller state
            _playerController.LoadFromSaveData(saveData);

            // Set position and rotation
            transform.position = saveData.savePosition;
            transform.eulerAngles = saveData.saveRotation;

            Debug.Log($"PlayerMonoBehaviour: Loaded save data from {saveData.saveID}");
        }

        public PlayerSaveData CreateSaveData(string savePointID)
        {
            if (!IsInitialized) return null;

            return _playerController.CreateSaveData(savePointID, transform.position, transform.eulerAngles);
        }

        #endregion

        #region Public Interface

        public void SetPosition(Vector3 position)
        {
            _characterController.enabled = false;
            transform.position = position;
            _characterController.enabled = true;
        }

        public void SetRotation(Vector3 rotation)
        {
            transform.eulerAngles = rotation;
        }

        public void TakeDamage(float damage)
        {
            if (IsInitialized)
            {
                _playerController.TakeDamage(damage);
            }
        }

        public void Heal(float amount)
        {
            if (IsInitialized)
            {
                _playerController.Heal(amount);
            }
        }

        #endregion

        #region Debug

        private void DrawDebugInfo()
        {
            if (!IsInitialized) return;

            // Display stats in scene view
            var stats = _playerController.Stats;
            string edgeInfo = _enableEdgeProtection ? 
                $"Edges: F:{_canMoveForward} B:{_canMoveBackward} L:{_canMoveLeft} R:{_canMoveRight}" : 
                "Edge Protection: OFF";
                
            Debug.Log($"Health: {stats.currentHealth}/{stats.maxHealth}, " +
                     $"State: {_playerController.CurrentState}, " +
                     $"Can Move: {_playerController.StateMachine.CanMove()}, " +
                     $"{edgeInfo}");
        }

        void OnDrawGizmosSelected()
        {            
            // Draw edge detection rays
            if (_enableEdgeProtection)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * _edgeRaycastHeight;
                Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
                
                for (int i = 0; i < directions.Length; i++)
                {
                    Vector3 edgePosition = rayOrigin + directions[i] * _edgeDetectionDistance;
                    
                    // Real-time check for this direction
                    bool isSafe = IsPositionSafe(transform.position, directions[i], 0.1f);
                    
                    // Draw horizontal ray
                    Gizmos.color = isSafe ? Color.green : Color.red;
                    Gizmos.DrawLine(rayOrigin, edgePosition);
                    
                    // Draw downward ray from edge (2米长度)
                    Gizmos.color = isSafe ? Color.green : Color.red;
                    Gizmos.DrawLine(edgePosition, edgePosition + Vector3.down * 2f);
                    
                    // Draw a small sphere at the check position for better visualization
                    Gizmos.color = isSafe ? Color.green : Color.red;
                    Gizmos.DrawWireSphere(edgePosition, 0.1f);
                }
            }
        }

        #endregion
    }
}
