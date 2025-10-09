using UnityEngine;
using UnityEngine.UI;
using Resonance.Core;
using Resonance.Items;
using Resonance.Utilities;
using Resonance.Player;
using Resonance.Player.Core;
using Resonance.Interfaces.Services;

namespace Resonance.UI
{
    /// <summary>
    /// Crosshair Panel - Displays dynamic crosshair UI for aiming
    /// Uses Canvas UI with world-to-screen positioning
    /// Shows crosshair size based on weapon accuracy and changes color based on accuracy percentage
    /// </summary>
    public class CrosshairPanel : UIPanel
    {
        [Header("Crosshair Components")]
        [SerializeField] private RectTransform _crosshairContainer;
        [SerializeField] private Image _crosshairCircle;
        
        [Header("Crosshair Settings")]
        [SerializeField] private float _baseCrosshairSize = 50f;
        [SerializeField] private float _maxCrosshairSize = 200f;
        [SerializeField] private Color _accurateColor = Color.green;
        [SerializeField] private Color _inaccurateColor = Color.red;
        [SerializeField] private float _colorTransitionSpeed = 5f;
        
        [Header("Animation Settings")]
        [SerializeField] private float _sizeTransitionSpeed = 8f;
        
        // Services and Controllers
        private IPlayerService _playerService;
        private PlayerController _playerController;
        private Camera _mainCamera;
        private Canvas _canvas;
        
        // State
        private bool _isInitialized = false;
        private float _targetSize = 0f;
        private float _currentSize = 0f;
        private Color _targetColor = Color.white;
        private Color _currentColor = Color.white;
        private Vector3 _lastAimPoint = Vector3.zero;

        protected override void Awake()
        {
            base.Awake();
            
            _panelName = "CrosshairPanel";
            _layer = UILayer.Game;
            _hideOnStart = true;
        }
        
        #region UIPanel Overrides

        protected override void OnInitialize()
        {
            Debug.Log("CrosshairPanel: OnInitialize called");
            
            // Get canvas reference
            _canvas = GetComponent<Canvas>();
            
            // Setup camera reference
            SetupCamera();
            
            // Get player service for later use
            _playerService = ServiceRegistry.Get<IPlayerService>();
            if (_playerService == null)
            {
                Debug.LogError("CrosshairPanel: PlayerService not found");
            }
            
            // Initialize crosshair components
            InitializeCrosshair();
            
            _isInitialized = true;
            Debug.Log("CrosshairPanel: Initialized successfully");
        }

        protected override void OnShow()
        {
            Debug.Log("CrosshairPanel: Shown");
            
            // Get player controller when panel is shown (late binding)
            RefreshPlayerController();
            
            // Reset crosshair state
            _currentSize = _baseCrosshairSize;
            _targetSize = _baseCrosshairSize;
            _currentColor = _accurateColor;
            _targetColor = _accurateColor;
        }

        protected override void OnHide()
        {
            Debug.Log("CrosshairPanel: Hidden");
        }

        protected override void OnCleanup()
        {
            Debug.Log("CrosshairPanel: Cleaned up");
            _playerController = null;
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (!_isInitialized || !IsVisible) return;
            
            // Refresh player controller if needed
            if (_playerController == null)
            {
                RefreshPlayerController();
                if (_playerController == null) return;
            }
            
            // Only update when player is actually aiming with a weapon
            if (!ShouldShowCrosshair()) return;
            
            UpdateCrosshair();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Setup camera reference using the same logic as ShootingSystem
        /// </summary>
        private void SetupCamera()
        {
            // First try to find CameraManager's main camera
            var cameraManager = Object.FindAnyObjectByType<Resonance.Cameras.CameraManager>();
            if (cameraManager != null && cameraManager.Brain != null)
            {
                _mainCamera = cameraManager.Brain.OutputCamera;
                Debug.Log("CrosshairPanel: Found camera from CameraManager");
                return;
            }
            
            // Fallback: find Main Camera
            _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                Debug.Log("CrosshairPanel: Using Camera.main");
                return;
            }
            
            // Last fallback: find any camera
            _mainCamera = Object.FindAnyObjectByType<Camera>();
            if (_mainCamera != null)
            {
                Debug.Log("CrosshairPanel: Using first found camera");
                return;
            }
            
            Debug.LogError("CrosshairPanel: No camera found! Crosshair positioning will not work.");
        }

