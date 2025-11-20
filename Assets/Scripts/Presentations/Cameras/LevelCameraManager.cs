using UnityEngine;
using Unity.Cinemachine;
using Resonance.Core;
using Resonance.Core.StateMachine.States;
using Resonance.Gameplay.Player.Actions;
using Resonance.Gameplay.Enemies.Triggers;
using Resonance.Shared.Interfaces;
using Resonance.Shared.Interfaces.Services;

namespace Resonance.Cameras
{
    /// <summary>
    /// Level-specific camera manager that extends CameraManager with gameplay-specific features.
    /// Handles automatic player following setup and wave action camera switching.
    /// </summary>
    public class LevelCameraManager : CameraManager
    {
        [Header("Level Camera Settings")]
        [SerializeField] private string _fixedCameraName = "CM_FixCamera";
        [SerializeField] private string _playerCameraName = "CM_PlayerCamera";
        
        [Header("Player Following")]
        [SerializeField] private bool _autoSetupPlayerFollow = true;
        [SerializeField] private Vector3 _playerCameraOffset = new Vector3(0, 2, 0);
        
        [Header("Camera Impulse Sources")]
        [SerializeField] private string _impulseSourceParentName = "Camera_Impulse_Source";
        [SerializeField] private string _shootRecoilImpulseName = "Player_Shoot_Recoil_Impulse";
        [SerializeField] private string _playerHitImpulseName = "Player_Hit_Impulse";
        [SerializeField] private string _waveAttackMissImpulseName = "Player_Wave_Attack_Miss_Impulse";
        
        // State tracking
        private bool _isInWaveMode = false;
        private Transform _playerTransform;
        
        // Impulse source references
        private CinemachineImpulseSource _shootRecoilImpulse;
        private CinemachineImpulseSource _playerHitImpulse;
        private CinemachineImpulseSource _waveAttackMissImpulse;
        
        // Events
        public System.Action<bool> OnWaveModeChanged;
        
        // Properties
        public bool IsInWaveMode => _isInWaveMode;
        public string FixedCameraName => _fixedCameraName;
        public string PlayerCameraName => _playerCameraName;
        
        protected override void Start()
        {
            base.Start();
            
            // Setup impulse sources
            SetupImpulseSources();
            
            // Setup player following after base initialization
            if (_autoSetupPlayerFollow)
            {
                SetupPlayerFollowing();
            }
            
            // Subscribe to wave action events
            SubscribeToWaveEvents();
            
            // Ensure we start with the fixed camera
            SwitchToFixedCamera();
        }
        
