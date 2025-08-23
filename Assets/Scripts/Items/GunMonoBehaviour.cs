using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Resonance.Interfaces.Services;
using Resonance.Utilities;

namespace Resonance.Items
{
    /// <summary>
    /// 场景中可交互的Gun物体
    /// 玩家可以拾取并装备到武器管理器中
    /// 
    /// Visual System Responsibilities:
    /// - _pickupVisual: The visual representation for the gun in the world (pickup state)
    /// - This handles pickup animations (bob, rotation) and interaction triggers
    /// - When equipped, the gun data is passed to WeaponManager but no visual weapon is shown on player yet
    /// - Future: Add equipped weapon visual system to player for when gun is equipped
    /// </summary>
    public class GunMonoBehaviour : MonoBehaviour
    {
        [Header("Gun Configuration")]
        [SerializeField] private GunDataAsset _gunDataAsset;
        
        [Header("Interaction")]
        [SerializeField] private string _interactionText = "Press E";
        
        [Header("Pickup Visual")]
        [SerializeField] private GameObject _pickupVisual;
        [SerializeField] private bool _rotateWhenIdle = true;
        [SerializeField] private float _rotationSpeed = 30f;
        [SerializeField] private bool _bobUpAndDown = true;
        [SerializeField] private float _bobSpeed = 2f;
        [SerializeField] private float _bobHeight = 0.2f;
        
        [Header("Interaction UI")]
        [SerializeField] private GameObject _interactUI;
        [SerializeField] private TextMeshProUGUI _interactText; // For TextMeshPro
        [SerializeField] private Text _interactTextLegacy; // For legacy UI Text

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
        private IAudioService _audioService;

        // Events
        public System.Action<GunMonoBehaviour> OnPlayerEnterRange;
        public System.Action<GunMonoBehaviour> OnPlayerExitRange;
        public System.Action<GunMonoBehaviour, Transform> OnPickedUp;

        // Properties
        public GunDataAsset GunData => _gunDataAsset;
        public bool IsPickedUp => _isPickedUp;
        public bool PlayerInRange => _playerInRange;
        public string InteractionText => _interactionText;

        void Start()
        {
            // 验证Gun数据资产
            if (_gunDataAsset == null)
            {
                Debug.LogError($"GunMonoBehaviour: No GunDataAsset assigned to {gameObject.name}!");
                return;
            }

            Debug.Log($"GunMonoBehaviour: Using gun data asset: {_gunDataAsset.weaponName}");

            // 记录原始位置用于动画
            _originalPosition = transform.position;
            
            // 如果没有指定拾取视觉模型，使用自身
            if (_pickupVisual == null)
            {
                _pickupVisual = gameObject;
            }

            // 设置音频服务
            SetupAudioService();
            
            // 设置Visual子对象的触发器事件
            SetupVisualTrigger();
            
            // 设置交互UI
            SetupInteractionUI();

            // 获取交互服务
            _interactionService = ServiceRegistry.Get<IInteractionService>();
            if (_interactionService != null)
            {
                _interactionService.RegisterInteractable(gameObject);
                Debug.Log($"GunMonoBehaviour: {_gunDataAsset.weaponName} registered with InteractionService");
            }
            else
            {
                Debug.LogWarning("GunMonoBehaviour: InteractionService not found");
            }

            Debug.Log($"GunMonoBehaviour: {_gunDataAsset.weaponName} ready for pickup");
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
            
            // 执行视觉动画
            PerformVisualAnimations();
        }

        /// <summary>
        /// 设置音频服务引用
        /// </summary>
        private void SetupAudioService()
        {
            _audioService = ServiceRegistry.Get<IAudioService>();
            if (_audioService == null)
            {
                Debug.LogWarning("GunMonoBehaviour: AudioService not found. Audio effects will be disabled.");
            }
            else
            {
                Debug.Log("GunMonoBehaviour: AudioService connected successfully");
            }
        }

