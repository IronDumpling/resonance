using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using System.Collections.Generic;
using Resonance.Interfaces.Services;

namespace Resonance.UI
{
    /// <summary>
    /// CanvasUIManager acts as a scene-local coordinator that automatically discovers 
    /// and registers Canvas UI panels with the global UIService. It manages the actual
    /// instantiation and lifecycle of UI prefabs in the scene.
    /// </summary>
    public class CanvasUIManager : MonoBehaviour
    {
        [Header("UI Panel Discovery")]
        [SerializeField] private bool _autoDiscoverPanels = true;
        [SerializeField] private UIPanel[] _manualPanels; // Fallback for manual assignment
        
        [Header("UI Prefab Loading")]
        [SerializeField] private string[] _prefabResourcePaths;
        [SerializeField] private Transform _uiRoot; // Where to instantiate UI prefabs

        private IUIService _uiService;
        private List<UIPanel> _discoveredPanels = new List<UIPanel>();
        private List<GameObject> _instantiatedPrefabs = new List<GameObject>();

        void Start()
        {
            // Get the global UI service
            _uiService = ServiceRegistry.Get<IUIService>();
            
            if (_uiService == null)
            {
                Debug.LogError("CanvasUIManager: UIService not found. Make sure GameManager is initialized.");
                return;
            }

            // Setup UI root if not assigned
            if (_uiRoot == null)
            {
                _uiRoot = transform;
            }

            // Load prefabs and discover panels
            LoadUIPrefabs();
            DiscoverUIPanels();
            RegisterAllPanels();
        }

        private void LoadUIPrefabs()
        {
            if (_prefabResourcePaths == null || _prefabResourcePaths.Length == 0) return;

            foreach (string resourcePath in _prefabResourcePaths)
            {
                if (string.IsNullOrEmpty(resourcePath)) continue;

                GameObject prefab = Resources.Load<GameObject>(resourcePath);
                if (prefab != null)
                {
                    GameObject instance = Instantiate(prefab, _uiRoot);
                    _instantiatedPrefabs.Add(instance);
                    
                    // Directly get and register the UIPanel component
                    UIPanel panel = instance.GetComponent<UIPanel>();
                    if (panel != null)
                    {
                        _discoveredPanels.Add(panel);
                        Debug.Log($"CanvasUIManager: Loaded and registered UI prefab {panel.PanelName} from {resourcePath}");
                    }
                    else
                    {
                        Debug.LogWarning($"CanvasUIManager: Loaded prefab {resourcePath} but no UIPanel component found");
                    }
                }
                else
                {
                    Debug.LogWarning($"CanvasUIManager: Could not load UI prefab at {resourcePath}");
                }
            }
        }

        private void DiscoverUIPanels()
        {
            // Don't clear, as LoadUIPrefabs() already added panels
            // _discoveredPanels.Clear();

            if (_autoDiscoverPanels)
            {
                // Find all UIPanel components in the scene (including children)
                UIPanel[] foundPanels = FindObjectsByType<UIPanel>(
                    FindObjectsSortMode.None
                );
                
                foreach (var panel in foundPanels)
                {
                    // Only register panels that belong to this scene and aren't already registered
                    if (panel.gameObject.scene == gameObject.scene && !_discoveredPanels.Contains(panel))
                    {
                        _discoveredPanels.Add(panel);
                    }
                }
                
                Debug.Log($"CanvasUIManager: Auto-discovered {_discoveredPanels.Count} UI panels in scene");
            }

            // Add manually assigned panels
            if (_manualPanels != null)
            {
                foreach (var panel in _manualPanels)
                {
                    if (panel != null && !_discoveredPanels.Contains(panel))
                    {
                        _discoveredPanels.Add(panel);
                    }
                }
            }
        }

        private void RegisterAllPanels()
        {
            foreach (var panel in _discoveredPanels)
            {
                if (panel != null)
                {
                    _uiService.RegisterPanel(panel);
                    Debug.Log($"CanvasUIManager: Registered Canvas UI panel {panel.PanelName} from scene {gameObject.scene.name}");
                }
            }
            
            _uiService.NotifySceneUIPanelsReady(gameObject.scene.name);
            Debug.Log($"CanvasUIManager: Notified UIService that scene {gameObject.scene.name} UI panels are ready");
        }

        void OnDestroy()
        {
            // Unregister panels when scene is destroyed
            if (_uiService != null && _discoveredPanels != null)
            {
                foreach (var panel in _discoveredPanels)
                {
                    if (panel != null)
                    {
                        _uiService.UnregisterPanel(panel.PanelName);
                    }
                }
            }

            // Clean up instantiated prefabs
            foreach (var prefabInstance in _instantiatedPrefabs)
            {
                if (prefabInstance != null)
                {
                    Destroy(prefabInstance);
                }
            }
            _instantiatedPrefabs.Clear();
        }

        // Helper methods for scene-specific UI operations
        public void ShowPanel(string panelName)
        {
            _uiService?.ShowPanel(panelName);
        }

        public void HidePanel(string panelName)
        {
            _uiService?.HidePanel(panelName);
        }

        public T GetPanel<T>(string panelName) where T : class, IUIPanel
        {
            return _uiService?.GetPanel<T>(panelName);
        }
    }
}
