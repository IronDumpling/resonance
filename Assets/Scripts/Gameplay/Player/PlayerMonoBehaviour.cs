using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Resonance.Player.Core;
using Resonance.Player.Data;
using Resonance.Player.Triggers;
using Resonance.Core;
using Resonance.Enemies.Triggers;
using Resonance.Items;
using Resonance.Cameras;
using Resonance.Utilities;
using Resonance.Utilities.Types;
using Resonance.Utilities.Waves;
using Resonance.Interfaces;
using Resonance.Interfaces.Services;

namespace Resonance.Player
{
    /// <summary>
    /// MonoBehaviour component that handles Unity-specific player functionality.
    /// Acts as a bridge between Unity's GameObject system and the player logic.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMonoBehaviour : MonoBehaviour, IDamageable, IHasPhysicalHealth, IHasCoreHealth
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

        [Header("Player Visual Components")]
        [SerializeField] private Transform _playerVisual;
        [SerializeField] private Renderer _playerBodyRenderer;
        [SerializeField] private float _playerRotationSpeed = 8f;
        
        [Header("Right Arm Animation")]
        [SerializeField] private Transform _rightArm;
        [SerializeField] private float _armRotationSpeed = 5f;
        [SerializeField] private Camera _playerCamera;

        [Header("Shooting")]
        [SerializeField] private Vector3 _shootOriginOffset = new Vector3(0, 0f, 0.5f);
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;

        // Components
        private CharacterController _characterController;
        private PlayerController _playerController;
        private IInputService _inputService;
        private IInteractionService _interactionService;
        private WaveAttackTrigger _waveAttackTrigger;
        private PlayerInteractTrigger _playerInteractTrigger;
        private LevelCameraManager _cameraManager;
        private bool _cameraManagerInitialized = false;
        private PlayerHitboxManager _hitboxManager;

        // Visual Materials
        private Material _normalMaterial;
        private Material _damageMaterial;

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
        public PlayerHitboxManager HitboxManager => _hitboxManager;
        public PlayerCrystalCoreHitbox CrystalCoreHitbox => _hitboxManager?.GetCrystalCoreHitbox();

        public Vector3 ShootOriginOffset => _shootOriginOffset;

        #region Unity Lifecycle

        void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            
            if (_baseStats == null)
            {
                Debug.LogError("PlayerMonoBehaviour: BaseStats not assigned!");
                return;
            }

            // Ensure Visual child object is properly configured for interaction
            SetupVisualComponents();

            InitializePlayer();
            
            // Setup hitbox system
            SetupHitboxSystem();
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

            // Get interaction service
            _interactionService = ServiceRegistry.Get<IInteractionService>();
            if (_interactionService == null)
            {
                Debug.LogError("PlayerMonoBehaviour: InteractionService not found!");
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

            // Initialize WaveAttackTrigger
            InitializeWaveAttackTrigger();

            // Initialize PlayerInteractTrigger
            InitializePlayerInteractTrigger();

            // Load materials
            LoadResources();

            // Set initial material
            SetMaterial(_normalMaterial);

            Debug.Log("PlayerMonoBehaviour: Initialized and registered");
        }

        void Update()
        {
            if (!IsInitialized || _playerController.IsPaused) return;

            HandlePhysics();
            UpdatePlayerVisualRotation();
            UpdateRightArmAnimation();
            UpdateAimingLine();
            
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
            
            // Cleanup WaveAttackTrigger events
            if (_waveAttackTrigger != null)
            {
                _waveAttackTrigger.OnWavableEntered -= OnWavableEnteredRange;
                _waveAttackTrigger.OnWavableExited -= OnWavableExitedRange;
                _waveAttackTrigger.OnWavablesChanged -= OnWavablesChangedInRange;
            }

            // Cleanup PlayerInteractTrigger
            if (_playerInteractTrigger != null)
            {
                _playerInteractTrigger.Cleanup();
            }
            
            // Unregister from PlayerService
            var playerService = ServiceRegistry.Get<IPlayerService>();
            playerService?.UnregisterPlayer();
        }

