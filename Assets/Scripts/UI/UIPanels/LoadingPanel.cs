using UnityEngine;
using UnityEngine.UI;
using Resonance.Core;

namespace Resonance.UI
{
    /// <summary>
    /// LoadingPanel displays loading progress and status information.
    /// Used during game initialization and scene transitions.
    /// </summary>
    public class LoadingPanel : UIPanel
    {
        [Header("Loading UI Elements")]
        [SerializeField] private Text _statusText;
        [SerializeField] private Slider _progressBar;
        [SerializeField] private Image _loadingIcon;
        
        [Header("Animation")]
        [SerializeField] private float _iconRotationSpeed = 90f;
        
        private bool _isLoading = false;
        
        protected override void Awake()
        {
            base.Awake();
            
            // Set panel configuration
            _panelName = "LoadingPanel";
            _layer = UILayer.Overlay;
            _hideOnStart = false; // Loading panel might be shown at start
        }

        protected override void Start()
        {
            base.Start();
            
            // Auto-find UI components if not assigned
            if (_statusText == null)
                _statusText = GetComponentInChildren<Text>();
            if (_progressBar == null)
                _progressBar = GetComponentInChildren<Slider>();
            if (_loadingIcon == null)
                _loadingIcon = transform.Find("LoadingIcon")?.GetComponent<Image>();
            
            // Initialize UI
            SetStatus("Initializing...");
            SetProgress(0f);
        }

        void Update()
        {
            // Animate loading icon
            if (_isLoading && _loadingIcon != null)
            {
                _loadingIcon.transform.Rotate(0, 0, -_iconRotationSpeed * Time.deltaTime);
            }
        }

        public void SetStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = status;
            }
            Debug.Log($"LoadingPanel: {status}");
        }

        public void SetProgress(float progress)
        {
            if (_progressBar != null)
            {
                _progressBar.value = Mathf.Clamp01(progress);
            }
        }

        public void StartLoading(string status = "Loading...")
        {
            _isLoading = true;
            SetStatus(status);
            SetProgress(0f);
            Show();
        }

        public void FinishLoading(string status = "Complete!")
        {
            _isLoading = false;
            SetStatus(status);
            SetProgress(1f);
            
            // Auto-hide after a short delay
            Invoke(nameof(Hide), 0.5f);
        }

        protected override void OnShow()
        {
            base.OnShow();
            _isLoading = true;
            Debug.Log("LoadingPanel: Shown");
        }

        protected override void OnHide()
        {
            base.OnHide();
            _isLoading = false;
            Debug.Log("LoadingPanel: Hidden");
        }
    }
}
