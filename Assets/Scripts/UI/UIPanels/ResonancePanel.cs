using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Resonance.Core;
using Resonance.Interfaces.Services;
using Resonance.Utilities;
using Resonance.Enemies;

namespace Resonance.UI
{
    public class ResonancePanel : UIPanel
    {
        [Header("QTE UI Elements")]
        [SerializeField] private TextMeshProUGUI _qteValueText;
        [SerializeField] private Slider _qteProgressSlider;
        [SerializeField] private TextMeshProUGUI _instructionText;
        
        // QTE Logic
        private IInputService _inputService;
        private bool _isInitialized = false;
        private bool _isQTEActive = false;
        private float _qteValue = 0f;
        private float _qteSpeed = 1f;
        private float _qteTargetWindow = 0.2f; // Target window around 0
        private EnemyHitbox _targetCore;

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
            
            // Get input service
            _inputService = ServiceRegistry.Get<IInputService>();
            if (_inputService != null)
            {
                _inputService.OnQTE += OnQTEInput;
                Debug.Log("ResonancePanel: Subscribed to QTE input events");
            }
            
            // Initialize UI elements
            if (_instructionText != null)
            {
                _instructionText.text = "Press F when the value is close to 0!";
            }
            
            _isInitialized = true;
        }

        protected override void OnShow()
        {
            Debug.Log("ResonancePanel: Shown");
            
            // Start QTE sequence
            StartQTE();
        }

        protected override void OnHide()
        {
            Debug.Log("ResonancePanel: Hidden");
            
            // Stop QTE sequence
            StopQTE();
        }

        protected override void OnCleanup()
        {
            // Unsubscribe from events (Risk mitigation: Event lifecycle management)
            if (_inputService != null)
            {
                _inputService.OnQTE -= OnQTEInput;
                Debug.Log("ResonancePanel: Unsubscribed from QTE input events");
            }
            
            StopQTE();
            _isInitialized = false;
            Debug.Log("ResonancePanel: Cleaned up");
        }

        #endregion
        
        #region QTE Logic
        
        /// <summary>
        /// Set the target core for this QTE session
        /// </summary>
        /// <param name="targetCore">The enemy core being attacked</param>
        public void SetTargetCore(EnemyHitbox targetCore)
        {
            _targetCore = targetCore;
            Debug.Log($"ResonancePanel: Set target core to {targetCore?.name}");
        }
        
        /// <summary>
        /// Start the QTE sequence
        /// </summary>
        private void StartQTE()
        {
            if (!_isInitialized) return;
            
            _isQTEActive = true;
            _qteValue = 1f; // Start at maximum
            _qteSpeed = 2f; // Adjust speed as needed
            
            Debug.Log("ResonancePanel: Started QTE sequence");
        }
        
        /// <summary>
        /// Stop the QTE sequence
        /// </summary>
        private void StopQTE()
        {
            _isQTEActive = false;
            Debug.Log("ResonancePanel: Stopped QTE sequence");
        }
        
        /// <summary>
        /// Update QTE value each frame
        /// </summary>
        private void Update()
        {
            if (!_isQTEActive) return;
            
            // Update QTE value using a sine wave function for smooth oscillation
            _qteValue = Mathf.Sin(Time.time * _qteSpeed);
            
            // Update UI elements
            UpdateQTEUI();
        }
        
        /// <summary>
        /// Update QTE UI elements
        /// </summary>
        private void UpdateQTEUI()
        {
            if (_qteValueText != null)
            {
                _qteValueText.text = _qteValue.ToString("F2");
                
                // Change color based on proximity to target
                float proximityToZero = Mathf.Abs(_qteValue);
                if (proximityToZero <= _qteTargetWindow)
                {
                    _qteValueText.color = Color.green; // Good timing
                }
                else if (proximityToZero <= _qteTargetWindow * 2f)
                {
                    _qteValueText.color = Color.yellow; // Okay timing
                }
                else
                {
                    _qteValueText.color = Color.red; // Poor timing
                }
            }
            
            if (_qteProgressSlider != null)
            {
                // Map -1 to 1 range to 0 to 1 for slider
                _qteProgressSlider.value = (_qteValue + 1f) / 2f;
            }
        }
        
        /// <summary>
        /// Handle QTE input from player
        /// </summary>
        private void OnQTEInput()
        {
            if (!_isQTEActive) return;
            
            float proximityToZero = Mathf.Abs(_qteValue);
            bool isSuccess = proximityToZero <= _qteTargetWindow;
            
            Debug.Log($"ResonancePanel: QTE input received. Value: {_qteValue:F2}, Success: {isSuccess}");
            
            if (isSuccess)
            {
                HandleQTESuccess();
            }
            else
            {
                HandleQTEFailure();
            }
        }
        
        /// <summary>
        /// Handle successful QTE input
        /// </summary>
        private void HandleQTESuccess()
        {
            Debug.Log("ResonancePanel: QTE Success!");
            
            // Apply mental damage to target core
            if (_targetCore != null)
            {
                // TODO: Apply mental damage to the target enemy
                // This will require accessing the enemy's health system
                Debug.Log($"ResonancePanel: Applying mental damage to {_targetCore.name}");
                
                // For now, just log the success
                // In the future, this should:
                // 1. Calculate damage based on timing accuracy
                // 2. Apply damage to enemy mental health
                // 3. Trigger appropriate visual/audio effects
            }
            
            // Provide visual feedback
            if (_instructionText != null)
            {
                _instructionText.text = "SUCCESS! Mental damage applied!";
                _instructionText.color = Color.green;
            }
            
            // QTE sequence completes on success
            StopQTE();
        }
        
        /// <summary>
        /// Handle failed QTE input
        /// </summary>
        private void HandleQTEFailure()
        {
            Debug.Log("ResonancePanel: QTE Failed!");
            
            // Provide visual feedback
            if (_instructionText != null)
            {
                _instructionText.text = "MISSED! Try again...";
                _instructionText.color = Color.red;
            }
            
            // Continue QTE sequence on failure (player can try again)
        }
        
        #endregion
    }
}