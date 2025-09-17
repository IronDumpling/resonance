using UnityEngine;
using TMPro;
using Resonance.Interfaces.Services;
using Resonance.Interfaces.Objects;
using Resonance.Utilities;
using Resonance.Core;
using Resonance.Core.StateMachine;
using Resonance.Core.StateMachine.States;


namespace Resonance.Items
{
    public class InfoMonoBehaviour : MonoBehaviour, IInteractable, IPausable
    {
        [Header("Info Configuration")]
        [SerializeField] private InfoDataAsset _infoDataAsset;

        [Header("Interaction")]
        [SerializeField] private string _interactionText = "E";
        [SerializeField] private float _interactionDuration = 0.1f;

        [Header("Pickup Visual")]
        [SerializeField] private GameObject _pickupVisual;
        [SerializeField] private bool _rotateWhenIdle = true;
        [SerializeField] private float _rotationSpeed = 45f;
        [SerializeField] private bool _bobUpAndDown = true;
        [SerializeField] private float _bobSpeed = 3f;
        [SerializeField] private float _bobHeight = 0.15f;

        [Header("Interaction UI")]
        [SerializeField] private GameObject _interactUI;
        [SerializeField] private TextMeshProUGUI _interactTextComponent;

        // Whether it has been picked up
        private bool _isPickedUp = false;
        
        // Interaction state
        private bool _isInteracting = false;
        
        // Animation related
        private Vector3 _originalPosition;
        private float _bobTimer = 0f;
        
        // Services
        private IInteractionService _interactionService;
        private IAudioService _audioService;

        // Properties
        public InfoDataAsset InfoData => _infoDataAsset;
        public bool IsPickedUp => _isPickedUp;
        public string InteractionText => _interactionText;

        # region Life Cycle

        void Start()
        {
            // Validate InfoDataAsset
            if (_infoDataAsset == null)
            {
                Debug.LogError($"InfoMonoBehaviour: No InfoDataAsset assigned to {gameObject.name}!");
                return;
            }

            // Record original position for animation
            _originalPosition = transform.position;

            // If no pickup visual model is specified, use itself
            if (_pickupVisual == null)
            {
                _pickupVisual = gameObject;
            }

            // Setup audio service
            SetupAudioService();
            
            // Setup interaction UI
            SetupInteractionUI();
            
            // Get interaction service and register as interactable object
            RegisterInteractionService();

            // Register with SelectivePauseService
            RegisterWithPauseService();
        }

        void OnDestroy()
        {
            // Clean up interaction service registration
            if (_interactionService != null)
            {
                _interactionService.UnregisterInteractable(gameObject);
            }
        }

        void Update()
        {
            if (_isPickedUp || _isPaused) return;
            
            // Perform visual animations
            PerformVisualAnimations();
        }

        #endregion

        #region Setup

        private void SetupAudioService()
        {
            _audioService = ServiceRegistry.Get<IAudioService>();
            if (_audioService == null)
            {
                Debug.LogWarning("InfoMonoBehaviour: AudioService not found. Audio effects will be disabled.");
            }
            else
            {
                Debug.Log("InfoMonoBehaviour: AudioService connected successfully");
            }
        }

        private void SetupInteractionUI()
        {
            // Find InteractUI child object (if not manually assigned)
            if (_interactUI == null)
            {
                Transform interactUIChild = transform.Find("InteractUI");
                if (interactUIChild != null)
                {
                    _interactUI = interactUIChild.gameObject;
                    Debug.Log($"InfoMonoBehaviour: Found InteractUI child object: {interactUIChild.name}");
                }
            }

            if (_interactUI == null)
            {
                Debug.LogWarning($"InfoMonoBehaviour: No InteractUI found on {gameObject.name}. UI interaction will be disabled.");
                return;
            }
            
            if (_interactTextComponent == null)
            {
                Transform textChild = _interactUI.transform.Find("Text");
                if (textChild != null)
                {
                    _interactTextComponent = textChild.GetComponent<TextMeshProUGUI>();
                }
            }
            
            if(_interactTextComponent == null)
            {
                Debug.LogWarning($"InfoMonoBehaviour: No TextMeshProUGUI component found in InteractUI on {gameObject.name}");
            }
            else
            {
                Debug.Log($"InfoMonoBehaviour: Found TextMeshProUGUI component for interaction UI");
                _interactTextComponent.text = _interactionText;
            }

            if (_interactUI != null)
            {
                _interactUI.SetActive(false);
            }

            Debug.Log($"InfoMonoBehaviour: Interaction UI setup complete");
        }

        private void RegisterInteractionService()
        {
            _interactionService = ServiceRegistry.Get<IInteractionService>();
            if (_interactionService != null)
            {
                _interactionService.RegisterInteractable(gameObject);
            }
            else
            {
                Debug.LogWarning("InfoMonoBehaviour: InteractionService not found");
            }
        }