        /// <summary>
        /// Initialize crosshair UI components
        /// </summary>
        private void InitializeCrosshair()
        {
            // Ensure we have all required components
            if (_crosshairContainer == null)
            {
                Debug.LogError("CrosshairPanel: CrosshairContainer not assigned!");
                return;
            }
            
            // Setup crosshair circle
            if (_crosshairCircle != null)
            {
                _crosshairCircle.color = _accurateColor;
                _crosshairCircle.type = Image.Type.Simple;
            }
            else
            {
                Debug.LogWarning("CrosshairPanel: CrosshairCircle not assigned!");
            }
        }

        /// <summary>
        /// Refresh player controller reference (late binding)
        /// Called when panel is shown or when controller is null
        /// </summary>
        private void RefreshPlayerController()
        {
            if (_playerService == null)
            {
                _playerService = ServiceRegistry.Get<IPlayerService>();
            }
            
            if (_playerService?.CurrentPlayer != null)
            {
                _playerController = _playerService.CurrentPlayer.Controller;
                if (_playerController != null)
                {
                    Debug.Log("CrosshairPanel: PlayerController acquired");
                }
                else
                {
                    Debug.LogWarning("CrosshairPanel: CurrentPlayer exists but Controller is null");
                }
            }
        }

        #endregion

        #region Crosshair Update

        /// <summary>
        /// Update crosshair each frame
        /// </summary>
        private void UpdateCrosshair()
        {
            // Get current aim point from shooting system
            Vector3 aimPoint = GetCurrentAimPoint();
            
            // Convert world position to screen position
            Vector3 screenPos = WorldToScreenPoint(aimPoint);
            
            // Update crosshair position
            if (_crosshairContainer != null)
            {
                _crosshairContainer.position = screenPos;
            }
            
            // Update crosshair size based on accuracy
            UpdateCrosshairSize();
            
            // Update crosshair color based on accuracy
            UpdateCrosshairColor();
            
            // Smooth transitions
            SmoothTransitions();
        }

        /// <summary>
        /// Get current aim point from shooting system
        /// </summary>
        private Vector3 GetCurrentAimPoint()
        {
            if (_playerController?.ShootingSystem != null)
            {
                Vector3 aimPoint = _playerController.ShootingSystem.GetCurrentMouseTargetPoint();
                _lastAimPoint = aimPoint;
                return aimPoint;
            }
            
            // Fallback: use mouse world position
            if (_mainCamera != null)
            {
                Vector2 mousePos = UnityEngine.InputSystem.Mouse.current?.position.ReadValue() ?? Vector2.zero;
                Ray mouseRay = _mainCamera.ScreenPointToRay(mousePos);
                
                // Simple plane intersection at player height
                Vector3 playerPos = _playerController?.PlayerGameObject?.transform.position ?? Vector3.zero;
                Plane plane = new Plane(Vector3.up, playerPos);
                if (plane.Raycast(mouseRay, out float distance))
                {
                    Vector3 aimPoint = mouseRay.GetPoint(distance);
                    _lastAimPoint = aimPoint;
                    return aimPoint;
                }
            }
            
            return _lastAimPoint;
        }

        /// <summary>
        /// Convert world position to screen position
        /// </summary>
        private Vector3 WorldToScreenPoint(Vector3 worldPos)
        {
            if (_mainCamera == null) return Vector3.zero;
            
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);
            
            // Check if the point is behind the camera
            if (screenPos.z < 0)
            {
                return Vector3.zero;
            }
            
            // Convert to UI space if using Screen Space - Camera canvas
            if (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPos, _canvas.worldCamera, out localPoint);
                return localPoint;
            }
            