        #endregion

        #region Player Initialization

        private void SetupVisualComponents()
        {
            Debug.Log($"PlayerMonoBehaviour: Setting up Visual child for interaction");
            
            // 查找Visual子对象
            Transform visualChild = transform.Find("Visual");
            if (visualChild == null)
            {
                Debug.LogWarning("PlayerMonoBehaviour: No 'Visual' child found. Looking for _playerVisual reference...");
                if (_playerVisual != null)
                {
                    visualChild = _playerVisual;
                    Debug.Log($"PlayerMonoBehaviour: Using _playerVisual reference: {visualChild.name}");
                }
                else
                {
                    Debug.LogError("PlayerMonoBehaviour: No Visual child found and _playerVisual is not set!");
                    return;
                }
            }
            
            // Ensure Visual child object has correct tag
            if (!visualChild.CompareTag("Player"))
            {
                visualChild.tag = "Player";
                Debug.Log($"PlayerMonoBehaviour: Set Player tag on Visual child: {visualChild.name}");
            }
            
            // Get Body renderer
            Transform bodyTransform = visualChild.Find("Body");
            if (bodyTransform != null)
            {
                _playerBodyRenderer = bodyTransform.GetComponent<Renderer>();
                if (_playerBodyRenderer == null)
                {
                    Debug.LogError("PlayerMonoBehaviour: No Renderer found on Body child");
                }
            }
            else
            {
                Debug.LogError("PlayerMonoBehaviour: No Body child found on Visual child");
            }
        }

        private void InitializePlayer()
        {
            _playerController = new PlayerController(_baseStats);
            
            // Use gameObject reference to initialize shooting system
            _playerController.Initialize(_baseStats, gameObject);
            
            // Subscribe to death events for game logic (not UI)
            _playerController.OnDeath += HandleDeath;

            OnPlayerInitialized?.Invoke(_playerController);
            Debug.Log("PlayerMonoBehaviour: Player controller initialized with shooting system");
        }

        /// <summary>
        /// Setup player hitbox system
        /// </summary>
        private void SetupHitboxSystem()
        {
            // Try to find existing Visual child
            Transform visualChild = _playerVisual ?? transform.Find("Visual");
            if (visualChild == null)
            {
                Debug.LogWarning($"PlayerMonoBehaviour: No Visual child found for hitbox system setup on {gameObject.name}");
                return;
            }
            
            GameObject visualObject = visualChild.gameObject;
            
            // Check if PlayerHitboxManager already exists
            PlayerHitboxManager existingManager = visualObject.GetComponent<PlayerHitboxManager>();
            
            if (existingManager != null)
            {
                existingManager.Initialize(this);
                _hitboxManager = existingManager;
                Debug.Log($"PlayerMonoBehaviour: Initialized existing PlayerHitboxManager on {visualObject.name}");
            }
            else
            {
                PlayerHitboxManager newManager = visualObject.AddComponent<PlayerHitboxManager>();
                newManager.Initialize(this);
                _hitboxManager = newManager;
                Debug.Log($"PlayerMonoBehaviour: Added and initialized new PlayerHitboxManager on {visualObject.name}");
            }
        }

