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
        [SerializeField] private Transform _spawnPoint;

        [Header("Physics")]
        [SerializeField] private LayerMask _groundLayerMask = 1;
        [SerializeField] private float _groundCheckDistance = 0.1f;
        [SerializeField] private float _gravity = -9.81f;

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
        public Vector3 SpawnPosition => _spawnPoint != null ? _spawnPoint.position : transform.position;

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
            _playerController.Movement.OnJump += HandleJump;
            _playerController.Movement.OnLand += HandleLand;

            OnPlayerInitialized?.Invoke(_playerController);
            Debug.Log("PlayerMonoBehaviour: Player controller initialized");
        }

        #endregion

        #region Input Handling

        private void SubscribeToInput()
        {
            if (_inputService == null) return;

            _inputService.OnMove += HandleMoveInput;
            _inputService.OnJump += HandleJumpInput;
            _inputService.OnInteract += HandleInteractInput;
            _inputService.OnRun += HandleRunInput;
            _inputService.OnAttack += HandleAttackInput;
        }

        private void UnsubscribeFromInput()
        {
            if (_inputService == null) return;

            _inputService.OnMove -= HandleMoveInput;
            _inputService.OnJump -= HandleJumpInput;
            _inputService.OnInteract -= HandleInteractInput;
            _inputService.OnRun -= HandleRunInput;
            _inputService.OnAttack -= HandleAttackInput;
        }

        private void HandleMoveInput(Vector2 input)
        {
            if (!IsInitialized) return;
            
            _playerController.Movement.SetMovementInput(input);
        }

        private void HandleJumpInput()
        {
            if (!IsInitialized) return;
            
            _playerController.Movement.RequestJump();
        }

        private void HandleInteractInput()
        {
            if (!IsInitialized) return;
            
            // Handle interaction logic
            Debug.Log("PlayerMonoBehaviour: Interact input received");
        }

        private void HandleRunInput(bool isRunning)
        {
            if (!IsInitialized) return;
            
            _playerController.Movement.SetRunning(isRunning);
        }

        private void HandleAttackInput()
        {
            if (!IsInitialized) return;
            
            _playerController.PerformAttack();
        }

        #endregion

        #region Physics

        private void HandlePhysics()
        {
            // Ground check
            CheckGrounded();

            // Calculate movement
            Vector3 movement = _playerController.Movement.CalculateMovement(Time.deltaTime);

            // Apply gravity
            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Small downward force to keep grounded
            }
            else
            {
                _velocity.y += _gravity * Time.deltaTime;
            }

            // Update velocity from player controller
            _velocity = _playerController.Movement.CalculateVelocity(_velocity, Time.deltaTime);

            // Apply movement
            Vector3 finalMovement = movement + (_velocity * Time.deltaTime);
            _characterController.Move(finalMovement);
        }

        private void CheckGrounded()
        {
            bool wasGrounded = _isGrounded;
            _isGrounded = Physics.CheckSphere(
                transform.position + Vector3.down * _groundCheckDistance,
                0.1f,
                _groundLayerMask
            );

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

        private void HandleJump()
        {
            Debug.Log("PlayerMonoBehaviour: Player jumped");
            // Play jump animation, sound effects, etc.
        }

        private void HandleLand()
        {
            Debug.Log("PlayerMonoBehaviour: Player landed");
            // Play land animation, sound effects, etc.
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

            // Draw ground check
            Debug.DrawRay(transform.position, Vector3.down * _groundCheckDistance, 
                _isGrounded ? Color.green : Color.red);

            // Display stats in scene view
            var stats = _playerController.Stats;
            Debug.Log($"Health: {stats.currentHealth}/{stats.maxHealth}, " +
                     $"Grounded: {_isGrounded}, " +
                     $"Velocity: {_velocity}");
        }

        void OnDrawGizmosSelected()
        {
            // Draw ground check sphere
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.down * _groundCheckDistance, 0.1f);

            // Draw spawn point
            if (_spawnPoint != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(_spawnPoint.position, Vector3.one);
            }
        }

        #endregion
    }
}
