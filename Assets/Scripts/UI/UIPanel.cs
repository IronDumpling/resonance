using UnityEngine;
using Resonance.Core;

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

        [Header("UI Components")]
        [SerializeField] protected CanvasGroup _mainCanvasGroup;
        [SerializeField] protected GameObject[] _uiComponents; // Child UI elements to manage
        [SerializeField] protected CanvasGroup[] _componentCanvasGroups; // Individual canvas groups for fine control

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
                Debug.Log($"UIPanel {_panelName}: Auto-discovered {_uiComponents.Length} UI components");
            }

            // Auto-discover canvas groups from components
            if (_componentCanvasGroups == null || _componentCanvasGroups.Length == 0)
            {
                var foundCanvasGroups = new System.Collections.Generic.List<CanvasGroup>();
                
                foreach (var component in _uiComponents)
                {
                    if (component != null)
                    {
                        var canvasGroup = component.GetComponent<CanvasGroup>();
                        if (canvasGroup != null)
                        {
                            foundCanvasGroups.Add(canvasGroup);
                        }
                    }
                }
                
                _componentCanvasGroups = foundCanvasGroups.ToArray();
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
            
            // Control main canvas group
            if (_mainCanvasGroup != null)
            {
                _mainCanvasGroup.alpha = visible ? 1f : 0f;
                _mainCanvasGroup.interactable = visible;
                _mainCanvasGroup.blocksRaycasts = visible;
            }

            // Control individual component canvas groups
            if (_componentCanvasGroups != null)
            {
                foreach (var canvasGroup in _componentCanvasGroups)
                {
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = visible ? 1f : 0f;
                        canvasGroup.interactable = visible;
                        canvasGroup.blocksRaycasts = visible;
                    }
                }
            }

            // Fallback: control individual UI components
            if (_uiComponents != null)
            {
                foreach (var component in _uiComponents)
                {
                    if (component != null)
                    {
                        component.SetActive(visible);
                    }
                }
            }

            // Last resort: control entire GameObject
            if (_mainCanvasGroup == null && (_uiComponents == null || _uiComponents.Length == 0))
            {
                gameObject.SetActive(visible);
            }
        }

        // Public methods for managing individual UI components
        public void ShowComponent(int componentIndex)
        {
            if (_uiComponents != null && componentIndex >= 0 && componentIndex < _uiComponents.Length)
            {
                if (_uiComponents[componentIndex] != null)
                {
                    _uiComponents[componentIndex].SetActive(true);
                }
            }
        }

        public void HideComponent(int componentIndex)
        {
            if (_uiComponents != null && componentIndex >= 0 && componentIndex < _uiComponents.Length)
            {
                if (_uiComponents[componentIndex] != null)
                {
                    _uiComponents[componentIndex].SetActive(false);
                }
            }
        }

        public GameObject GetComponent(int componentIndex)
        {
            if (_uiComponents != null && componentIndex >= 0 && componentIndex < _uiComponents.Length)
            {
                return _uiComponents[componentIndex];
            }
            return null;
        }

        public T GetUIComponent<T>(int componentIndex) where T : Component
        {
            var component = GetComponent(componentIndex);
            return component?.GetComponent<T>();
        }

        // Override these methods in derived classes for custom behavior
        protected virtual void OnInitialize() { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnCleanup() { }
    }
}