        /// <summary>
        /// Initialize the WaveAttackTrigger component
        /// </summary>
        private void InitializeWaveAttackTrigger()
        {
            if (_playerController == null)
            {
                Debug.LogError("PlayerMonoBehaviour: Cannot initialize WaveAttackTrigger - PlayerController is null");
                return;
            }

            // Find the WaveAttackRange GameObject
            Transform waveAttackRangeTransform = transform.Find("WaveAttackRange");
            if (waveAttackRangeTransform == null)
            {
                Debug.LogError("PlayerMonoBehaviour: WaveAttackRange GameObject not found as child of Player");
                return;
            }

            // Get or add the WaveAttackTrigger component
            _waveAttackTrigger = waveAttackRangeTransform.GetComponent<WaveAttackTrigger>();
            if (_waveAttackTrigger == null)
            {
                _waveAttackTrigger = waveAttackRangeTransform.gameObject.AddComponent<WaveAttackTrigger>();
                Debug.Log("PlayerMonoBehaviour: Added WaveAttackTrigger component to WaveAttackRange GameObject");
            }

            // Initialize with player controller, range, and layer mask from base stats
            float waveAttackRange = _baseStats?.InteractionRange ?? 1.5f;
            LayerMask waveInteractionLayerMask = _baseStats?.WaveInteractionLayerMask ?? LayerDict.GetLayer("Enemy");
            _waveAttackTrigger.Initialize(_playerController, waveAttackRange, waveInteractionLayerMask);

            // Subscribe to events for debugging
            _waveAttackTrigger.OnWavableEntered += OnWavableEnteredRange;
            _waveAttackTrigger.OnWavableExited += OnWavableExitedRange;
            _waveAttackTrigger.OnWavablesChanged += OnWavablesChangedInRange;

            Debug.Log($"PlayerMonoBehaviour: WaveAttackTrigger initialized with range {waveAttackRange} and layer mask {waveInteractionLayerMask.value}");
        }

        /// <summary>
        /// Initialize the PlayerInteractTrigger component
        /// </summary>
        private void InitializePlayerInteractTrigger()
        {
            // Find the InteractRange GameObject
            Transform interactRangeTransform = transform.Find("InteractRange");
            if (interactRangeTransform == null)
            {
                Debug.LogError("PlayerMonoBehaviour: InteractRange GameObject not found as child of Player");
                return;
            }

            // Get or add the PlayerInteractTrigger component
            _playerInteractTrigger = interactRangeTransform.GetComponent<PlayerInteractTrigger>();
            if (_playerInteractTrigger == null)
            {
                _playerInteractTrigger = interactRangeTransform.gameObject.AddComponent<PlayerInteractTrigger>();
                Debug.Log("PlayerMonoBehaviour: Added PlayerInteractTrigger component to InteractRange GameObject");
            }

            // Initialize with player reference
            _playerInteractTrigger.Initialize(this);

            // Set the collider radius and layer mask from base stats
            float interactionRange = _baseStats?.InteractionRange ?? 1.5f;
            LayerMask interactionLayerMask = _baseStats?.InteractionLayerMask ?? LayerDict.GetLayer("Interactable");
            
            var sphereCollider = interactRangeTransform.GetComponent<SphereCollider>();
            if (sphereCollider != null)
            {
                sphereCollider.radius = interactionRange;
                Debug.Log($"PlayerMonoBehaviour: Set InteractRange radius to {interactionRange}");
            }

            // Set the interaction layer mask
            _playerInteractTrigger.SetInteractionLayerMask(interactionLayerMask);


        }

        #endregion

        #region Input Handling

        private void SubscribeToInput()
        {
            if (_inputService == null) return;

            _inputService.OnMove += HandleMoveInput;
            _inputService.OnInteract += HandleInteractInput;
            _inputService.OnWaveAttack += HandleWaveAttackInput; // F key short press (WaveAttack)
            _inputService.OnHeal += HandleHealInput; // F key press/release (Heal)
            _inputService.OnRun += HandleRunInput;
            _inputService.OnAim += HandleAimInput;
            _inputService.OnShoot += HandleShootInput;
            _inputService.OnReload += HandleReloadInput; // R key press (Reload)
        }

        private void UnsubscribeFromInput()
        {
            if (_inputService == null) return;

            _inputService.OnMove -= HandleMoveInput;
            _inputService.OnInteract -= HandleInteractInput;
            _inputService.OnWaveAttack -= HandleWaveAttackInput;
            _inputService.OnHeal -= HandleHealInput;
            _inputService.OnRun -= HandleRunInput;
            _inputService.OnAim -= HandleAimInput;
            _inputService.OnShoot -= HandleShootInput;
            _inputService.OnReload -= HandleReloadInput;
        }