        /// <summary>
        /// 设置交互UI
        /// </summary>
        private void SetupInteractionUI()
        {
            // 查找InteractUI子对象（如果没有手动分配）
            if (_interactUI == null)
            {
                Transform interactUIChild = transform.Find("InteractUI");
                if (interactUIChild != null)
                {
                    _interactUI = interactUIChild.gameObject;
                    Debug.Log($"GunMonoBehaviour: Found InteractUI child object: {interactUIChild.name}");
                }
            }
            
            if (_interactUI == null)
            {
                Debug.LogWarning($"GunMonoBehaviour: No InteractUI found on {gameObject.name}. UI interaction will be disabled.");
                return;
            }
            
            // 查找Text组件（支持TextMeshPro和Legacy Text）
            if (_interactText == null && _interactTextLegacy == null)
            {
                // 先尝试找TextMeshPro组件
                _interactText = _interactUI.GetComponentInChildren<TextMeshProUGUI>();
                
                // 如果没找到，尝试找Legacy Text组件
                if (_interactText == null)
                {
                    _interactTextLegacy = _interactUI.GetComponentInChildren<Text>();
                }
                
                // 也可以尝试查找名为"Text"的子对象
                if (_interactText == null && _interactTextLegacy == null)
                {
                    Transform textChild = _interactUI.transform.Find("Text");
                    if (textChild != null)
                    {
                        _interactText = textChild.GetComponent<TextMeshProUGUI>();
                        if (_interactText == null)
                        {
                            _interactTextLegacy = textChild.GetComponent<Text>();
                        }
                    }
                }
            }
            
            if (_interactText == null && _interactTextLegacy == null)
            {
                Debug.LogWarning($"GunMonoBehaviour: No Text component found in InteractUI on {gameObject.name}");
            }
            else
            {
                string textType = _interactText != null ? "TextMeshPro" : "Legacy Text";
                Debug.Log($"GunMonoBehaviour: Found {textType} component for interaction UI");
            }
            
            // 初始化UI文本内容
            UpdateInteractionText();
            
            // 初始状态：隐藏UI
            SetInteractionUIVisible(false);
        }

        /// <summary>
        /// 更新交互UI的文本内容
        /// </summary>
        private void UpdateInteractionText()
        {
            if (_interactText != null)
            {
                _interactText.text = _interactionText;
            }
            else if (_interactTextLegacy != null)
            {
                _interactTextLegacy.text = _interactionText;
            }
        }
        
        /// <summary>
        /// 显示/隐藏交互UI
        /// </summary>
        /// <param name="visible">是否显示</param>
        private void SetInteractionUIVisible(bool visible)
        {
            if (_interactUI != null)
            {
                _interactUI.SetActive(visible);
                Debug.Log($"GunMonoBehaviour: Set interaction UI {(visible ? "visible" : "hidden")} for {_gunDataAsset.weaponName}");
            }
        }

        /// <summary>
        /// 设置Visual子对象的触发器事件
        /// </summary>
        private void SetupVisualTrigger()
        {
            Debug.Log($"GunMonoBehaviour: Setting up trigger for Visual object");
            
            // 查找Visual子对象上的触发器
            Collider visualCollider = null;
            if (_pickupVisual != null && _pickupVisual != gameObject)
            {
                visualCollider = _pickupVisual.GetComponent<Collider>();
            }
            
            if (visualCollider == null)
            {
                // 如果拾取视觉模型没有collider，检查是否有名为"Visual"的子对象
                Transform visualChild = transform.Find("Visual");
                if (visualChild != null)
                {
                    visualCollider = visualChild.GetComponent<Collider>();
                    _pickupVisual = visualChild.gameObject;
                    Debug.Log($"GunMonoBehaviour: Found Visual child object: {visualChild.name}");
                }
            }
            
            if (visualCollider == null)
            {
                Debug.LogError($"GunMonoBehaviour: No trigger collider found on Visual object! Please ensure the Visual child has a Collider component set as Trigger.");
                return;
            }
            
            if (!visualCollider.isTrigger)
            {
                Debug.LogError($"GunMonoBehaviour: Collider on {visualCollider.name} is not set as Trigger! Please check 'Is Trigger' in the Collider component.");
                return;
            }
            
            Debug.Log($"GunMonoBehaviour: Visual trigger setup complete - Object: {visualCollider.name}, Type: {visualCollider.GetType().Name}, IsTrigger: {visualCollider.isTrigger}");
            
            // 添加一个简单的触发器处理组件到Visual对象
            var existingHandler = visualCollider.GetComponent<GunVisualTriggerHandler>();
            if (existingHandler == null)
            {
                var handler = visualCollider.gameObject.AddComponent<GunVisualTriggerHandler>();
                handler.SetGunMonoBehaviour(this);
                Debug.Log($"GunMonoBehaviour: Added trigger handler to {visualCollider.name}");
            }
            else
            {
                existingHandler.SetGunMonoBehaviour(this);
                Debug.Log($"GunMonoBehaviour: Updated existing trigger handler on {visualCollider.name}");
            }
        }

