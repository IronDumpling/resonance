using UnityEngine;
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
        [SerializeField] private float _groundCheckDistance = 0.1f;
        [SerializeField] private float _gravity = -9.81f;
        
        [Header("2D Platform Settings")]
        [SerializeField] private bool _lockYPosition = false;
        [SerializeField] private float _fixedYPosition = 0f;

        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;

        // Components
        private CharacterController _characterController;
        private PlayerController _playerController;
        private IInputService _inputService;

        // Physics
        private Vector3 _velocity;
        private bool _isGrounded;

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
            _playerController.Update(Time.deltaTime);

            if (_showDebugInfo)
            {
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
            
            _playerController.Movement.SetMovementInput(input);
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
            // Ground check for 2D platform game
            CheckGrounded();

            // Calculate movement
            Vector3 movement = _playerController.Movement.CalculateMovement(Time.deltaTime);

            // Handle gravity and Y-axis movement
            Vector3 verticalMovement = Vector3.zero;
            
            if (_lockYPosition)
            {
                // For pure 2D games, lock Y position
                _velocity.y = 0f;
                transform.position = new Vector3(transform.position.x, _fixedYPosition, transform.position.z);
            }
            else
            {
                // Standard gravity for 2.5D platform games
                if (_isGrounded && _velocity.y < 0)
                {
                    _velocity.y = -2f; // Small downward force to keep grounded
                }
                else
                {
                    _velocity.y += _gravity * Time.deltaTime;
                }
                verticalMovement = new Vector3(0, _velocity.y * Time.deltaTime, 0);
            }

            // Apply movement
            Vector3 finalMovement = movement + verticalMovement;
            _characterController.Move(finalMovement);
        }

        private void CheckGrounded()
        {
            bool wasGrounded = _isGrounded;
            
            if (_lockYPosition)
            {
                // For pure 2D games, always consider grounded
                _isGrounded = true;
            }
            else
            {
                // Standard ground check for 2.5D platform games
                _isGrounded = Physics.CheckSphere(
                    transform.position + Vector3.down * _groundCheckDistance,
                    0.1f,
                    _groundLayerMask
                );
            }

            _playerController.Movement.SetGrounded(_isGrounded);
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

            // Draw ground check (if not locked Y position)
            if (!_lockYPosition)
            {
                Debug.DrawRay(transform.position, Vector3.down * _groundCheckDistance, 
                    _isGrounded ? Color.green : Color.red);
            }

            // Display stats in scene view
            var stats = _playerController.Stats;
            Debug.Log($"Health: {stats.currentHealth}/{stats.maxHealth}, " +
                     $"State: {_playerController.CurrentState}, " +
                     $"Grounded: {_isGrounded}, " +
                     $"Can Move: {_playerController.StateMachine.CanMove()}");
        }

        void OnDrawGizmosSelected()
        {
            // Draw ground check sphere (only if not locked Y position)
            if (!_lockYPosition)
            {
                Gizmos.color = _isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(transform.position + Vector3.down * _groundCheckDistance, 0.1f);
            }
            
            // Draw fixed Y position line for 2D mode
            if (_lockYPosition)
            {
                Gizmos.color = Color.blue;
                Vector3 start = transform.position + Vector3.left * 2f;
                Vector3 end = transform.position + Vector3.right * 2f;
                start.y = end.y = _fixedYPosition;
                Gizmos.DrawLine(start, end);
            }
        }

        #endregion
    }
}
