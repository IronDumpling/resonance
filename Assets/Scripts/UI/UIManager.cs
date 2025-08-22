using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;

namespace Resonance.UI
{
    /// <summary>
    /// UIManager acts as a local coordinator that registers scene-specific UI panels
    /// with the global UIService. This allows each scene to manage its own UI components
    /// while still being coordinated by the global state-driven UI system.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Scene UI Panels")]
        [SerializeField] private UIPanel[] _scenePanels;

        private IUIService _uiService;

        void Start()
        {
            // Get the global UI service
            _uiService = ServiceRegistry.Get<IUIService>();
            
            if (_uiService == null)
            {
                Debug.LogError("UIManager: UIService not found. Make sure GameManager is initialized.");
                return;
            }

            // Register all scene panels with the global service
            RegisterScenePanels();
        }

        private void RegisterScenePanels()
        {
            if (_scenePanels == null) return;

            foreach (var panel in _scenePanels)
            {
                if (panel != null)
                {
                    _uiService.RegisterPanel(panel);
                    Debug.Log($"UIManager: Registered panel {panel.PanelName} from scene {gameObject.scene.name}");
                }
            }
        }

        void OnDestroy()
        {
            // Unregister panels when scene is destroyed
            if (_uiService != null && _scenePanels != null)
            {
                foreach (var panel in _scenePanels)
                {
                    if (panel != null)
                    {
                        _uiService.UnregisterPanel(panel.PanelName);
                    }
                }
            }
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
