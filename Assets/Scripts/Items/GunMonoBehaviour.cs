using UnityEngine;
using Resonance.Interfaces.Services;
using Resonance.Utilities;

namespace Resonance.Items
{
    /// <summary>
    /// 场景中可交互的Gun物体
    /// 玩家可以拾取并装备到武器管理器中
    /// </summary>
    public class GunMonoBehaviour : MonoBehaviour
    {
        [Header("Gun Configuration")]
        [SerializeField] private GunDataAsset _gunDataAsset;
        [SerializeField] private GunData _gunData;
        
        [Header("Interaction")]
        [SerializeField] private float _interactionRange = 2f;
        [SerializeField] private LayerMask _playerLayerMask = 1;
        [SerializeField] private string _interactionText = "Press E to pick up";
        
        [Header("Visual")]
        [SerializeField] private GameObject _visualModel;
        [SerializeField] private bool _rotateWhenIdle = true;
        [SerializeField] private float _rotationSpeed = 30f;
        [SerializeField] private bool _bobUpAndDown = true;
        [SerializeField] private float _bobSpeed = 2f;
        [SerializeField] private float _bobHeight = 0.2f;

        // 是否已被拾取
        private bool _isPickedUp = false;
        
        // 动画相关
        private Vector3 _originalPosition;
        private float _bobTimer = 0f;
        
        // 玩家检测
        private bool _playerInRange = false;
        private Transform _playerTransform;
        
        // Services
        private IInteractionService _interactionService;

        // Events
        public System.Action<GunMonoBehaviour> OnPlayerEnterRange;
        public System.Action<GunMonoBehaviour> OnPlayerExitRange;
        public System.Action<GunMonoBehaviour, Transform> OnPickedUp;

        // Properties
        public GunData GunData => _gunData;
        public bool IsPickedUp => _isPickedUp;
        public bool PlayerInRange => _playerInRange;
        public string InteractionText => _interactionText;
        public float InteractionRange => _interactionRange;

        void Start()
        {
            // 从ScriptableObject创建Gun数据，如果没有则创建默认数据
            if (_gunDataAsset != null)
            {
                _gunData = _gunDataAsset.CreateGunData();
                Debug.Log($"GunMonoBehaviour: Created gun data from asset: {_gunData.weaponName}");
            }
            else if (_gunData == null)
            {
                CreateDefaultGunData();
            }

            // 记录原始位置用于动画
            _originalPosition = transform.position;
            
            // 如果没有指定视觉模型，使用自身
            if (_visualModel == null)
            {
                _visualModel = gameObject;
            }

            // 获取交互服务
            _interactionService = ServiceRegistry.Get<IInteractionService>();
            if (_interactionService != null)
            {
                _interactionService.RegisterInteractable(gameObject);
                Debug.Log($"GunMonoBehaviour: {_gunData.weaponName} registered with InteractionService");
            }
            else
            {
                Debug.LogWarning("GunMonoBehaviour: InteractionService not found");
            }

            Debug.Log($"GunMonoBehaviour: {_gunData.weaponName} ready for pickup");
        }

        void OnDestroy()
        {
            // 清理交互服务注册
            if (_interactionService != null)
            {
                _interactionService.UnregisterInteractable(gameObject);
                if (_interactionService.CurrentInteractable == gameObject)
                {
                    _interactionService.ClearCurrentInteractable();
                }
            }
        }

        void Update()
        {
            if (_isPickedUp) return;

            // 检测玩家范围
            DetectPlayer();
            
            // 执行视觉动画
            PerformVisualAnimations();
        }

        /// <summary>
        /// 创建默认的Gun数据（用于测试）
        /// </summary>
        private void CreateDefaultGunData()
        {
            _gunData = new GunData
            {
                weaponName = "Test Gun",
                weaponDescription = "A test weapon",
                maxAmmo = 8,
                currentAmmo = 8,
                ammoType = "TypeA",
                damage = 25f,
                range = 100f,
                fireRate = 1f,
                gridWidth = 2,
                gridHeight = 3
            };
        }

