using UnityEngine;
using UnityEngine.UI;
using Resonance.Core;
using Resonance.Utilities;

namespace Resonance.UI
{
    /// <summary>
    /// MainMenuPanel handles the main menu UI interactions and navigation.
    /// Designed to be attached directly to a Canvas GameObject.
    /// </summary>
    public class MainMenuPanel : UIPanel
    {
        [Header("Main Menu Buttons")]
        [SerializeField] private Button _startGameButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;
        
        [Header("Services")]
        private ILoadSceneService _sceneService;
        
        protected override void Awake()
        {
            base.Awake();
            
            // Set panel configuration
            _panelName = "MainMenuPanel";
            _layer = UILayer.Menu;
        }

        protected override void Start()
        {
            base.Start();
            
            // Get services
            _sceneService = ServiceRegistry.Get<ILoadSceneService>();
            
            // Setup button events
            SetupButtonEvents();
        }

        private void SetupButtonEvents()
        {
            // Auto-find buttons if not assigned
            if (_startGameButton == null)
                _startGameButton = FindButtonByName("StartGame", "Start", "Play");
            if (_settingsButton == null)
                _settingsButton = FindButtonByName("Settings", "Options");
            if (_quitButton == null)
                _quitButton = FindButtonByName("Quit", "Exit");

            // Setup events
            if (_startGameButton != null)
                _startGameButton.onClick.AddListener(OnStartGameClicked);
            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(OnSettingsClicked);
            if (_quitButton != null)
                _quitButton.onClick.AddListener(OnQuitClicked);
        }

        private Button FindButtonByName(params string[] possibleNames)
        {
            foreach (string name in possibleNames)
            {
                var found = transform.Find(name);
                if (found != null)
                {
                    var button = found.GetComponent<Button>();
                    if (button != null) return button;
                }

                // Search in children
                Button[] buttons = GetComponentsInChildren<Button>();
                foreach (var btn in buttons)
                {
                    if (btn.name.ToLower().Contains(name.ToLower()))
                        return btn;
                }
            }
            return null;
        }

        private void OnStartGameClicked()
        {
            Debug.Log("MainMenu: Start Game clicked");
            
            if (_sceneService != null)
            {
                _sceneService.LoadScene("Level_01");
            }
            else
            {
                Debug.LogError("MainMenu: LoadSceneService not found");
            }
        }

        private void OnSettingsClicked()
        {
            Debug.Log("MainMenu: Settings clicked");
            
            var uiService = ServiceRegistry.Get<IUIService>();
            uiService?.ShowPanel("Settings");
        }

        private void OnQuitClicked()
        {
            Debug.Log("MainMenu: Quit clicked");
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        protected override void OnShow()
        {
            base.OnShow();
            Debug.Log("MainMenu: Panel shown");
        }

        protected override void OnHide()
        {
            base.OnHide();
            Debug.Log("MainMenu: Panel hidden");
        }

        protected override void OnCleanup()
        {
            base.OnCleanup();
            
            // Cleanup button events
            if (_startGameButton != null)
                _startGameButton.onClick.RemoveAllListeners();
            if (_settingsButton != null)
                _settingsButton.onClick.RemoveAllListeners();
            if (_quitButton != null)
                _quitButton.onClick.RemoveAllListeners();
        }
    }
}