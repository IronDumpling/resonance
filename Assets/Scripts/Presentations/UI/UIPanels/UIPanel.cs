using UnityEngine;
using Resonance.Core;
using Resonance.Interfaces.Services;

namespace Resonance.UI
{
    /// <summary>
    /// UIPanel acts as a container for multiple Canvas UI components.
    /// It's designed to be used as a prefab and provides unified control
    /// over a group of related UI elements.
    /// </summary>
    public class UIPanel : MonoBehaviour, IUIPanel
    {
        [Header("Panel Configuration")]
        [SerializeField] protected string _panelName;
        [SerializeField] protected UILayer _layer = UILayer.Menu;
        [SerializeField] protected bool _hideOnStart = true;

        // UI Components
        private CanvasGroup _mainCanvasGroup;
        private GameObject[] _uiComponents; // Child UI elements to manage
        private CanvasGroup[] _componentCanvasGroups; // Individual canvas groups for fine control

        public string PanelName => _panelName;
        public UILayer Layer => _layer;
        public bool IsVisible { get; private set; }

        protected virtual void Awake()
        {
            // Auto-assign panel name if not set
            if (string.IsNullOrEmpty(_panelName))
            {
                _panelName = gameObject.name;
            }

            // Auto-discover UI components and canvas groups
            AutoDiscoverComponents();

            // Set initial visibility
            if (_hideOnStart)
            {
                SetVisibility(false, true);
            }
        }

        private void AutoDiscoverComponents()
        {
            // Auto-find main CanvasGroup if not assigned
            if (_mainCanvasGroup == null)
            {
                _mainCanvasGroup = GetComponent<CanvasGroup>();
                if (_mainCanvasGroup == null)
                {
                    _mainCanvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            // Auto-discover child UI components if not manually assigned
            if (_uiComponents == null || _uiComponents.Length == 0)
            {
                // Find all child GameObjects with UI components (Canvas, Image, Text, Button, etc.)
                var foundComponents = new System.Collections.Generic.List<GameObject>();
                
                foreach (Transform child in transform)
                {
                    if (HasUIComponent(child.gameObject))
                    {
                        foundComponents.Add(child.gameObject);
                    }
                }
                
                _uiComponents = foundComponents.ToArray();
            }
        }

        private bool HasUIComponent(GameObject obj)
        {
            // Check for common UI components
            return obj.GetComponent<UnityEngine.UI.Image>() != null ||
                   obj.GetComponent<UnityEngine.UI.Text>() != null ||
                   obj.GetComponent<UnityEngine.UI.Button>() != null ||
                   obj.GetComponent<UnityEngine.UI.Toggle>() != null ||
                   obj.GetComponent<UnityEngine.UI.Slider>() != null ||
                   obj.GetComponent<UnityEngine.UI.InputField>() != null ||
                   obj.GetComponent<UnityEngine.UI.Dropdown>() != null ||
                   obj.GetComponent<Canvas>() != null;
        }

        protected virtual void Start()
        {
            // Panels should be initialized by UIManager, not self-initialize
        }

        public virtual void Initialize()
        {
            Debug.Log($"UIPanel: Initializing panel {_panelName}");
            OnInitialize();
        }

        public virtual void Show()
        {
            if (!IsVisible)
            {
                SetVisibility(true);
                OnShow();
            }
        }

        public virtual void Hide()
        {
            if (IsVisible)
            {
                SetVisibility(false);
                OnHide();
            }
        }

        public virtual void Cleanup()
        {
            Debug.Log($"UIPanel: Cleaning up panel {_panelName}");
            OnCleanup();
        }

        protected virtual void SetVisibility(bool visible, bool immediate = false)
        {
            IsVisible = visible;
            
            // Primary method: Use CanvasGroup for performance (recommended)
            if (_mainCanvasGroup != null)
            {
                _mainCanvasGroup.alpha = visible ? 1f : 0f;
                _mainCanvasGroup.interactable = visible;
                _mainCanvasGroup.blocksRaycasts = visible;
                
                // Don't fall through to other methods if CanvasGroup exists
                return;
            }

            // Fallback method: Control entire GameObject
            // This should rarely happen since we auto-create CanvasGroup above
            Debug.LogWarning($"UIPanel {_panelName}: No CanvasGroup found, using GameObject.SetActive() as fallback.");
            gameObject.SetActive(visible);
        }

        // Override these methods in derived classes for custom behavior
        protected virtual void OnInitialize() { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnCleanup() { }
    }
}