        private void HandleMoveInput(Vector2 input)
        {
            if (!IsInitialized) return;
            
            // Check if current action blocks movement (e.g., WaveAttackAction, HealAction)
            if (_playerController.PlayerActionController.IsBlocking)
            {
                Debug.Log("PlayerMonoBehaviour: Movement input blocked by action");
                _playerController.Movement.SetMovementInput(Vector2.zero);
                return;
            }
            
            _playerController.Movement.SetMovementInput(input);
        }

        private void HandleInteractInput()
        {
            if (!IsInitialized) return;

            bool interactStarted = _playerController.TryStartAction("Interact");
            if (interactStarted)
            {
                Debug.Log("PlayerMonoBehaviour: Started InteractAction via E key");
            }
            else
            {
                Debug.Log("PlayerMonoBehaviour: InteractAction conditions not met");
            }
        }

        /// <summary>
        /// Handle WaveAttack press input (F key short press) - WaveAttackAction
        /// </summary>
        private void HandleWaveAttackInput()
        {
            if (!IsInitialized) return;

            // Short press F -> WaveAttackAction only when Core hitboxes are in range
            if (HasWavablesInWaveAttackRange())
            {
                // Try to start WaveAttackAction
                bool waveAttackStarted = _playerController.TryStartAction("WaveAttack");
                if (waveAttackStarted)
                {
                    Debug.Log("PlayerMonoBehaviour: Started WaveAttackAction via short press F");
                }
                else
                {
                    Debug.Log("PlayerMonoBehaviour: WaveAttackAction conditions not met");
                }
            }
            else
            {
                // No Core hitboxes in range - short press F does nothing
                Debug.Log("PlayerMonoBehaviour: Short press F ignored - no Core hitboxes in range");
            }
        }

