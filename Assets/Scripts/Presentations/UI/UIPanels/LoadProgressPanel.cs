using System;
using UnityEngine;
using UnityEngine.UI;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Shared.Interfaces.Services;

namespace Resonance.Presentations.UI
{
    public class LoadProgressPanel : UIPanel
    {
        [Header("Save Slot Buttons")]
        [SerializeField] private Button _saveSlot1Button;
        [SerializeField] private Button _saveSlot2Button;
        [SerializeField] private Button _saveSlot3Button;
        [SerializeField] private Button _saveSlot4Button;
        [SerializeField] private Button _backButton;

        [Header("Services")]
        private ISceneTransitionService _sceneService;
        private GameManager _gameManager;

        public static event Action OnBackToMainMenuRequested;

        protected override void Awake()
        {
            base.Awake();
            
            // Set panel configuration
            _panelName = "LoadProgressPanel";
            _layer = UILayer.Menu;
        }

        protected override void Start()
        {
            base.Start();
            
            // Get services
            _sceneService = ServiceRegistry.Get<ISceneTransitionService>();
            _gameManager = GameManager.Instance;

            SetupButtonEvents();
        }

        private void SetupButtonEvents()
        {
            // Auto-find buttons if not assigned
            if (_saveSlot1Button == null)
                _saveSlot1Button = FindButtonByName("SaveSlot1");
            if (_saveSlot2Button == null)
                _saveSlot2Button = FindButtonByName("SaveSlot2");
            if (_saveSlot3Button == null)
                _saveSlot3Button = FindButtonByName("SaveSlot3");
            if (_saveSlot4Button == null)
                _saveSlot4Button = FindButtonByName("SaveSlot4");
            if (_backButton == null)
                _backButton = FindButtonByName("Back");

            // Setup events
            if (_saveSlot1Button != null)
                _saveSlot1Button.onClick.AddListener(OnSaveSlot1Clicked);
            if (_saveSlot2Button != null)
                _saveSlot2Button.onClick.AddListener(OnSaveSlot2Clicked);
            if (_saveSlot3Button != null)
                _saveSlot3Button.onClick.AddListener(OnSaveSlot3Clicked);
            if (_saveSlot4Button != null)
                _saveSlot4Button.onClick.AddListener(OnSaveSlot4Clicked);
            if (_backButton != null)
                _backButton.onClick.AddListener(OnBackClicked);
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

        private void OnSaveSlot1Clicked()
        {
            Debug.Log("LoadProgressPanel: Save Slot 1 clicked");

            if (_gameManager != null && _gameManager.StateMachine != null)
            {
                // Change to Gameplay top-level state
                bool success = _gameManager.StateMachine.ChangeState("Gameplay");
                if (success)
                {
                    Debug.Log("LoadProgressPanel: Successfully switched to Gameplay state");
                    
                    // Then load the gameplay scene
                    if (_sceneService != null)
                    {
                        _sceneService.LoadScene("room_01");
                    }
                    else
                    {
                        Debug.LogError("LoadProgressPanel: SceneTransitionService not found");
                    }
                }
                else
                {
                    Debug.LogError("LoadProgressPanel: Failed to switch to Gameplay state");
                }
            }
            else
            {
                Debug.LogError("LoadProgressPanel: GameManager or StateMachine not found");
            }
        }

        private void OnSaveSlot2Clicked()
        {
            Debug.Log("LoadProgressPanel: Save Slot 2 clicked");
        }

        private void OnSaveSlot3Clicked()
        {
            Debug.Log("LoadProgressPanel: Save Slot 3 clicked");
        }

        private void OnSaveSlot4Clicked()
        {
            Debug.Log("LoadProgressPanel: Save Slot 4 clicked");
        }

        private void OnBackClicked()
        {
            OnBackToMainMenuRequested?.Invoke();
            Debug.Log("LoadProgressPanel: Back button clicked");
        }

        protected override void OnCleanup()
        {
            base.OnCleanup();

            if (_saveSlot1Button != null)
                _saveSlot1Button.onClick.RemoveAllListeners();
            if (_saveSlot2Button != null)
                _saveSlot2Button.onClick.RemoveAllListeners();
            if (_saveSlot3Button != null)
                _saveSlot3Button.onClick.RemoveAllListeners();
            if (_saveSlot4Button != null)
                _saveSlot4Button.onClick.RemoveAllListeners();
            if (_backButton != null)
                _backButton.onClick.RemoveAllListeners();
        }
    }
}