        private void RegisterWithPauseService()
        {
            var pauseService = ServiceRegistry.Get<ISelectivePauseService>();
            if (pauseService != null)
            {
                pauseService.RegisterPausable(this);
                Debug.Log("InfoMonoBehaviour: Registered with SelectivePauseService");
            }
            else
            {
                Debug.LogWarning("InfoMonoBehaviour: SelectivePauseService not found, pause functionality will not work");
            }
        }

        #endregion

        private void PerformVisualAnimations()
        {
            if (_pickupVisual == null) return;

            Vector3 currentPosition = _originalPosition;
            Vector3 currentRotation = _pickupVisual.transform.eulerAngles;

            // Up and down animation
            if (_bobUpAndDown)
            {
                _bobTimer += Time.deltaTime * _bobSpeed;
                float bobOffset = Mathf.Sin(_bobTimer) * _bobHeight;
                currentPosition.y = _originalPosition.y + bobOffset;
            }

            // Rotation animation
            if (_rotateWhenIdle)
            {
                currentRotation.y += _rotationSpeed * Time.deltaTime;
            }

            // Apply transform
            transform.position = currentPosition;
            _pickupVisual.transform.eulerAngles = currentRotation;
        }

        #region IInteractable Implementation

        public bool CanInteract()
        {
            return !_isPickedUp;
        }

        public float GetInteractionDuration()
        {
            return _interactionDuration;
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }
        
        public void StartInteraction()
        {
            if (!CanInteract())
            {
                Debug.LogWarning($"InfoMonoBehaviour: Cannot start interaction with {_infoDataAsset.infoName}");
            }

            _isInteracting = true;

            Debug.Log($"InfoMonoBehaviour: Started interaction with {_infoDataAsset.infoName}");

            if (_interactUI != null)
            {
                _interactUI.SetActive(true);
            }
        }

        public void CompleteInteraction()
        {
            if (!_isInteracting)
            {
                Debug.LogWarning($"InfoMonoBehaviour: CompleteInteraction called but not interacting with {_infoDataAsset.infoName}");
                return;
            }

            Debug.Log($"InfoMonoBehaviour: Completing interaction with {_infoDataAsset.infoName}");

            _isInteracting = false;

            // Start the info reading session
            StartInfoReadingSession();
        }

        /// <summary>
        /// Start the info reading session by transitioning to InfoReading state
        /// </summary>
        private void StartInfoReadingSession()
        {
            if (_infoDataAsset == null)
            {
                Debug.LogError("InfoMonoBehaviour: Cannot start info reading session with null InfoDataAsset");
                return;
            }

            // Get the GameManager and its state machine
            var gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogError("InfoMonoBehaviour: GameManager instance not found");
                return;
            }

            var stateMachine = gameManager.GetComponent<GameStateMachine>();
            if (stateMachine == null)
            {
                Debug.LogError("InfoMonoBehaviour: GameStateMachine not found on GameManager");
                return;
            }

            // Get the current GameplayState
            var gameplayState = stateMachine.GetState<GameplayState>("Gameplay");
            if (gameplayState == null)
            {
                Debug.LogError("InfoMonoBehaviour: GameplayState not found in state machine");
                return;
            }

            Debug.Log($"InfoMonoBehaviour: Starting info reading session for {_infoDataAsset.infoName}");

            // Start the info reading session
            gameplayState.StartInfoReading(_infoDataAsset);

            // Hide the interaction UI since we're now in reading mode
            HideInteractionUI();
        }

        public void CancelInteraction()
        {
            if (!_isInteracting) return;

            _isInteracting = false;

            // Hide interaction UI
            if (_interactUI != null)
            {
                _interactUI.SetActive(false);
            }
        }

        public string GetInteractableName()
        {
            return _infoDataAsset?.infoName ?? "Unknown Info";
        }

        #endregion

        #region IPausable Implementation

        private bool _isPaused = false;

        public bool IsPaused => _isPaused;

        public void Pause()
        {
            if (_isPaused) return;
            
            _isPaused = true;
            Debug.Log("InfoMonoBehaviour: Paused - animations stopped");
            
            // Note: This only pauses visual animations in Update()
            // UI interactions and pickup functionality remain active
        }

        public void Resume()
        {
            if (!_isPaused) return;
            
            _isPaused = false;
            Debug.Log("InfoMonoBehaviour: Resumed - animations restarted");
            
            // Animations will resume in the next Update() call
        }

        #endregion
        
        #region Public Methods

        public void ShowInteractionUI()
        {
            if (_interactUI != null)
            {
                _interactUI.SetActive(true);
            }
        }

        public void HideInteractionUI()
        {
            if (_interactUI != null)
            {
                _interactUI.SetActive(false);
            }
        }

        public void ResetInfo()
        {
            _isPickedUp = false;
            _isInteracting = false;
            gameObject.SetActive(true);
        }

        #endregion

        #region Private Methods

        #endregion
    }
}
