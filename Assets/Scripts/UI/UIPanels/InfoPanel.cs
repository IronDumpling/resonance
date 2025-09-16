using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Resonance.Core;
using Resonance.Interfaces.Services;
using Resonance.Items;
using Resonance.Utilities;

namespace Resonance.UI
{
    public class InfoPanel : UIPanel
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _infoName;
        [SerializeField] private TextMeshProUGUI _infoContent;

        protected override void Awake()
        {
            base.Awake();

            _panelName = "InfoPanel";
            _layer = UILayer.Game;
            _hideOnStart = true;
        }

        protected override void Start()
        {
            base.Start();

            AutoDiscoverUIComponents();
        }
        
        private void AutoDiscoverUIComponents()
        {
            if (_infoName == null)
                _infoName = transform.Find("InfoName").GetComponent<TextMeshProUGUI>();
            if (_infoContent == null)
                _infoContent = transform.Find("InfoContent").GetComponent<TextMeshProUGUI>();
        }
    }
}