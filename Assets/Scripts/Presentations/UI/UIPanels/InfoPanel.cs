using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Resonance.Core;
using Resonance.Gameplay.Items;
using Resonance.Utilities;
using Resonance.Shared.Interfaces.Services;
using Resonance.Core.StateMachine.States;

namespace Resonance.Presentations.UI
{
    public class InfoPanel : UIPanel
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _infoName;
        [SerializeField] private TextMeshProUGUI _infoContent;
        [SerializeField] private Image _infoImage;

        // Current info data
        private InfoDataAsset _currentInfoData;

        private IInputService _inputService;

        protected override void Awake()
        {
            base.Awake();

            _panelName = "InfoPanel";
            _layer = UILayer.Game;
            _hideOnStart = true;
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            AutoDiscoverUIComponents();
            SetupEventListeners();
        }

        private void OnDestroy()
        {
            CleanupEventListeners();
        }
        
        private void AutoDiscoverUIComponents()
        {
            if (_infoName == null)
                _infoName = transform.Find("InfoName")?.GetComponent<TextMeshProUGUI>();
            if (_infoContent == null)
                _infoContent = transform.Find("InfoContent")?.GetComponent<TextMeshProUGUI>();
            if (_infoImage == null)
                _infoImage = transform.Find("InfoImage")?.GetComponent<Image>();
        }

        private void SetupEventListeners()
        {
            // Subscribe to InfoReadingState events
            InfoReadingState.OnInfoReadingStarted += OnInfoReadingStarted;

            _inputService = ServiceRegistry.Get<IInputService>();
            if (_inputService != null)
            {
                _inputService.OnInformationClose += CloseInfoPanel;
                Debug.Log("InfoPanel: Subscribed to InformationClose input events");
            }
            
            Debug.Log("InfoPanel: Event listeners setup complete");
        }

        private void CleanupEventListeners()
        {
            // Unsubscribe from InfoReadingState events
            InfoReadingState.OnInfoReadingStarted -= OnInfoReadingStarted;

            if (_inputService != null)
            {
                _inputService.OnInformationClose -= CloseInfoPanel;
                Debug.Log("InfoPanel: Unsubscribed from InformationClose input events");
            }
            
            Debug.Log("InfoPanel: Event listeners cleaned up");
        }

        private void OnInfoReadingStarted(InfoDataAsset infoData)
        {
            Debug.Log($"InfoPanel: Info reading started for {infoData?.infoName ?? "Unknown"}");
            DisplayInfoData(infoData);
        }

        /// <summary>
        /// Display the info data in the panel
        /// </summary>
        /// <param name="infoData">Info data to display</param>
        public void DisplayInfoData(InfoDataAsset infoData)
        {
            _currentInfoData = infoData;
            
            if (infoData == null)
            {
                Debug.LogWarning("InfoPanel: Cannot display null InfoDataAsset");
                return;
            }

            // Update UI elements
            if (_infoName != null)
                _infoName.text = infoData.infoName ?? "Unknown Info";
            
            if (_infoContent != null)
                _infoContent.text = infoData.infoContent ?? "No content available.";
            
            if (_infoImage != null && infoData.infoImage != null)
            {
                _infoImage.sprite = infoData.infoImage;
                _infoImage.gameObject.SetActive(true);
            }
            else if (_infoImage != null)
            {
                _infoImage.gameObject.SetActive(false);
            }

            Debug.Log($"InfoPanel: Displayed info data for {infoData.infoName}");
        }

        /// <summary>
        /// Close the info panel and return to normal gameplay
        /// </summary>
        private void CloseInfoPanel()
        {
            Debug.Log("InfoPanel: Closing info panel");
            
            // Trigger the info reading end event via static method
            InfoReadingState.TriggerInfoReadingEnd();
            
            // Clear current data
            _currentInfoData = null;
        }

        /// <summary>
        /// Get the currently displayed info data
        /// </summary>
        /// <returns>Current info data or null if none</returns>
        public InfoDataAsset GetCurrentInfoData()
        {
            return _currentInfoData;
        }
    }
}