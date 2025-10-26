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
    public class PlayerMonoBehaviour : MonoBehaviour, IDamageable, IWavable
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
                _waveAttackTrigger.OnCoreHitboxEntered -= OnCoreHitboxEnteredRange;
                _waveAttackTrigger.OnCoreHitboxExited -= OnCoreHitboxExitedRange;
                _waveAttackTrigger.OnCoreHitboxesChanged -= OnCoreHitboxesChangedInRange;
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
            
            // Check Visual child object's collider settings
            Collider visualCollider = visualChild.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Debug.Log($"PlayerMonoBehaviour: Visual collider found - Name: {visualChild.name}, " +
                         $"Layer: {visualChild.gameObject.layer} ({LayerMask.LayerToName(visualChild.gameObject.layer)}), " +
                         $"Tag: {visualChild.tag}, IsTrigger: {visualCollider.isTrigger}");
            }
            else
            {
                Debug.LogWarning($"PlayerMonoBehaviour: No collider found on Visual child {visualChild.name}");
            }

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
            _waveAttackTrigger.OnCoreHitboxEntered += OnCoreHitboxEnteredRange;
            _waveAttackTrigger.OnCoreHitboxExited += OnCoreHitboxExitedRange;
            _waveAttackTrigger.OnCoreHitboxesChanged += OnCoreHitboxesChangedInRange;

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
            if (HasCoreHitboxesInWaveAttackRange())
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
                if (!HasCoreHitboxesInWaveAttackRange())
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
        /// 从ShootingSystem获取鼠标目标点
        /// 使用与射击系统相同的逻辑, 确保玩家朝向和射击方向一致
        /// </summary>
        /// <returns>鼠标指向的世界坐标点</returns>
        private Vector3 GetMouseTargetPointFromShootingSystem()
        {
            if (!IsInitialized) return Vector3.zero;

            // 使用ShootingSystem的统一鼠标目标点逻辑
            return _playerController.ShootingSystem?.GetCurrentMouseTargetPoint() ?? Vector3.zero;
        }

        /// <summary>
        /// 计算Player Visual应该面向的Y轴旋转角度
        /// 使用与ShootingSystem相同的鼠标目标点, 确保玩家朝向和射击方向一致
        /// 算法说明：
        /// 1. 计算从Player到目标点的方向向量(仅考虑XZ平面)
        /// 2. 使用Atan2函数计算该方向在XZ平面的角度
        /// 3. 转换为Unity的Y轴旋转角度
        /// </summary>
        /// <param name="playerPosition">玩家位置</param>
        /// <param name="targetPosition">目标点世界坐标(来自ShootingSystem)</param>
        /// <returns>Y轴旋转角度(度数)</returns>
        private float CalculatePlayerYRotation(Vector3 playerPosition, Vector3 targetPosition)
        {
            // 步骤1: 计算从Player到目标点的方向向量
            Vector3 directionToTarget = targetPosition - playerPosition;
            
            // 步骤2: 将Y轴分量设为0, 确保只考虑XZ平面的旋转
            directionToTarget.y = 0f;
            
            // 步骤3: 确保方向向量有效(避免除零错误)
            if (directionToTarget.sqrMagnitude < 0.001f)
            {
                // 如果目标点和玩家位置过于接近, 保持当前旋转
                return _playerVisual.eulerAngles.y;
            }
            
            // 步骤4: 标准化方向向量
            directionToTarget.Normalize();
            
            // 步骤5: 使用Atan2计算角度
            // Atan2(z, x) 计算从X轴正方向到(x,z)点的角度
            // Unity的前方是Z轴正方向, 所以我们使用 Atan2(x, z)
            float angleInRadians = Mathf.Atan2(directionToTarget.x, directionToTarget.z);
            
            // 步骤6: 转换为度数
            float angleInDegrees = angleInRadians * Mathf.Rad2Deg;
            
            // 步骤7: 确保角度在0-360度范围内
            if (angleInDegrees < 0f)
            {
                angleInDegrees += 360f;
            }
            
            return angleInDegrees-90f; // TODO: tempfix for now
        }

        /// <summary>
        /// 平滑地旋转Player Visual到目标Y轴角度
        /// 使用Quaternion.Slerp进行球面线性插值, 确保旋转自然
        /// </summary>
        /// <param name="targetYRotation">目标Y轴旋转角度</param>
        private void ApplyPlayerYRotation(float targetYRotation)
        {
            // 获取当前旋转
            Vector3 currentEulerAngles = _playerVisual.eulerAngles;
            
            // 创建目标旋转(保持X和Z轴不变, 只改变Y轴)
            Vector3 targetEulerAngles = new Vector3(
                currentEulerAngles.x,  // 保持X轴旋转
                targetYRotation,       // 设置新的Y轴旋转
                currentEulerAngles.z   // 保持Z轴旋转
            );
            
            // 转换为Quaternion
            Quaternion currentRotation = _playerVisual.rotation;
            Quaternion targetRotation = Quaternion.Euler(targetEulerAngles);
            
            // 使用球面线性插值进行平滑旋转
            _playerVisual.rotation = Quaternion.Slerp(
                currentRotation, 
                targetRotation, 
                _playerRotationSpeed * Time.deltaTime
            );
        }

        #endregion

        #region Aiming Line Management

        /// <summary>
        /// 更新瞄准线显示
        /// </summary>
        private void UpdateAimingLine()
        {
            if (!IsInitialized) return;
            
            // 只在瞄准状态下显示瞄准线
            if (_playerController.IsAiming)
            {
                // 计算射击起始位置(与射击时相同)
                Vector3 shootOrigin = transform.position + _shootOriginOffset;
                
                // 更新瞄准线
                _playerController.ShootingSystem?.UpdateAimingLine(shootOrigin);
            }
            else
            {
                // 不在瞄准状态时隐藏瞄准线
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
                // 获取鼠标世界坐标
                Vector3 mouseWorldPosition = GetMouseWorldPosition();
                if (mouseWorldPosition != Vector3.zero)
                {
                    // 计算Right Arm的全方向旋转
                    Quaternion targetArmRotation = CalculateRightArmRotation(_rightArm.position, mouseWorldPosition);
                    
                    // 应用平滑旋转(全方向)
                    ApplyRightArmRotation(targetArmRotation);
                }
            }
            // 当不在瞄准状态时, 可以添加返回默认位置的逻辑
        }

        /// <summary>
        /// 计算Right Arm应该指向的全方向旋转
        /// 算法说明：
        /// 1. 计算从手臂到鼠标的方向向量(3D全方向)
        /// 2. 使用Quaternion.LookRotation计算旋转
        /// 3. 保持Up向量为世界坐标的上方向, 确保旋转自然
        /// </summary>
        /// <param name="armPosition">手臂位置</param>
        /// <param name="mousePosition">鼠标世界坐标</param>
        /// <returns>目标旋转四元数</returns>
        private Quaternion CalculateRightArmRotation(Vector3 armPosition, Vector3 mousePosition)
        {
            // 步骤1: 计算从手臂到鼠标的方向向量(全3D方向)
            Vector3 directionToMouse = mousePosition - armPosition;
            
            // 步骤2: 检查方向向量是否有效
            if (directionToMouse.sqrMagnitude < 0.001f)
            {
                // 如果距离过近, 保持当前旋转
                return _rightArm.rotation;
            }
            
            // 步骤3: 标准化方向向量
            directionToMouse.Normalize();
            
            // 步骤4: 使用LookRotation计算目标旋转
            // LookRotation(forward, up) 创建一个旋转, 使前方指向forward方向
            // 使用Vector3.up作为上方向, 确保旋转看起来自然
            Quaternion targetRotation = Quaternion.LookRotation(directionToMouse, Vector3.up);
            
            return targetRotation;
        }

        /// <summary>
        /// 平滑地旋转Right Arm到目标旋转
        /// 使用Quaternion.Slerp进行球面线性插值
        /// </summary>
        /// <param name="targetRotation">目标旋转四元数</param>
        private void ApplyRightArmRotation(Quaternion targetRotation)
        {
            // 使用球面线性插值进行平滑旋转(全方向)
            _rightArm.rotation = Quaternion.Slerp(
                _rightArm.rotation, 
                targetRotation, 
                _armRotationSpeed * Time.deltaTime
            );
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
        /// Supports multiple damage types: Physical Health, Core Health, Chaos
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
        /// Wave chaos state
        /// </summary>
        public WaveChaosState ChaosState => IsInitialized && _playerController.Stats.crystalCore != null 
            ? _playerController.Stats.crystalCore.ChaosState 
            : WaveChaosState.Order;

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

        /// <summary>
        /// Current chaos value
        /// </summary>
        public float CurrentChaos => IsInitialized && _playerController.Stats.crystalCore != null 
            ? _playerController.Stats.crystalCore.CurrentChaos 
            : 0f;

        /// <summary>
        /// Max chaos value
        /// </summary>
        public float MaxChaos => IsInitialized && _playerController.Stats.crystalCore != null 
            ? _playerController.Stats.crystalCore.MaxChaos 
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
        private void OnCoreHitboxEnteredRange(EnemyHitbox hitbox)
        {
            if (hitbox != null)
            {
                Debug.Log($"PlayerMonoBehaviour: Core hitbox {hitbox.name} entered wave attack range");
            }
        }

        /// <summary>
        /// Called when a Core hitbox exits wave attack range
        /// </summary>
        /// <param name="hitbox">The Core hitbox that exited range</param>
        private void OnCoreHitboxExitedRange(EnemyHitbox hitbox)
        {
            if (hitbox != null)
            {
                Debug.Log($"PlayerMonoBehaviour: Core hitbox {hitbox.name} exited wave attack range");
            }
        }

        /// <summary>
        /// Called when the list of Core hitboxes in range changes
        /// </summary>
        private void OnCoreHitboxesChangedInRange()
        {
            int coreHitboxCount = _waveAttackTrigger?.CoreHitboxCount ?? 0;
            Debug.Log($"PlayerMonoBehaviour: Core hitboxes in wave attack range: {coreHitboxCount}");
        }

        /// <summary>
        /// Public method to check if there are Core hitboxes in wave attack range
        /// Used by PlayerActionController for priority logic
        /// </summary>
        /// <returns>True if there are Core hitboxes in range</returns>
        public bool HasCoreHitboxesInWaveAttackRange()
        {
            return _waveAttackTrigger?.HasCoreHitboxesInRange ?? false;
        }

        /// <summary>
        /// Get the number of Core hitboxes in wave attack range
        /// </summary>
        /// <returns>Number of Core hitboxes in range</returns>
        public int GetCoreHitboxCount()
        {
            return _waveAttackTrigger?.CoreHitboxCount ?? 0;
        }

        /// <summary>
        /// Get the closest Core hitbox in wave attack range
        /// Used by PlayerWaveAttackAction to find target
        /// </summary>
        /// <returns>Closest Core hitbox or null if none</returns>
        public EnemyHitbox GetClosestCoreHitbox()
        {
            return _waveAttackTrigger?.GetClosestCoreHitbox();
        }

        /// <summary>
        /// Get all Core hitboxes in wave attack range
        /// </summary>
        /// <returns>List of Core hitboxes in range</returns>
        public List<EnemyHitbox> GetCoreHitboxesInRange()
        {
            return _waveAttackTrigger?.CoreHitboxesInRange ?? new List<EnemyHitbox>();
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

        #region IWavable Implementation

        /// <summary>
        /// Get the Wave object from CrystalCore
        /// </summary>
        public Wave GetWave()
        {
            return IsInitialized && _playerController.Stats.crystalCore != null 
                ? _playerController.Stats.crystalCore.Wave 
                : null;
        }

        /// <summary>
        /// Get the base damages for wave attacks
        /// For player, this comes from waveAttackDamages
        /// </summary>
        public Damages GetWaveBaseDamages()
        {
            if (IsInitialized && _playerController.Stats.waveAttackDamages != null)
            {
                return _playerController.Stats.waveAttackDamages;
            }
            return new Damages();
        }

        /// <summary>
        /// Apply wave damages from a source wavable
        /// </summary>
        /// <param name="damages">Damages to apply</param>
        /// <param name="sourceWavable">The source of the wave attack</param>
        /// <param name="description">Description of the damage source</param>
        /// <returns>True if damage was successfully applied</returns>
        public bool ApplyWaveDamages(Damages damages, IWavable sourceWavable, string description = "Wave Damage")
        {
            if (!IsInitialized)
            {
                Debug.LogError($"PlayerMonoBehaviour: Cannot apply wave damages - not initialized");
                return false;
            }

            if (damages == null)
            {
                Debug.LogError($"PlayerMonoBehaviour: Cannot apply wave damages - damages is null");
                return false;
            }

            // Get source information
            Vector3 sourcePosition = Vector3.zero;
            GameObject sourceObject = null;

            if (sourceWavable != null)
            {
                if (sourceWavable is MonoBehaviour sourceMono)
                {
                    sourcePosition = sourceMono.transform.position;
                    sourceObject = sourceMono.gameObject;
                }
            }

            // Create damage info
            DamageInfo damageInfo = new DamageInfo(
                damages: damages,
                sourcePosition: sourcePosition,
                sourceObject: sourceObject,
                description: description
            );

            // Apply damage through the player's damage system
            TakeDamage(damageInfo);

            Debug.Log($"PlayerMonoBehaviour: Applied wave damages to {name} - " +
                      $"CoreHealth: {damages.GetDamage(DamageType.CoreHealth):F1}, " +
                      $"Chaos: {damages.GetDamage(DamageType.Chaos):F1}");
            
            return true;
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