        /// <summary>
        /// 处理触发器进入事件（由GunVisualTriggerHandler调用）
        /// </summary>
        /// <param name="other">进入触发器的碰撞体</param>
        public void HandleTriggerEnter(Collider other)
        {
            Debug.Log($"GunMonoBehaviour: HandleTriggerEnter called with {other.name} (Tag: {other.tag}, Layer: {other.gameObject.layer})");
            
            if (_isPickedUp) 
            {
                Debug.Log("GunMonoBehaviour: Gun already picked up, ignoring trigger");
                return;
            }
            
            // Check if the collider belongs to a player
            if (other.CompareTag("Player"))
            {
                Debug.Log($"GunMonoBehaviour: Found Player collider: {other.name}");
                
                // Get the root player transform (might be on a child collider)
                Transform playerRoot = other.transform;
                var playerMono = other.GetComponentInParent<Resonance.Player.PlayerMonoBehaviour>();
                if (playerMono != null)
                {
                    playerRoot = playerMono.transform;
                    Debug.Log($"GunMonoBehaviour: Found PlayerMonoBehaviour on {playerRoot.name}");
                }
                
                if (!_playerInRange)
                {
                    _playerInRange = true;
                    _playerTransform = playerRoot;
                    OnPlayerEnterRange?.Invoke(this);
                    
                    // 显示交互UI
                    SetInteractionUIVisible(true);
                    
                    // 设置为当前可交互对象
                    if (_interactionService != null)
                    {
                        _interactionService.SetCurrentInteractable(gameObject, _interactionText);
                        Debug.Log($"GunMonoBehaviour: Set current interactable to {gameObject.name}");
                    }
                    else
                    {
                        Debug.LogError("GunMonoBehaviour: InteractionService is null!");
                    }
                    
                    Debug.Log($"GunMonoBehaviour: Player entered range of {_gunDataAsset.weaponName}");
                }
            }
            else
            {
                Debug.Log($"GunMonoBehaviour: Collider {other.name} doesn't have Player tag (Tag: {other.tag})");
            }
        }
        
        /// <summary>
        /// 处理触发器退出事件（由GunVisualTriggerHandler调用）
        /// </summary>
        /// <param name="other">退出触发器的碰撞体</param>
        public void HandleTriggerExit(Collider other)
        {
            Debug.Log($"GunMonoBehaviour: HandleTriggerExit called with {other.name} (Tag: {other.tag}, Layer: {other.gameObject.layer})");
            
            if (_isPickedUp) 
            {
                Debug.Log("GunMonoBehaviour: Gun already picked up, ignoring trigger exit");
                return;
            }
            
            // Check if the collider belongs to a player
            if (other.CompareTag("Player"))
            {
                Debug.Log($"GunMonoBehaviour: Player collider {other.name} exiting trigger");
                
                // Get the root player transform (might be on a child collider)
                Transform playerRoot = other.transform;
                var playerMono = other.GetComponentInParent<Resonance.Player.PlayerMonoBehaviour>();
                if (playerMono != null)
                {
                    playerRoot = playerMono.transform;
                }
                
                // Only clear if this is the same player that entered
                if (_playerInRange && _playerTransform == playerRoot)
                {
                    _playerInRange = false;
                    _playerTransform = null;
                    OnPlayerExitRange?.Invoke(this);
                    
                    // 隐藏交互UI
                    SetInteractionUIVisible(false);
                    
                    // 清除当前可交互对象
                    if (_interactionService != null && _interactionService.CurrentInteractable == gameObject)
                    {
                        _interactionService.ClearCurrentInteractable();
                        Debug.Log($"GunMonoBehaviour: Cleared current interactable");
                    }
                    
                    Debug.Log($"GunMonoBehaviour: Player left range of {_gunDataAsset.weaponName}");
                }
            }
        }