            return screenPos;
        }

        /// <summary>
        /// Update crosshair size based on weapon accuracy configuration
        /// </summary>
        private void UpdateCrosshairSize()
        {
            if (_playerController?.ShootingSystem == null || _playerController?.WeaponManager?.CurrentGun == null)
            {
                _targetSize = _baseCrosshairSize;
                return;
            }
            
            // Get current weapon's accuracy configuration
            var weapon = _playerController.WeaponManager.CurrentGun;
            var accuracyConfig = weapon.accuracyConfig;
            
            if (accuracyConfig == null)
            {
                _targetSize = _baseCrosshairSize;
                return;
            }
            
            // Get current crosshair radius from accuracy system (world units)
            float worldRadius = _playerController.ShootingSystem.GetCurrentCrosshairRadius();
            
            // Convert world radius to screen size based on weapon configuration
            float screenRadius = WorldRadiusToScreenSize(worldRadius, accuracyConfig);
            
            // Clamp to reasonable range
            screenRadius = Mathf.Clamp(screenRadius, _baseCrosshairSize, _maxCrosshairSize);
            
            _targetSize = screenRadius;
        }

        /// <summary>
        /// Convert world radius to screen size based on weapon accuracy configuration
        /// </summary>
        private float WorldRadiusToScreenSize(float worldRadius, WeaponAccuracyConfig accuracyConfig)
        {
            if (_mainCamera == null) return _baseCrosshairSize;
            
            // Get the aim point
            Vector3 aimPoint = GetCurrentAimPoint();
            
            // Calculate screen size based on distance from camera
            float distance = Vector3.Distance(_mainCamera.transform.position, aimPoint);
            
            // Avoid division by zero
            if (distance < 0.1f) distance = 0.1f;
            
            // Use weapon's base radius as reference for screen size calculation
            float baseWorldRadius = accuracyConfig.baseRadius;
            float normalizedRadius = worldRadius / baseWorldRadius;
            
            // Calculate base screen size from weapon's base radius
            // Use field of view to calculate proper screen size
            float fov = _mainCamera.fieldOfView;
            float screenHeight = Screen.height;
            float baseScreenSize = (baseWorldRadius / distance) * screenHeight / (2f * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad));
            
            // Apply normalized radius to get final screen size
            float screenSize = baseScreenSize * normalizedRadius;
            
            return screenSize;
        }

        /// <summary>
        /// Update crosshair color based on accuracy
        /// </summary>
        private void UpdateCrosshairColor()
        {
            if (_playerController?.ShootingSystem == null)
            {
                _targetColor = _accurateColor;
                return;
            }
            
            float accuracyPercentage = _playerController.ShootingSystem.GetAccuracyPercentage();
            _targetColor = Color.Lerp(_inaccurateColor, _accurateColor, accuracyPercentage);
        }

        /// <summary>
        /// Smooth transitions for size and color
        /// </summary>
        private void SmoothTransitions()
        {
            // Smooth size transition
            _currentSize = Mathf.Lerp(_currentSize, _targetSize, _sizeTransitionSpeed * Time.deltaTime);
            
            // Apply size to crosshair circle
            Vector2 size = Vector2.one * _currentSize;
            if (_crosshairContainer != null)
            {
                _crosshairContainer.sizeDelta = size;
            }
            
            if (_crosshairCircle != null)
            {
                _crosshairCircle.rectTransform.sizeDelta = size;
            }
            
            // Smooth color transition
            _currentColor = Color.Lerp(_currentColor, _targetColor, _colorTransitionSpeed * Time.deltaTime);
            
            // Apply color to crosshair circle
            if (_crosshairCircle != null)
            {
                _crosshairCircle.color = _currentColor;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Check if crosshair should be shown
        /// </summary>
        private bool ShouldShowCrosshair()
        {
            return _playerController != null && 
                   _playerController.IsAiming && 
                   _playerController.HasEquippedWeapon;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get current crosshair size for debugging
        /// </summary>
        public float GetCurrentCrosshairSize()
        {
            return _currentSize;
        }

        /// <summary>
        /// Get current accuracy percentage for debugging
        /// </summary>
        public float GetCurrentAccuracyPercentage()
        {
            return _playerController?.ShootingSystem?.GetAccuracyPercentage() ?? 0f;
        }

        #endregion
    }
}
