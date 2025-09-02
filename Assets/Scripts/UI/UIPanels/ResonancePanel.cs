using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Resonance.Core;
using Resonance.Interfaces.Services;

namespace Resonance.UI
{
    public class ResonancePanel : UIPanel
    {

        private bool _isInitialized = false;

        protected override void Awake()
        {
            base.Awake();
            _panelName = "ResonancePanel";
            _layer = UILayer.Game;
            _hideOnStart = true;
        }
        
        #region UIPanel Overrides

        protected override void OnInitialize()
        {
            Debug.Log("ResonancePanel: OnInitialize called");
        }

        protected override void OnShow()
        {
            Debug.Log("ResonancePanel: Shown");
        }

        protected override void OnHide()
        {
            Debug.Log("ResonancePanel: Hidden");
        }

        protected override void OnCleanup()
        {
            // UnsubscribeFromPlayerEvents();
            _isInitialized = false;
            Debug.Log("ResonancePanel: Cleaned up");
        }

        #endregion
    }
}