        /// <summary>
        /// 检测附近的玩家
        /// </summary>
        private void DetectPlayer()
        {
            Collider[] playersInRange = Physics.OverlapSphere(transform.position, _interactionRange, _playerLayerMask);
            
            bool foundPlayer = false;
            foreach (var collider in playersInRange)
            {
                if (collider.CompareTag("Player"))
                {
                    foundPlayer = true;
                    if (!_playerInRange)
                    {
                        _playerInRange = true;
                        _playerTransform = collider.transform;
                        OnPlayerEnterRange?.Invoke(this);
                        
                        // 设置为当前可交互对象
                        if (_interactionService != null)
                        {
                            _interactionService.SetCurrentInteractable(gameObject, _interactionText);
                        }
                        
                        Debug.Log($"GunMonoBehaviour: Player entered range of {_gunData.weaponName}");
                    }
                    break;
                }
            }
            
            if (!foundPlayer && _playerInRange)
            {
                _playerInRange = false;
                _playerTransform = null;
                OnPlayerExitRange?.Invoke(this);
                
                // 清除当前可交互对象
                if (_interactionService != null && _interactionService.CurrentInteractable == gameObject)
                {
                    _interactionService.ClearCurrentInteractable();
                }
                
                Debug.Log($"GunMonoBehaviour: Player left range of {_gunData.weaponName}");
            }
        }

        /// <summary>
        /// 执行视觉动画
        /// </summary>
        private void PerformVisualAnimations()
        {
            if (_visualModel == null) return;

            Vector3 currentPosition = _originalPosition;
            Vector3 currentRotation = _visualModel.transform.eulerAngles;

            // 上下浮动动画
            if (_bobUpAndDown)
            {
                _bobTimer += Time.deltaTime * _bobSpeed;
                float bobOffset = Mathf.Sin(_bobTimer) * _bobHeight;
                currentPosition.y = _originalPosition.y + bobOffset;
            }

            // 旋转动画
            if (_rotateWhenIdle)
            {
                currentRotation.y += _rotationSpeed * Time.deltaTime;
            }

            // 应用变换
            transform.position = currentPosition;
            _visualModel.transform.eulerAngles = currentRotation;
        }

        /// <summary>
        /// 检查玩家是否在交互范围内
        /// </summary>
        /// <param name="playerPosition">玩家位置</param>
        /// <returns>是否可以交互</returns>
        public bool CanInteract(Vector3 playerPosition)
        {
            if (_isPickedUp) return false;
            
            float distance = Vector3.Distance(transform.position, playerPosition);
            return distance <= _interactionRange;
        }

        /// <summary>
        /// 直接检查当前是否可以被交互
        /// </summary>
        /// <returns>是否可以交互</returns>
        public bool CanInteract()
        {
            return !_isPickedUp && _playerInRange;
        }

        /// <summary>
        /// 拾取武器
        /// </summary>
        /// <param name="player">拾取的玩家Transform</param>
        /// <returns>武器数据的副本</returns>
        public GunData PickupWeapon(Transform player = null)
        {
            if (_isPickedUp)
            {
                Debug.LogWarning("GunMonoBehaviour: Weapon already picked up");
                return null;
            }

            _isPickedUp = true;
            _playerInRange = false;
            
            // 创建数据副本
            GunData gunCopy = _gunData.Clone();
            
            // 触发拾取事件
            OnPickedUp?.Invoke(this, player);
            
            // 停止所有动画
            StopAllCoroutines();
            
            // 隐藏物体（或销毁，根据需要）
            if (_visualModel != null)
            {
                _visualModel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
            
            Debug.Log($"GunMonoBehaviour: {_gunData.weaponName} picked up by {(player ? player.name : "unknown player")}");
            
            return gunCopy;
        }

        /// <summary>
        /// 重置武器状态（用于重新生成或测试）
        /// </summary>
        public void ResetWeapon()
        {
            _isPickedUp = false;
            gameObject.SetActive(true);
            Debug.Log($"GunMonoBehaviour: {_gunData.weaponName} reset");
        }

        void OnDrawGizmosSelected()
        {
            // 绘制交互范围
            Gizmos.color = _isPickedUp ? Color.gray : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
        }

        void OnDrawGizmos()
        {
            // 始终显示一个小的交互指示
            if (!_isPickedUp)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.2f);
            }
        }
    }
}