        /// <summary>
        /// 执行视觉动画
        /// </summary>
        private void PerformVisualAnimations()
        {
            if (_pickupVisual == null) return;

            Vector3 currentPosition = _originalPosition;
            Vector3 currentRotation = _pickupVisual.transform.eulerAngles;

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
            _pickupVisual.transform.eulerAngles = currentRotation;
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
        public GunDataAsset PickupWeapon(Transform player = null)
        {
            if (_isPickedUp)
            {
                Debug.LogWarning("GunMonoBehaviour: Weapon already picked up");
                return null;
            }

            _isPickedUp = true;
            _playerInRange = false;

            PlayPickuoAudio(transform.position);
            
            // 隐藏交互UI
            SetInteractionUIVisible(false);
            
            // 创建运行时副本
            GunDataAsset gunCopy = _gunDataAsset.CreateRuntimeCopy();
            
            // 触发拾取事件
            OnPickedUp?.Invoke(this, player);
            
            // 停止所有动画
            StopAllCoroutines();
            
            // 隐藏拾取视觉对象
            if (_pickupVisual != null)
            {
                _pickupVisual.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
            
            Debug.Log($"GunMonoBehaviour: {_gunDataAsset.weaponName} picked up by {(player ? player.name : "unknown player")}");
            
            return gunCopy;
        }

        /// <summary>
        /// 播放拾取音频
        /// </summary>
        /// <param name="pickupPosition">拾取位置</param>
        private void PlayPickuoAudio(Vector3 pickupPosition)
        {
            if (_audioService == null) return;

            AudioClipType audioClipType = AudioClipType.ItemPickup;
            _audioService.PlaySFX3D(audioClipType, pickupPosition, 0.8f, 1f);

            Debug.Log($"GunMonoBehaviour: Played pickup audio {audioClipType} at {pickupPosition}");
        }

        /// <summary>
        /// 重置武器状态（用于重新生成或测试）
        /// </summary>
        public void ResetWeapon()
        {
            _isPickedUp = false;
            gameObject.SetActive(true);
            SetInteractionUIVisible(false); // 重置时隐藏UI
            Debug.Log($"GunMonoBehaviour: {_gunDataAsset.weaponName} reset");
        }

        /// <summary>
        /// 设置交互文本内容（运行时更改）
        /// </summary>
        /// <param name="newText">新的交互文本</param>
        public void SetInteractionText(string newText)
        {
            _interactionText = newText;
            UpdateInteractionText();
            Debug.Log($"GunMonoBehaviour: Updated interaction text to '{newText}'");
        }
    }

    /// <summary>
    /// 简单的触发器处理器，用于转发触发器事件到父对象的GunMonoBehaviour
    /// 此组件会被自动添加到Visual子对象上
    /// </summary>
    public class GunVisualTriggerHandler : MonoBehaviour
    {
        private GunMonoBehaviour _gunMonoBehaviour;

        public void SetGunMonoBehaviour(GunMonoBehaviour gunMono)
        {
            _gunMonoBehaviour = gunMono;
        }

        void OnTriggerEnter(Collider other)
        {
            if (_gunMonoBehaviour != null)
            {
                _gunMonoBehaviour.HandleTriggerEnter(other);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (_gunMonoBehaviour != null)
            {
                _gunMonoBehaviour.HandleTriggerExit(other);
            }
        }
    }
}
