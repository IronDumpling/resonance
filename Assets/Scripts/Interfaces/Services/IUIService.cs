using System;
using System.Collections.Generic;
using UnityEngine;

namespace Resonance.Core
{
    public enum UILayer
    {
        Background = 0,
        Game = 10,
        Menu = 20,
        Popup = 30,
        Overlay = 40,
        Debug = 50
    }

    public interface IUIPanel
    {
        string PanelName { get; }
        UILayer Layer { get; }
        bool IsVisible { get; }
        void Show();
        void Hide();
        void Initialize();
        void Cleanup();
    }

    public interface IUIService : IGameService
    {
        event Action<string> OnPanelShown;
        event Action<string> OnPanelHidden;

        void RegisterPanel(IUIPanel panel);
        void UnregisterPanel(string panelName);
        void ShowPanel(string panelName);
        void HidePanel(string panelName);
        void HideAllPanels();
        void ShowPanelsForState(string stateName);
        void SetStatePanels(string stateName, params string[] panelNames);
        
        T GetPanel<T>(string panelName) where T : class, IUIPanel;
        bool IsPanelVisible(string panelName);
        List<string> GetVisiblePanels();
    }
}
