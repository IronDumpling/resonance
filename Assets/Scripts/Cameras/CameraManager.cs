using UnityEngine;
using Unity.Cinemachine;
using Resonance.Core;
using Resonance.Utilities;
using System.Collections.Generic;

namespace Resonance.Cameras
{
    /// <summary>
    /// Scene-local camera manager that automatically discovers and manages 
    /// Cinemachine cameras in the current scene. Provides a unified interface
    /// for camera switching and management.
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        [Header("Camera Discovery")]
        [SerializeField] private bool _autoDiscoverCameras = true;
        [SerializeField] private CinemachineCamera[] _manualCameras;
        
        [Header("Camera Management")]
        [SerializeField] private string _defaultCameraName;
        
        private CinemachineBrain _brain;
        private Dictionary<string, CinemachineCamera> _cameras = new Dictionary<string, CinemachineCamera>();
        private CinemachineCamera _currentCamera;
        
        // Events
        public System.Action<string> OnCameraChanged;
        
        // Properties
        public CinemachineBrain Brain => _brain;
        public CinemachineCamera CurrentCamera => _currentCamera;
        public string CurrentCameraName => _currentCamera?.name ?? "None";
        
        void Awake()
        {
            // Get or create Cinemachine Brain
            _brain = GetComponentInChildren<CinemachineBrain>();
            if (_brain == null)
            {
                Debug.LogError("CameraManager: No CinemachineBrain found in children!");
                return;
            }
            
            // Discover cameras
            DiscoverCameras();
        }
        
        void Start()
        {
            // Activate default camera
            if (!string.IsNullOrEmpty(_defaultCameraName))
            {
                SwitchToCamera(_defaultCameraName);
            }
            else if (_cameras.Count > 0)
            {
                // Activate the first camera found
                var firstCamera = System.Linq.Enumerable.First(_cameras.Values);
                SwitchToCamera(firstCamera.name);
            }
        }
        
        private void DiscoverCameras()
        {
            _cameras.Clear();
            
            if (_autoDiscoverCameras)
            {
                // Find all Cinemachine cameras in the scene
                CinemachineCamera[] foundCameras = FindObjectsByType<CinemachineCamera>(
                    FindObjectsSortMode.None
                );
                
                foreach (var camera in foundCameras)
                {
                    // Only register cameras in the same scene
                    if (camera.gameObject.scene == gameObject.scene)
                    {
                        RegisterCamera(camera);
                    }
                }
                
                Debug.Log($"CameraManager: Auto-discovered {_cameras.Count} cameras in scene {gameObject.scene.name}");
            }
            
            // Add manually assigned cameras
            if (_manualCameras != null)
            {
                foreach (var camera in _manualCameras)
                {
                    if (camera != null)
                    {
                        RegisterCamera(camera);
                    }
                }
            }
        }
        
        private void RegisterCamera(CinemachineCamera camera)
        {
            if (_cameras.ContainsKey(camera.name))
            {
                Debug.LogWarning($"CameraManager: Camera {camera.name} already registered. Overwriting.");
            }
            
            _cameras[camera.name] = camera;
            
            // Set initial priority (inactive cameras should have priority 0)
            camera.Priority = 0;
            
            Debug.Log($"CameraManager: Registered camera {camera.name}");
        }
        
        /// <summary>
        /// Switch to a specific camera by name
        /// </summary>
        public bool SwitchToCamera(string cameraName)
        {
            if (!_cameras.TryGetValue(cameraName, out CinemachineCamera targetCamera))
            {
                Debug.LogWarning($"CameraManager: Camera {cameraName} not found!");
                return false;
            }
            
            // Deactivate current camera
            if (_currentCamera != null)
            {
                _currentCamera.Priority = 0;
            }
            
            // Activate target camera
            targetCamera.Priority = 10;
            _currentCamera = targetCamera;
            
            OnCameraChanged?.Invoke(cameraName);
            Debug.Log($"CameraManager: Switched to camera {cameraName}");
            
            return true;
        }
        
        /// <summary>
        /// Get a camera by name
        /// </summary>
        public T GetCamera<T>(string cameraName) where T : CinemachineVirtualCameraBase
        {
            if (_cameras.TryGetValue(cameraName, out CinemachineCamera camera))
            {
                return camera as T;
            }
            return null;
        }
        
        /// <summary>
        /// Check if a camera exists
        /// </summary>
        public bool HasCamera(string cameraName)
        {
            return _cameras.ContainsKey(cameraName);
        }
        
        /// <summary>
        /// Get all registered camera names
        /// </summary>
        public string[] GetCameraNames()
        {
            return System.Linq.Enumerable.ToArray(_cameras.Keys);
        }
        
        /// <summary>
        /// Set the follow and look at targets for a specific camera
        /// </summary>
        public void SetCameraTarget(string cameraName, Transform followTarget, Transform lookAtTarget = null)
        {
            if (_cameras.TryGetValue(cameraName, out CinemachineCamera camera))
            {
                // CinemachineCamera directly supports Follow and LookAt
                camera.Follow = followTarget;
                camera.LookAt = lookAtTarget ?? followTarget;
                Debug.Log($"CameraManager: Set targets for camera {cameraName}");
            }
            else
            {
                Debug.LogWarning($"CameraManager: Camera {cameraName} not found for target setting!");
            }
        }
        
        /// <summary>
        /// Enable/disable a specific camera
        /// </summary>
        public void SetCameraEnabled(string cameraName, bool enabled)
        {
            if (_cameras.TryGetValue(cameraName, out CinemachineCamera camera))
            {
                camera.gameObject.SetActive(enabled);
                Debug.Log($"CameraManager: {(enabled ? "Enabled" : "Disabled")} camera {cameraName}");
            }
        }
        
        void OnDestroy()
        {
            OnCameraChanged = null;
        }
    }
}