        /// <summary>
        /// Handle Heal input (F key press/release) - HealAction
        /// </summary>
        /// <param name="isPressed">True when F key is pressed, false when released</param>
        private void HandleHealInput(bool isPressed)
        {
            if (!IsInitialized) return;

            if (isPressed)
            {
                // F key pressed - try to start HealAction only when no Core hitboxes in range
                if (!HasWavablesInWaveAttackRange())
                {
                    bool recoverStarted = _playerController.TryStartAction("Heal");
                    if (recoverStarted)
                    {
                        Debug.Log("PlayerMonoBehaviour: Started HealAction via F key press");
                    }
                    else
                    {
                        Debug.Log("PlayerMonoBehaviour: HealAction conditions not met");
                    }
                }
                else
                {
                    Debug.Log("PlayerMonoBehaviour: HealAction blocked - Core hitboxes in range (use short press for Wave)");
                }
            }
            else
            {
                // F key released - stop HealAction if it's running
                if (_playerController.GetCurrentActionName() == "Heal")
                {
                    _playerController.CancelCurrentAction();
                    Debug.Log("PlayerMonoBehaviour: Stopped HealAction via F key release");
                }
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
            
            // Don't handle shooting if player is in aiming state
            // PlayerAimingState will handle shooting instead
            if (_playerController.IsAiming)
            {
                return;
            }
            
            // Vector3 shootOrigin = transform.position + transform.forward * 0.5f;
            Vector3 shootOrigin = transform.position + _shootOriginOffset;
            
            var result = _playerController.PerformShoot(shootOrigin);
            if (result.success && _showDebugInfo)
            {
                Debug.Log($"Shot fired: Hit={result.hasHit}, Target={result.hitObject?.name ?? "None"}");
            }
        }

        private void HandleReloadInput()
        {
            if (!IsInitialized) return;

            // Try to start reload action
            bool reloadStarted = _playerController.TryStartAction("Reload");
            if (reloadStarted)
            {
                Debug.Log("PlayerMonoBehaviour: Started Reload action via R key press");
            }
            else
            {
                Debug.Log("PlayerMonoBehaviour: Reload action conditions not met");
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

        #region Player Visual Rotation

        private void UpdatePlayerVisualRotation()
        {
            if (_playerVisual == null) return;

            // 使用ShootingSystem的鼠标目标点逻辑, 确保玩家朝向和射击方向一致
            Vector3 mouseTargetPoint = GetMouseTargetPointFromShootingSystem();
            if (mouseTargetPoint == Vector3.zero) return;

            // 计算Player Visual的Y轴旋转角度(仅使用XZ平面)
            float targetYRotation = CalculatePlayerYRotation(transform.position, mouseTargetPoint);
            
            // 应用平滑旋转(仅Y轴)
            ApplyPlayerYRotation(targetYRotation);
        }

        /// <summary>
        /// From ShootingSystem get the mouse target point
        /// Use the same logic as the ShootingSystem, ensure the player is facing the shooting direction
        /// </summary>
        /// <returns>The world coordinate point the mouse is pointing at</returns>
        private Vector3 GetMouseTargetPointFromShootingSystem()
        {
            if (!IsInitialized) return Vector3.zero;

            // Use the same logic as the ShootingSystem
            return _playerController.ShootingSystem?.GetCurrentMouseTargetPoint() ?? Vector3.zero;
        }

        /// <summary>
        /// Calculate the Y axis rotation angle the Player Visual should face
        /// Use the same mouse target point as the ShootingSystem, ensure the player is facing the shooting direction
        /// Algorithm:
        /// 1. Calculate the direction vector from the Player to the target point (only consider the XZ plane)
        /// 2. Use the Atan2 function to calculate the angle of the direction in the XZ plane
        /// 3. Convert to the Y axis rotation angle in Unity
        /// </summary>
        /// <param name="playerPosition">The position of the Player</param>
        /// <param name="targetPosition">The world coordinate point of the target (from the ShootingSystem)</param>
        /// <returns>The Y axis rotation angle (degrees)</returns>
        private float CalculatePlayerYRotation(Vector3 playerPosition, Vector3 targetPosition)
        {
            // Step 1: Calculate the direction vector from the Player to the target point
            Vector3 directionToTarget = targetPosition - playerPosition;
            
            // Step 2: Set the Y axis component to 0, ensure only considering the rotation in the XZ plane
            directionToTarget.y = 0f;
            
            // Step 3: Ensure the direction vector is valid (avoid division by zero)
            if (directionToTarget.sqrMagnitude < 0.001f)
            {
                // If the target point and the player position are too close, keep the current rotation
                return _playerVisual.eulerAngles.y;
            }
            
            // Step 4: Normalize the direction vector
            directionToTarget.Normalize();
            
            // Step 5: Use Atan2 to calculate the angle
            // Atan2(z, x) Calculate the angle from the X axis positive direction to the (x,z) point
            // Unity's forward is the Z axis positive direction, so we use Atan2(x, z)
            float angleInRadians = Mathf.Atan2(directionToTarget.x, directionToTarget.z);
            
            // Step 6: Convert to degrees
            float angleInDegrees = angleInRadians * Mathf.Rad2Deg;
            
            // Step 7: Ensure the angle is within the range of 0-360 degrees
            if (angleInDegrees < 0f)
            {
                angleInDegrees += 360f;
            }
            
            return angleInDegrees-90f; // TODO: tempfix for now
        }

        /// <summary>
        /// Smoothly rotate the Player Visual to the target Y axis angle
        /// Use Quaternion.Slerp to perform spherical linear interpolation, ensure the rotation is natural
        /// </summary>
        /// <param name="targetYRotation">The target Y axis rotation angle</param>
        private void ApplyPlayerYRotation(float targetYRotation)
        {
            // Get the current rotation
            Vector3 currentEulerAngles = _playerVisual.eulerAngles;
            
            // Create the target rotation (keep the X and Z axes unchanged, only change the Y axis)
            Vector3 targetEulerAngles = new Vector3(
                currentEulerAngles.x,  // Keep the X axis rotation
                targetYRotation,       // Set the new Y axis rotation
                currentEulerAngles.z   // Keep the Z axis rotation
            );
            
            // Convert to Quaternion
            Quaternion currentRotation = _playerVisual.rotation;
            Quaternion targetRotation = Quaternion.Euler(targetEulerAngles);
            
            // Use spherical linear interpolation to smoothly rotate
            _playerVisual.rotation = Quaternion.Slerp(
                currentRotation, 
                targetRotation, 
                _playerRotationSpeed * Time.deltaTime
            );
        }

        #endregion

        #region Aiming Line Management

        /// <summary>
        /// Update the aiming line display
        /// </summary>
        private void UpdateAimingLine()
        {
            if (!IsInitialized) return;
            
            // Only show the aiming line in aiming state
            if (_playerController.IsAiming)
            {
                // Calculate the shooting origin position (the same as when shooting)
                Vector3 shootOrigin = transform.position + _shootOriginOffset;
                
                // Update the aiming line
                _playerController.ShootingSystem?.UpdateAimingLine(shootOrigin);
            }
            else
            {
                // Hide the aiming line when not in aiming state
                _playerController.ShootingSystem?.HideAimingLine();
            }
        }

        #endregion

        #region Right Arm Animation

        private void UpdateRightArmAnimation()
        {
            if (_rightArm == null || _playerCamera == null) return;

            if (_playerController.IsAiming)
            {
                // Get the mouse world coordinate
                Vector3 mouseWorldPosition = GetMouseWorldPosition();
                if (mouseWorldPosition != Vector3.zero)
                {
                    // Calculate the full direction rotation of the Right Arm
                    Quaternion targetArmRotation = CalculateRightArmRotation(_rightArm.position, mouseWorldPosition);
                    
                    // Apply smooth rotation (full direction)
                    ApplyRightArmRotation(targetArmRotation);
                }
            }
            // When not in aiming state, can add logic to return to the default position
        }

        /// <summary>
        /// Calculate the full direction rotation the Right Arm should face
        /// Algorithm:
        /// 1. Calculate the direction vector from the arm to the mouse (3D full direction)
        /// 2. Use Quaternion.LookRotation to calculate the rotation
        /// 3. Keep the Up vector as the world coordinate's up direction, ensure the rotation is natural
        /// </summary>
        /// <param name="armPosition">The position of the arm</param>
        /// <param name="mousePosition">The world coordinate of the mouse</param>
        /// <returns>The target rotation Quaternion</returns>
        private Quaternion CalculateRightArmRotation(Vector3 armPosition, Vector3 mousePosition)
        {
            // Step 1: Calculate the direction vector from the arm to the mouse (3D full direction)
            Vector3 directionToMouse = mousePosition - armPosition;
            
            // Step 2: Check if the direction vector is valid
            if (directionToMouse.sqrMagnitude < 0.001f)
            {
                // If the distance is too close, keep the current rotation
                return _rightArm.rotation;
            }
            
            // Step 3: Normalize the direction vector
            directionToMouse.Normalize();
            
            // Step 4: Use LookRotation to calculate the target rotation
            // LookRotation(forward, up) Create a rotation, make the forward direction point to the forward direction
            // Use Vector3.up as the up direction, ensure the rotation looks natural
            Quaternion targetRotation = Quaternion.LookRotation(directionToMouse, Vector3.up);
            
            return targetRotation;
        }

        /// <summary>
        /// Smoothly rotate the Right Arm to the target rotation
        /// Use Quaternion.Slerp to perform spherical linear interpolation
        /// </summary>
        /// <param name="targetRotation">The target rotation Quaternion</param>
        private void ApplyRightArmRotation(Quaternion targetRotation)
        {
            // Use spherical linear interpolation to smoothly rotate (full direction)
            _rightArm.rotation = Quaternion.Slerp(
                _rightArm.rotation, 
                targetRotation, 
                _armRotationSpeed * Time.deltaTime
            );
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (_playerCamera == null) return Vector3.zero;

            // Get mouse position using the new Input System
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

        private void HandleDeath()
        {
            Debug.Log("PlayerMonoBehaviour: Death - triggering game over sequence");
            
            // Disable input
            if (_inputService != null)
            {
                _inputService.DisablePlayerInput();
            }

            // Trigger death animation/effects
            // This should trigger game over screen, respawn logic, etc.
            
            // For now, just load the last save
            var saveSystem = ServiceRegistry.Get<ISaveService>();
            saveSystem?.LoadLastSave();
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

        #endregion

        #region IDamageable Implementation

        /// <summary>
        /// Take damage using the new damage system
        /// Supports multiple damage types: Physical Health, Core Health
        /// Invulnerability is handled at the DamageInfo level, not per damage type
        /// </summary>
        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!IsInitialized || damageInfo.damages == null) return;
            
            // Delegate to controller's unified damage handling
            // Controller handles invulnerability check and applies all damage types
            _playerController.TakeDamage(damageInfo);

            // Trigger camera impulse for hit feedback
            TriggerPlayerHitImpulse(damageInfo);

            // Show visual feedback
            ShowDamageEffect(damageInfo);
        }

        /// <summary>
        /// Physical health state
        /// </summary>
        public PhysicalHealthState PhysicalState => IsInitialized && _playerController.Stats.IsAlive 
            ? PhysicalHealthState.Alive 
            : PhysicalHealthState.Dead;

        /// <summary>
        /// Core health state
        /// </summary>
        public CoreHealthState CoreState => IsInitialized && _playerController.Stats.crystalCore != null 
            ? _playerController.Stats.crystalCore.CoreHealthState 
            : CoreHealthState.Destroyed;

        /// <summary>
        /// Current physical health
        /// </summary>
        public float CurrentPhysicalHealth => IsInitialized ? _playerController.Stats.currentHealth : 0f;

        /// <summary>
        /// Max physical health
        /// </summary>
        public float MaxPhysicalHealth => IsInitialized ? _playerController.Stats.maxHealth : 0f;

        /// <summary>
        /// Current core health
        /// </summary>
        public float CurrentCoreHealth => IsInitialized && _playerController.Stats.crystalCore != null 
            ? _playerController.Stats.crystalCore.CurrentCoreHealth 
            : 0f;

        /// <summary>
        /// Max core health
        /// </summary>
        public float MaxCoreHealth => IsInitialized && _playerController.Stats.crystalCore != null 
            ? _playerController.Stats.crystalCore.MaxCoreHealth 
            : 0f;

        #endregion

        #region Camera Impulse

        /// <summary>
        /// Initialize camera manager when needed
        /// Called from TriggerPlayerHitImpulse if not already initialized
        /// </summary>
        private void InitializeCameraManager()
        {
            if (_cameraManagerInitialized) return;
            
            _cameraManager = FindAnyObjectByType<LevelCameraManager>();
            if (_cameraManager != null)
            {
                _cameraManagerInitialized = true;
                Debug.Log("PlayerMonoBehaviour: LevelCameraManager connected successfully");
            }
            else
            {
                Debug.LogWarning("PlayerMonoBehaviour: LevelCameraManager not found. Camera shake effects will be disabled.");
                _cameraManagerInitialized = false;
            }
        }

        /// <summary>
        /// Trigger player hit camera impulse when taking damage
        /// </summary>
        /// <param name="damageInfo">Damage information</param>
        private void TriggerPlayerHitImpulse(DamageInfo damageInfo)
        {
            // Initialize camera manager if not already done
            InitializeCameraManager();
            
            if (_cameraManager == null) return;

            var impulseSource = _cameraManager.GetPlayerHitImpulse();
            if (impulseSource == null) return;

            // Calculate impulse force based on total damage
            float totalDamage = damageInfo.GetTotalDamage();
            
            // Base force is 1.0, scale by damage (clamped to reasonable range)
            float impulseForce = Mathf.Clamp(totalDamage * 0.1f, 0.5f, 2.0f);
            
            // Generate impulse
            impulseSource.GenerateImpulse(impulseForce);

            if (_showDebugInfo)
            {
                Debug.Log($"PlayerMonoBehaviour: Player hit impulse triggered with force {impulseForce:F2} " +
                         $"(total damage: {totalDamage:F1})");
            }
        }

        #endregion

        #region Visual Effects

        private void LoadResources()
        {
            _normalMaterial = Resources.Load<Material>(_baseStats.NormalMaterialPath);
            if (_normalMaterial == null)
            {
                Debug.LogError($"PlayerMonoBehaviour: Failed to load normal material from {_baseStats.NormalMaterialPath}");
            }

            _damageMaterial = Resources.Load<Material>(_baseStats.DamageMaterialPath);
            if (_damageMaterial == null)
            {
                Debug.LogError($"PlayerMonoBehaviour: Failed to load damage material from {_baseStats.DamageMaterialPath}");
            }
        }

        private void SetMaterial(Material material)
        {
            if (_playerBodyRenderer != null && material != null)
            {
                _playerBodyRenderer.material = material;
            }
        }

        private void ShowDamageEffect(DamageInfo damageInfo)
        {
            if (_playerBodyRenderer != null && _damageMaterial != null)
            {
                StartCoroutine(DamageFlashCoroutine());
            }
        }

        private IEnumerator DamageFlashCoroutine()
        {
            if (_playerBodyRenderer != null && _damageMaterial != null)
            {
                Material originalMaterial = _playerBodyRenderer.material;
                SetMaterial(_damageMaterial);
                yield return new WaitForSeconds(_baseStats.DamageFlashDuration);
                SetMaterial(originalMaterial);
            }
        }

        #endregion

        #region WaveAttackTrigger Events

        /// <summary>
        /// Called when a Core hitbox enters wave attack range
        /// </summary>
        /// <param name="hitbox">The Core hitbox that entered range</param>
        private void OnWavableEnteredRange(IWavable wavable)
        {
            if (wavable != null)
            {
                Debug.Log($"PlayerMonoBehaviour: IWavable entered wave attack range");
            }
        }

        /// <summary>
        /// Called when a Core hitbox exits wave attack range
        /// </summary>
        /// <param name="hitbox">The Core hitbox that exited range</param>
        private void OnWavableExitedRange(IWavable wavable)
        {
            if (wavable != null)
            {
                Debug.Log($"PlayerMonoBehaviour: IWavable exited wave attack range");
            }
        }

        /// <summary>
        /// Called when the list of IWavables in range changes
        /// </summary>
        private void OnWavablesChangedInRange()
        {
            int wavableCount = _waveAttackTrigger?.WavableCount ?? 0;
            Debug.Log($"PlayerMonoBehaviour: IWavables in wave attack range: {wavableCount}");
        }

        /// <summary>
        /// Public method to check if there are Core hitboxes in wave attack range
        /// Used by PlayerActionController for priority logic
        /// </summary>
        /// <returns>True if there are Core hitboxes in range</returns>
        public bool HasWavablesInWaveAttackRange()
        {
            return _waveAttackTrigger?.HasWavablesInRange ?? false;
        }

        /// <summary>
        /// Get debug information about wave attack range detection
        /// </summary>
        /// <returns>Debug info string</returns>
        public string GetWaveAttackRangeDebugInfo()
        {
            return _waveAttackTrigger?.GetDebugInfo() ?? "WaveAttackTrigger not initialized";
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
                
            // WaveAttackTrigger debug info
            string waveAttackInfo = GetWaveAttackRangeDebugInfo();
            
            Debug.Log($"Physical Health: {stats.currentHealth}/{stats.maxHealth}, " +
                     $"Core Energy: {stats.crystalCore.CurrentEnergy}/{stats.crystalCore.MaxEnergy}, " +
                     $"Core Health: {stats.crystalCore.CurrentCoreHealth}/{stats.crystalCore.MaxCoreHealth}, " +
                     $"Core Tier: {_playerController.Stats.crystalCore.EnergyTier}, Physical Tier: {_playerController.Stats.healthTier}, " +
                     $"Slots: {_playerController.Stats.crystalCore.GetEnergyInSlots():F1}/{stats.crystalCore.MaxSlots}, " +
                     $"State: {_playerController.CurrentState}, Action: {_playerController.GetCurrentActionName()}, " +
                     $"Can Move: {_playerController.StateMachine.CanMove()}, " +
                     $"{edgeInfo}, {waveAttackInfo}");
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
