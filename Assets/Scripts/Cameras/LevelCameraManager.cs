using UnityEngine;
using Unity.Cinemachine;
using Resonance.Core;
using Resonance.Interfaces.Services;
using Resonance.Player.Actions;

namespace Resonance.Cameras
{
    /// <summary>
    /// Level-specific camera manager that extends CameraManager with gameplay-specific features.
    /// Handles automatic player following setup and resonance action camera switching.
    /// </summary>
    public class LevelCameraManager : CameraManager
    {
        [Header("Level Camera Settings")]
        [SerializeField] private string _fixedCameraName = "CM_FixCamera";
        [SerializeField] private string _playerCameraName = "CM_PlayerCamera";
        
        [Header("Player Following")]
        [SerializeField] private bool _autoSetupPlayerFollow = true;
        [SerializeField] private Vector3 _playerCameraOffset = new Vector3(0, 2, 0);
        
        // State tracking
        private bool _isInResonanceMode = false;
        private Transform _playerTransform;
        
        // Events
        public System.Action<bool> OnResonanceModeChanged;
        
        // Properties
        public bool IsInResonanceMode => _isInResonanceMode;
        public string FixedCameraName => _fixedCameraName;
        public string PlayerCameraName => _playerCameraName;
        
        protected override void Start()
        {
            base.Start();
            
            // Setup player following after base initialization
            if (_autoSetupPlayerFollow)
            {
                SetupPlayerFollowing();
            }
            
            // Subscribe to resonance action events
            SubscribeToResonanceEvents();
            
            // Ensure we start with the fixed camera
            SwitchToFixedCamera();
        }
        
        /// <summary>
        /// Automatically find and configure player following for the player camera
        /// </summary>
        private void SetupPlayerFollowing()
        {
            // Get player through GameManager services
            var gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogWarning("LevelCameraManager: GameManager not found, cannot setup player following");
                return;
            }
            
            var playerService = gameManager.Services.GetService<IPlayerService>();
            if (playerService?.CurrentPlayer == null)
            {
                Debug.LogWarning("LevelCameraManager: Player not found, cannot setup player following");
                // Try again in a moment
                Invoke(nameof(SetupPlayerFollowing), 1f);
                return;
            }
            
            _playerTransform = playerService.CurrentPlayer.transform;
            
            // Configure the player camera to follow the player
            if (HasCamera(_playerCameraName))
            {
                SetCameraTarget(_playerCameraName, _playerTransform, _playerTransform);
                Debug.Log($"LevelCameraManager: Configured {_playerCameraName} to follow player");
            }
            else
            {
                Debug.LogWarning($"LevelCameraManager: Player camera '{_playerCameraName}' not found!");
            }
        }
        
        /// <summary>
        /// Subscribe to resonance action events for automatic camera switching
        /// </summary>
        private void SubscribeToResonanceEvents()
        {
            PlayerResonanceAction.OnResonanceActionStarted += OnResonanceStarted;
            PlayerResonanceAction.OnResonanceActionEnded += OnResonanceEnded;
        }
        
        /// <summary>
        /// Unsubscribe from resonance action events
        /// </summary>
        private void UnsubscribeFromResonanceEvents()
        {
            PlayerResonanceAction.OnResonanceActionStarted -= OnResonanceStarted;
            PlayerResonanceAction.OnResonanceActionEnded -= OnResonanceEnded;
        }
        
        /// <summary>
        /// Handle resonance action started - switch to player camera
        /// </summary>
        /// <param name="targetCore">The target core hitbox (not used for camera switching)</param>
        private void OnResonanceStarted(Resonance.Enemies.EnemyHitbox targetCore)
        {
            Debug.Log("LevelCameraManager: Resonance started, switching to player camera");
            SwitchToPlayerCamera();
        }
        
        /// <summary>
        /// Handle resonance action ended - switch back to fixed camera
        /// </summary>
        private void OnResonanceEnded()
        {
            Debug.Log("LevelCameraManager: Resonance ended, switching to fixed camera");
            SwitchToFixedCamera();
        }
        
        /// <summary>
        /// Switch to the fixed camera (default gameplay camera)
        /// </summary>
        public bool SwitchToFixedCamera()
        {
            if (!HasCamera(_fixedCameraName))
            {
                Debug.LogWarning($"LevelCameraManager: Fixed camera '{_fixedCameraName}' not found!");
                return false;
            }
            
            bool success = SwitchToCamera(_fixedCameraName);
            if (success)
            {
                _isInResonanceMode = false;
                OnResonanceModeChanged?.Invoke(false);
                Debug.Log("LevelCameraManager: Switched to fixed camera");
            }
            
            return success;
        }
        
        /// <summary>
        /// Switch to the player camera (resonance camera)
        /// </summary>
        public bool SwitchToPlayerCamera()
        {
            if (!HasCamera(_playerCameraName))
            {
                Debug.LogWarning($"LevelCameraManager: Player camera '{_playerCameraName}' not found!");
                return false;
            }
            
            // Ensure player following is set up
            if (_playerTransform == null)
            {
                SetupPlayerFollowing();
            }
            
            bool success = SwitchToCamera(_playerCameraName);
            if (success)
            {
                _isInResonanceMode = true;
                OnResonanceModeChanged?.Invoke(true);
                Debug.Log("LevelCameraManager: Switched to player camera");
            }
            
            return success;
        }
        
        /// <summary>
        /// Manually toggle between fixed and player cameras
        /// </summary>
        public void ToggleCamera()
        {
            if (_isInResonanceMode)
            {
                SwitchToFixedCamera();
            }
            else
            {
                SwitchToPlayerCamera();
            }
        }
        
        /// <summary>
        /// Update player camera target if player changes
        /// </summary>
        public void RefreshPlayerTarget()
        {
            if (_autoSetupPlayerFollow)
            {
                SetupPlayerFollowing();
            }
        }
        
        /// <summary>
        /// Get the current camera type
        /// </summary>
        /// <returns>True if in player camera mode, false if in fixed camera mode</returns>
        public bool IsCurrentCameraPlayerCamera()
        {
            return CurrentCameraName == _playerCameraName;
        }
        
        /// <summary>
        /// Configure camera settings for better resonance experience
        /// </summary>
        public void ConfigureResonanceCamera(float fieldOfView = 65f, float followDamping = 1.2f)
        {
            if (!HasCamera(_playerCameraName)) return;
            
            var playerCamera = GetCamera<CinemachineCamera>(_playerCameraName);
            if (playerCamera != null)
            {
                // Adjust field of view for better resonance view
                playerCamera.Lens.FieldOfView = fieldOfView;
                
                // TODO: Configure follow damping if needed
                // This would require accessing the camera's body component
                
                Debug.Log($"LevelCameraManager: Configured resonance camera settings (FOV: {fieldOfView})");
            }
        }
        
        protected override void OnDestroy()
        {
            UnsubscribeFromResonanceEvents();
            OnResonanceModeChanged = null;
            base.OnDestroy();
        }
        
        // Development/Debug methods
        #if UNITY_EDITOR
        [ContextMenu("Test Switch to Player Camera")]
        private void TestSwitchToPlayerCamera()
        {
            SwitchToPlayerCamera();
        }
        
        [ContextMenu("Test Switch to Fixed Camera")]
        private void TestSwitchToFixedCamera()
        {
            SwitchToFixedCamera();
        }
        
        [ContextMenu("Test Toggle Camera")]
        private void TestToggleCamera()
        {
            ToggleCamera();
        }
        #endif
    }
}
