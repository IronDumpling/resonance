using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Resonance.Core;
using Resonance.Interfaces.Services;

namespace Resonance.Core.GlobalServices
{
    /// <summary>
    /// Global Canvas UI management service. Handles state-driven UI display logic
    /// and coordinates with scene-local CanvasUIManagers for actual UI operations.
    /// </summary>
    public class UIService : IUIService
    {
        public int Priority => 15;
        public SystemState State { get; private set; } = SystemState.Uninitialized;

        public event Action<string> OnPanelShown;
        public event Action<string> OnPanelHidden;
        public event Action<string> OnSceneUIPanelsReady;

        private readonly Dictionary<string, IUIPanel> _panels = new();
        private readonly Dictionary<string, List<string>> _statePanels = new();
        private readonly List<string> _visiblePanels = new();

        public void Initialize()
        {
            if (State != SystemState.Uninitialized)
            {
                Debug.LogWarning("UIService already initialized");
                return;
            }

            State = SystemState.Initializing;
            Debug.Log("UIService: Initializing");

            // Setup default state panel configurations
            SetupDefaultStatePanels();

            State = SystemState.Running;
            Debug.Log("UIService: Initialized successfully");
        }

        private void SetupDefaultStatePanels()
        {
            // Configure which Canvas UI panels should be shown for each game state
            SetStatePanels("MainMenu", "MainMenuPanel");
            SetStatePanels("Gameplay", "PlayerStatusPanel"); // ResonancePanel moved to substate
            SetStatePanels("Gameplay/Resonance", "ResonancePanel"); // ResonancePanel only in Resonance substate
            SetStatePanels("Paused", "PauseMenuPanel");
        }

        public void RegisterPanel(IUIPanel panel)
        {
            if (panel == null)
            {
                Debug.LogError("UIService: Cannot register null panel");
                return;
            }

            if (_panels.ContainsKey(panel.PanelName))
            {
                Debug.LogWarning($"UIService: Panel {panel.PanelName} already registered. Overwriting.");
            }

            _panels[panel.PanelName] = panel;
            panel.Initialize();
            Debug.Log($"UIService: Registered panel {panel.PanelName}");
        }

        public void UnregisterPanel(string panelName)
        {
            if (_panels.TryGetValue(panelName, out IUIPanel panel))
            {
                panel.Cleanup();
                _panels.Remove(panelName);
                _visiblePanels.Remove(panelName);
                Debug.Log($"UIService: Unregistered panel {panelName}");
            }
        }

        public void ShowPanel(string panelName)
        {
            if (!_panels.TryGetValue(panelName, out IUIPanel panel))
            {
                Debug.LogWarning($"UIService: Canvas UI Panel {panelName} not found. Make sure it's registered by CanvasUIManager.");
                return;
            }

            if (!panel.IsVisible)
            {
                panel.Show();
                if (!_visiblePanels.Contains(panelName))
                {
                    _visiblePanels.Add(panelName);
                }
                OnPanelShown?.Invoke(panelName);
                Debug.Log($"UIService: Showed Canvas UI panel {panelName}");
            }
        }

        public void HidePanel(string panelName)
        {
            if (!_panels.TryGetValue(panelName, out IUIPanel panel))
            {
                Debug.LogWarning($"UIService: Panel {panelName} not found");
                return;
            }

            if (panel.IsVisible)
            {
                panel.Hide();
                _visiblePanels.Remove(panelName);
                OnPanelHidden?.Invoke(panelName);
                Debug.Log($"UIService: Hide panel {panelName}");
            }
        }

        public void HideAllPanels()
        {
            Debug.Log("UIService: Hiding all panels");
            var panelsToHide = new List<string>(_visiblePanels);
            
            foreach (string panelName in panelsToHide)
            {
                HidePanel(panelName);
            }
        }

        public void ShowPanelsForState(string stateName)
        {
            Debug.Log($"UIService: Showing panels for state {stateName}");
            
            // First hide all panels
            HideAllPanels();
            
            // Then show panels for the new state
            if (_statePanels.TryGetValue(stateName, out List<string> panelNames))
            {
                foreach (string panelName in panelNames)
                {
                    ShowPanel(panelName);
                }
            }
            else
            {
                Debug.LogWarning($"UIService: No panels configured for state {stateName}");
            }
        }

        public void SetStatePanels(string stateName, params string[] panelNames)
        {
            _statePanels[stateName] = new List<string>(panelNames);
            Debug.Log($"UIService: Set panels for state {stateName}: {string.Join(", ", panelNames)}");
        }

        public T GetPanel<T>(string panelName) where T : class, IUIPanel
        {
            if (_panels.TryGetValue(panelName, out IUIPanel panel))
            {
                return panel as T;
            }
            return null;
        }

        public bool IsPanelVisible(string panelName)
        {
            return _visiblePanels.Contains(panelName);
        }

        public List<string> GetVisiblePanels()
        {
            return new List<string>(_visiblePanels);
        }

        public void NotifySceneUIPanelsReady(string sceneName)
        {
            Debug.Log($"UIService: Scene UI panels ready for scene {sceneName}");
            OnSceneUIPanelsReady?.Invoke(sceneName);
        }

        public void Shutdown()
        {
            if (State == SystemState.Shutdown)
                return;

            Debug.Log("UIService: Shutting down");

            // Cleanup all panels
            foreach (var panel in _panels.Values)
            {
                panel.Cleanup();
            }

            _panels.Clear();
            _statePanels.Clear();
            _visiblePanels.Clear();

            // Clear events
            OnPanelShown = null;
            OnPanelHidden = null;
            OnSceneUIPanelsReady = null;

            State = SystemState.Shutdown;
        }
    }
}