        /// <summary>
        /// Setup and cache impulse source references
        /// </summary>
        private void SetupImpulseSources()
        {
            Transform impulseParent = transform.Find(_impulseSourceParentName);
            if (impulseParent == null)
            {
                Debug.LogWarning($"LevelCameraManager: Impulse source parent '{_impulseSourceParentName}' not found!");
                return;
            }
            
            // Find shoot recoil impulse
            Transform shootRecoilTransform = impulseParent.Find(_shootRecoilImpulseName);
            if (shootRecoilTransform != null)
            {
                _shootRecoilImpulse = shootRecoilTransform.GetComponent<CinemachineImpulseSource>();
                if (_shootRecoilImpulse != null)
                {
                    Debug.Log($"LevelCameraManager: Found shoot recoil impulse source: {_shootRecoilImpulseName}");
                }
            }
            else
            {
                Debug.LogWarning($"LevelCameraManager: Shoot recoil impulse '{_shootRecoilImpulseName}' not found!");
            }
            
            // Find player hit impulse
            Transform playerHitTransform = impulseParent.Find(_playerHitImpulseName);
            if (playerHitTransform != null)
            {
                _playerHitImpulse = playerHitTransform.GetComponent<CinemachineImpulseSource>();
                if (_playerHitImpulse != null)
                {
                    Debug.Log($"LevelCameraManager: Found player hit impulse source: {_playerHitImpulseName}");
                }
            }
            else
            {
                Debug.LogWarning($"LevelCameraManager: Player hit impulse '{_playerHitImpulseName}' not found!");
            }
            
            // Find wave attack miss impulse
            Transform waveAttackMissTransform = impulseParent.Find(_waveAttackMissImpulseName);
            if (waveAttackMissTransform != null)
            {
                _waveAttackMissImpulse = waveAttackMissTransform.GetComponent<CinemachineImpulseSource>();
                if (_waveAttackMissImpulse != null)
                {
                    Debug.Log($"LevelCameraManager: Found wave attack miss impulse source: {_waveAttackMissImpulseName}");
                }
            }
            else
            {
                Debug.LogWarning($"LevelCameraManager: Wave attack miss impulse '{_waveAttackMissImpulseName}' not found!");
            }
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
        /// Subscribe to wave action events for automatic camera switching
        /// </summary>
        private void SubscribeToWaveEvents()
        {
            WaveState.OnWaveStateEntered += OnWaveStateEntered;
            WaveState.OnWaveStateExited += OnWaveStateExited;
        }
        
        /// <summary>
        /// Unsubscribe from wave action events
        /// </summary>
        private void UnsubscribeFromWaveEvents()
        {
            WaveState.OnWaveStateEntered -= OnWaveStateEntered;
            WaveState.OnWaveStateExited -= OnWaveStateExited;
        }
        
        /// <summary>
        /// Handle wave action started - switch to player camera
        /// </summary>
        private void OnWaveStateEntered()
        {
            Debug.Log("LevelCameraManager: Wave started, switching to player camera");
            SwitchToPlayerCamera();
        }
        
        /// <summary>
        /// Handle wave state exited - switch back to fixed camera
        /// </summary>
        private void OnWaveStateExited()
        {
            Debug.Log("LevelCameraManager: Wave state exited, switching to fixed camera");
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
                _isInWaveMode = false;
                OnWaveModeChanged?.Invoke(false);
                Debug.Log("LevelCameraManager: Switched to fixed camera");
            }
            
            return success;
        }
        
        /// <summary>
        /// Switch to the player camera (wave camera)
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
                _isInWaveMode = true;
                OnWaveModeChanged?.Invoke(true);
                Debug.Log("LevelCameraManager: Switched to player camera");
            }
            
            return success;
        }
        
        /// <summary>
        /// Manually toggle between fixed and player cameras
        /// </summary>
        public void ToggleCamera()
        {
            if (_isInWaveMode)
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
        /// Configure camera settings for better wave experience
        /// </summary>
        public void ConfigureWaveCamera(float fieldOfView = 65f, float followDamping = 1.2f)
        {
            if (!HasCamera(_playerCameraName)) return;
            
            var playerCamera = GetCamera<CinemachineCamera>(_playerCameraName);
            if (playerCamera != null)
            {
                // Adjust field of view for better wave view
                playerCamera.Lens.FieldOfView = fieldOfView;
                
                // TODO: Configure follow damping if needed
                // This would require accessing the camera's body component
                
                Debug.Log($"LevelCameraManager: Configured wave camera settings (FOV: {fieldOfView})");
            }
        }
        
        #region Camera Impulse Sources
        
        /// <summary>
        /// Get the shoot recoil impulse source
        /// Used for shooting camera shake
        /// </summary>
        public CinemachineImpulseSource GetShootRecoilImpulse()
        {
            return _shootRecoilImpulse;
        }
        
        /// <summary>
        /// Get the player hit impulse source
        /// Used for player taking damage camera shake
        /// </summary>
        public CinemachineImpulseSource GetPlayerHitImpulse()
        {
            return _playerHitImpulse;
        }
        
        /// <summary>
        /// Get the wave attack miss impulse source
        /// Used for wave attack miss camera shake
        /// </summary>
        public CinemachineImpulseSource GetWaveAttackMissImpulse()
        {
            return _waveAttackMissImpulse;
        }
        
        #endregion
        
        protected override void OnDestroy()
        {
            UnsubscribeFromWaveEvents();
            OnWaveModeChanged = null;
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
