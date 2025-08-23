using UnityEngine;
using UnityEngine.InputSystem;
using Resonance.Player.Core;
using Resonance.Player.Data;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Interfaces.Services;

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

        [Header("Player Rotation")]
        [SerializeField] private Transform _playerVisual;
        [SerializeField] private float _playerRotationSpeed = 8f;
        
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
        private IInteractionService _interactionService;

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

            // Ensure Visual child object is properly configured for interaction
            SetupVisualForInteraction();

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

            Debug.Log("PlayerMonoBehaviour: Initialized and registered");
        }

        void Update()
        {
            if (!IsInitialized) return;

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
            
            // Unregister from PlayerService
            var playerService = ServiceRegistry.Get<IPlayerService>();
            playerService?.UnregisterPlayer();
        }

        #endregion

        #region Player Initialization

        private void SetupVisualForInteraction()
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
            
            // 确保Visual子对象有正确的标签
            if (!visualChild.CompareTag("Player"))
            {
                visualChild.tag = "Player";
                Debug.Log($"PlayerMonoBehaviour: Set Player tag on Visual child: {visualChild.name}");
            }
            
            // 检查Visual子对象的collider设置
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
            
            // 检查所有子colliders并确保有Player标签
            Collider[] childColliders = GetComponentsInChildren<Collider>();
            foreach (var collider in childColliders)
            {
                // 跳过CharacterController
                if (collider == _characterController) continue;
                
                if (!collider.CompareTag("Player"))
                {
                    collider.tag = "Player";
                    Debug.Log($"PlayerMonoBehaviour: Set Player tag on child collider: {collider.name}");
                }
            }
        }

        private void InitializePlayer()
        {
            _playerController = new PlayerController(_baseStats);
            
            // 使用gameObject引用来初始化射击系统
            _playerController.Initialize(_baseStats, gameObject);
            
            // Subscribe to player events
            _playerController.OnPlayerDied += HandlePlayerDeath;
            _playerController.OnHealthChanged += HandleHealthChanged;

            OnPlayerInitialized?.Invoke(_playerController);
            Debug.Log("PlayerMonoBehaviour: Player controller initialized with shooting system");
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
        }

        private void UnsubscribeFromInput()
        {
            if (_inputService == null) return;

            _inputService.OnMove -= HandleMoveInput;
            _inputService.OnInteract -= HandleInteractInput;
            _inputService.OnRun -= HandleRunInput;
            _inputService.OnAim -= HandleAimInput;
            _inputService.OnShoot -= HandleShootInput;
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
            if (!IsInitialized || _interactionService == null) return;
            
            // Only allow interaction in Normal state
            if (_playerController.StateMachine.CanInteract())
            {
                // 执行交互
                bool interactionSuccess = _interactionService.PerformInteraction(transform);
                
                if (interactionSuccess)
                {
                    Debug.Log("PlayerMonoBehaviour: Interaction successful");
                }
                else if (_interactionService.HasInteractable)
                {
                    Debug.LogWarning("PlayerMonoBehaviour: Interaction failed");
                }
                else
                {
                    Debug.Log("PlayerMonoBehaviour: No interactable object nearby");
                }
            }
            else
            {
                Debug.Log("PlayerMonoBehaviour: Cannot interact in current state");
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
            
            // 计算射击起始位置（从玩家中心稍微前方）
            Vector3 shootOrigin = transform.position + Vector3.up * 1.5f + transform.forward * 0.5f;
            
            var result = _playerController.PerformShoot(shootOrigin);
            if (result.success && _showDebugInfo)
            {
                Debug.Log($"Shot fired: Hit={result.hasHit}, Target={result.hitObject?.name ?? "None"}");
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

        #region Player Visual Rotation

        private void UpdatePlayerVisualRotation()
        {
            if (_playerVisual == null) return;

            // 使用ShootingSystem的鼠标目标点逻辑，确保玩家朝向和射击方向一致
            Vector3 mouseTargetPoint = GetMouseTargetPointFromShootingSystem();
            if (mouseTargetPoint == Vector3.zero) return;

            // 计算Player Visual的Y轴旋转角度（仅使用XZ平面）
            float targetYRotation = CalculatePlayerYRotation(transform.position, mouseTargetPoint);
            
            // 应用平滑旋转（仅Y轴）
            ApplyPlayerYRotation(targetYRotation);
        }

        /// <summary>
        /// 从ShootingSystem获取鼠标目标点
        /// 使用与射击系统相同的逻辑，确保玩家朝向和射击方向一致
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
        /// 使用与ShootingSystem相同的鼠标目标点，确保玩家朝向和射击方向一致
        /// 算法说明：
        /// 1. 计算从Player到目标点的方向向量（仅考虑XZ平面）
        /// 2. 使用Atan2函数计算该方向在XZ平面的角度
        /// 3. 转换为Unity的Y轴旋转角度
        /// </summary>
        /// <param name="playerPosition">玩家位置</param>
        /// <param name="targetPosition">目标点世界坐标（来自ShootingSystem）</param>
        /// <returns>Y轴旋转角度（度数）</returns>
        private float CalculatePlayerYRotation(Vector3 playerPosition, Vector3 targetPosition)
        {
            // 步骤1: 计算从Player到目标点的方向向量
            Vector3 directionToTarget = targetPosition - playerPosition;
            
            // 步骤2: 将Y轴分量设为0，确保只考虑XZ平面的旋转
            directionToTarget.y = 0f;
            
            // 步骤3: 确保方向向量有效（避免除零错误）
            if (directionToTarget.sqrMagnitude < 0.001f)
            {
                // 如果目标点和玩家位置过于接近，保持当前旋转
                return _playerVisual.eulerAngles.y;
            }
            
            // 步骤4: 标准化方向向量
            directionToTarget.Normalize();
            
            // 步骤5: 使用Atan2计算角度
            // Atan2(z, x) 计算从X轴正方向到(x,z)点的角度
            // Unity的前方是Z轴正方向，所以我们使用 Atan2(x, z)
            float angleInRadians = Mathf.Atan2(directionToTarget.x, directionToTarget.z);
            
            // 步骤6: 转换为度数
            float angleInDegrees = angleInRadians * Mathf.Rad2Deg;
            
            // 步骤7: 确保角度在0-360度范围内
            if (angleInDegrees < 0f)
            {
                angleInDegrees += 360f;
            }
            
            return angleInDegrees;
        }

        /// <summary>
        /// 平滑地旋转Player Visual到目标Y轴角度
        /// 使用Quaternion.Slerp进行球面线性插值，确保旋转自然
        /// </summary>
        /// <param name="targetYRotation">目标Y轴旋转角度</param>
        private void ApplyPlayerYRotation(float targetYRotation)
        {
            // 获取当前旋转
            Vector3 currentEulerAngles = _playerVisual.eulerAngles;
            
            // 创建目标旋转（保持X和Z轴不变，只改变Y轴）
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
                // 计算射击起始位置（与射击时相同）
                Vector3 shootOrigin = transform.position + Vector3.up * 1.5f + transform.forward * 0.5f;
                
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
                    
                    // 应用平滑旋转（全方向）
                    ApplyRightArmRotation(targetArmRotation);
                }
            }
            // 当不在瞄准状态时，可以添加返回默认位置的逻辑
        }

        /// <summary>
        /// 计算Right Arm应该指向的全方向旋转
        /// 算法说明：
        /// 1. 计算从手臂到鼠标的方向向量（3D全方向）
        /// 2. 使用Quaternion.LookRotation计算旋转
        /// 3. 保持Up向量为世界坐标的上方向，确保旋转自然
        /// </summary>
        /// <param name="armPosition">手臂位置</param>
        /// <param name="mousePosition">鼠标世界坐标</param>
        /// <returns>目标旋转四元数</returns>
        private Quaternion CalculateRightArmRotation(Vector3 armPosition, Vector3 mousePosition)
        {
            // 步骤1: 计算从手臂到鼠标的方向向量（全3D方向）
            Vector3 directionToMouse = mousePosition - armPosition;
            
            // 步骤2: 检查方向向量是否有效
            if (directionToMouse.sqrMagnitude < 0.001f)
            {
                // 如果距离过近，保持当前旋转
                return _rightArm.rotation;
            }
            
            // 步骤3: 标准化方向向量
            directionToMouse.Normalize();
            
            // 步骤4: 使用LookRotation计算目标旋转
            // LookRotation(forward, up) 创建一个旋转，使前方指向forward方向
            // 使用Vector3.up作为上方向，确保旋转看起来自然
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
            // 使用球面线性插值进行平滑旋转（全方向）
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
