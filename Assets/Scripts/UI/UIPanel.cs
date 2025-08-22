using UnityEngine;
using Resonance.Core;

namespace Resonance.UI
{
    /// <summary>
    /// Base class for all UI panels. Provides common functionality
    /// and implements the IUIPanel interface.
    /// </summary>
    public abstract class UIPanel : MonoBehaviour, IUIPanel
    {
        [Header("Panel Configuration")]
        [SerializeField] protected string _panelName;
        [SerializeField] protected UILayer _layer = UILayer.Menu;
        [SerializeField] protected CanvasGroup _canvasGroup;
        [SerializeField] protected bool _hideOnStart = true;

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

            // Auto-find CanvasGroup if not assigned
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            // Set initial visibility
            if (_hideOnStart)
            {
                SetVisibility(false, true);
            }
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
            
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }

        // Override these methods in derived classes for custom behavior
        protected virtual void OnInitialize() { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnCleanup() { }
    }
}
