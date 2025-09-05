using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Resonance.Core;
using Resonance.Interfaces;
using Resonance.Interfaces.Services;
using Resonance.Utilities;
using Resonance.Enemies;
using DG.Tweening;

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
        private EnemyHitbox _targetCore;
        
        // Enemy-specific QTE Configuration
        private QTEConfig _qteConfig;
        private Tween _qteTween;
        private float _qteStartTime;
        
        // Player damage configuration
        [Header("Player Damage Configuration")]
        [SerializeField] private float _baseMentalDamage = 50f;
        [SerializeField] private float _maxDamageMultiplier = 3f;
        [SerializeField] private float _damageScaleFactor = 10f;

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
            
            // Auto-find UI elements if not assigned in Inspector
            ValidateUIElements();
            
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
            
            // Stop QTE and clean up DoTween
            StopQTE();
            
            // Kill any remaining tweens
            _qteTween?.Kill();
            _qteTween = null;
            
            _isInitialized = false;
            Debug.Log("ResonancePanel: Cleaned up");
        }

        #endregion
        
        #region UI Element Validation
        
        /// <summary>
        /// Validate and auto-find UI elements if not assigned in Inspector
        /// Follows Unity hierarchy: ResonancePanel -> Panel -> Text (TMPro)
        /// </summary>
        private void ValidateUIElements()
        {
            // Auto-find QTE Value Text if not assigned
            if (_qteValueText == null)
            {
                // Try to find: ResonancePanel/Panel/Text
                Transform panelChild = transform.Find("Panel");
                if (panelChild != null)
                {
                    Transform textChild = panelChild.Find("Text");
                    if (textChild != null)
                    {
                        _qteValueText = textChild.GetComponent<TextMeshProUGUI>();
                        if (_qteValueText != null)
                        {
                            Debug.Log("ResonancePanel: Auto-found QTE Value Text at Panel/Text");
                        }
                        else
                        {
                            Debug.LogWarning("ResonancePanel: Found Text GameObject but no TextMeshProUGUI component");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("ResonancePanel: Could not find Text child under Panel");
                    }
                }
                else
                {
                    Debug.LogWarning("ResonancePanel: Could not find Panel child");
                }
            }
            
            // Auto-find Progress Slider if not assigned
            if (_qteProgressSlider == null)
            {
                Transform panelChild = transform.Find("Panel");
                if (panelChild != null)
                {
                    _qteProgressSlider = panelChild.GetComponentInChildren<Slider>();
                    if (_qteProgressSlider != null)
                    {
                        Debug.Log("ResonancePanel: Auto-found QTE Progress Slider");
                    }
                }
            }
            
            // Auto-find Instruction Text if not assigned
            if (_instructionText == null)
            {
                Transform panelChild = transform.Find("Panel");
                if (panelChild != null)
                {
                    // Look for a child named "InstructionText" or any other TextMeshProUGUI
                    Transform instructionChild = panelChild.Find("InstructionText");
                    if (instructionChild != null)
                    {
                        _instructionText = instructionChild.GetComponent<TextMeshProUGUI>();
                        if (_instructionText != null)
                        {
                            Debug.Log("ResonancePanel: Auto-found Instruction Text");
                        }
                    }
                }
            }
            
            // Validate that essential elements are found
            if (_qteValueText == null)
            {
                Debug.LogError("ResonancePanel: QTE Value Text (TextMeshProUGUI) is not assigned and could not be auto-found. " +
                              "Please assign it in Inspector or ensure hierarchy: ResonancePanel/Panel/Text");
            }
            else
            {
                // Test the TMPro text component
                TestQTETextDisplay();
            }
        }
        
        /// <summary>
        /// Test the QTE text display to ensure it works correctly
        /// </summary>
        private void TestQTETextDisplay()
        {
            if (_qteValueText != null)
            {
                // Test initial display
                _qteValueText.text = "0.00";
                _qteValueText.color = Color.white;
                
                Debug.Log($"ResonancePanel: QTE Text component validated - " +
                         $"GameObject: {_qteValueText.gameObject.name}, " +
                         $"Active: {_qteValueText.gameObject.activeInHierarchy}, " +
                         $"Enabled: {_qteValueText.enabled}, " +
                         $"Font: {(_qteValueText.font != null ? _qteValueText.font.name : "null")}");
            }
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
            
            // Get QTE configuration from the target core
            if (_targetCore != null && _targetCore.IsValidForQTE())
            {
                _qteConfig = _targetCore.GetQTEConfig();
                Debug.Log($"ResonancePanel: Set target core to {targetCore.name} with QTE config - " +
                         $"Ease: {_qteConfig?.easeType}, Duration: {_qteConfig?.cycleDuration}, Window: {_qteConfig?.targetWindow}");
            }
            else
            {
                // Use default configuration as fallback
                _qteConfig = new QTEConfig
                {
                    easeType = Ease.InOutSine,
                    cycleDuration = 3f,
                    targetWindow = 0.2f
                };
                Debug.LogWarning($"ResonancePanel: Target core invalid for QTE, using default configuration");
            }
        }
        
        /// <summary>
        /// Start the QTE sequence
        /// </summary>
        private void StartQTE()
        {
            if (!_isInitialized || _qteConfig == null) return;
            
            _isQTEActive = true;
            _qteStartTime = Time.time;
            
            // Start DoTween animation using enemy-specific configuration
            StartQTEAnimation();
            
            Debug.Log($"ResonancePanel: Started QTE sequence with {_qteConfig.easeType} ease, {_qteConfig.cycleDuration}s cycle");
        }
        
        /// <summary>
        /// Start DoTween-based QTE animation
        /// </summary>
        private void StartQTEAnimation()
        {
            // Kill any existing tween
            _qteTween?.Kill();
            
            // Create a looping tween that oscillates between 1 and -1
            _qteValue = 1f; // Start at 1
            
            _qteTween = DOTween.To(() => _qteValue, x => _qteValue = x, -1f, _qteConfig.cycleDuration / 2f)
                .SetEase(_qteConfig.easeType)
                .SetLoops(-1, LoopType.Yoyo)
                .OnUpdate(() => UpdateQTEUI());
        }
        
        /// <summary>
        /// Stop the QTE sequence
        /// </summary>
        private void StopQTE()
        {
            _isQTEActive = false;
            
            // Kill the DoTween animation
            _qteTween?.Kill();
            _qteTween = null;
            
            Debug.Log("ResonancePanel: Stopped QTE sequence");
        }
        
        /// <summary>
        /// Update method - DoTween handles the animation, we just need to check for timeouts
        /// </summary>
        private void Update()
        {
            if (!_isQTEActive) return;
            
            // DoTween handles the animation via OnUpdate callback
            // We could add timeout logic here if needed
        }
        
        /// <summary>
        /// Update QTE UI elements
        /// </summary>
        private void UpdateQTEUI()
        {
            // Update TMPro text with QTE value
            if (_qteValueText != null)
            {
                // Format the value to 2 decimal places for display
                string formattedValue = _qteValue.ToString("F2");
                _qteValueText.text = formattedValue;
                
                // Change color based on proximity to target using enemy-specific window
                float proximityToZero = Mathf.Abs(_qteValue);
                float targetWindow = _qteConfig?.targetWindow ?? 0.2f;
                
                if (proximityToZero <= targetWindow)
                {
                    _qteValueText.color = Color.green; // Good timing
                }
                else if (proximityToZero <= targetWindow * 2f)
                {
                    _qteValueText.color = Color.yellow; // Okay timing
                }
                else
                {
                    _qteValueText.color = Color.red; // Poor timing
                }
                
                // Ensure the text component is enabled and visible
                if (!_qteValueText.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning("ResonancePanel: QTE Value Text GameObject is not active");
                }
            }
            else
            {
                Debug.LogWarning("ResonancePanel: QTE Value Text (TextMeshProUGUI) is null - cannot update QTE display");
            }
            
            // Update progress slider if available
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
            float targetWindow = _qteConfig?.targetWindow ?? 0.2f;
            bool isSuccess = proximityToZero <= targetWindow;
            
            Debug.Log($"ResonancePanel: QTE input received. Value: {_qteValue:F2}, Target Window: {targetWindow:F2}, Success: {isSuccess}");
            
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
            if (_targetCore == null) return;
            
            // Calculate damage based on timing accuracy
            float accuracy = Mathf.Abs(_qteValue); // Distance from 0
            float damageMultiplier = CalculateDamageMultiplier(accuracy);
            float finalDamage = _baseMentalDamage * damageMultiplier;
            
            Debug.Log($"ResonancePanel: QTE Success! Accuracy: {accuracy:F3}, Multiplier: {damageMultiplier:F2}, Damage: {finalDamage:F1}");
            
            // Apply mental damage to target enemy
            bool damageApplied = ApplyMentalDamageToEnemy(finalDamage);
            
            if (damageApplied)
            {
                // Provide visual feedback with damage info
                ShowSuccessFeedback(finalDamage, accuracy);
                
                // Play success effects
                PlaySuccessEffects();
            }
            else
            {
                Debug.LogWarning("ResonancePanel: Failed to apply mental damage to enemy");
                ShowFailureFeedback("Failed to apply damage!");
            }
            
            // QTE sequence completes on success
            StopQTE();
        }
        
        /// <summary>
        /// Handle failed QTE input
        /// </summary>
        private void HandleQTEFailure()
        {
            float accuracy = Mathf.Abs(_qteValue);
            float targetWindow = _qteConfig?.targetWindow ?? 0.2f;
            
            Debug.Log($"ResonancePanel: QTE Failed! Accuracy: {accuracy:F3}, Required: {targetWindow:F3}");
            
            // Show failure feedback with accuracy info
            ShowFailureFeedback($"MISSED! (Off by {accuracy:F2}) Try again...");
            
            // Play failure effects
            PlayFailureEffects();
            
            // Continue QTE sequence on failure (player can try again)
        }
        
        #endregion
        
        #region Damage System
        
        /// <summary>
        /// Calculate damage multiplier based on QTE accuracy
        /// Uses inverse relationship: closer to 0 = higher damage
        /// </summary>
        /// <param name="accuracy">Distance from 0 (0 = perfect, higher = worse)</param>
        /// <returns>Damage multiplier (1.0 to maxDamageMultiplier)</returns>
        private float CalculateDamageMultiplier(float accuracy)
        {
            // Use inverse function: multiplier = maxMultiplier / (1 + accuracy * scaleFactor)
            // This creates a curve where perfect accuracy (0) gives max damage,
            // and accuracy decreases damage exponentially
            float multiplier = _maxDamageMultiplier / (1f + accuracy * _damageScaleFactor);
            
            // Ensure minimum multiplier of 1.0 for any successful QTE
            return Mathf.Max(1f, multiplier);
        }
        
        /// <summary>
        /// Apply mental damage to the target enemy
        /// </summary>
        /// <param name="damage">Amount of mental damage to apply</param>
        /// <returns>True if damage was successfully applied</returns>
        private bool ApplyMentalDamageToEnemy(float damage)
        {
            var enemyMono = _targetCore.GetEnemyMonoBehaviour();
            if (enemyMono == null)
            {
                Debug.LogError("ResonancePanel: Cannot apply damage - enemy MonoBehaviour is null");
                return false;
            }
            
            // Get player position for damage source
            var playerService = ServiceRegistry.Get<IPlayerService>();
            Vector3 playerPosition = playerService?.CurrentPlayer?.transform.position ?? Vector3.zero;
            GameObject playerObject = playerService?.CurrentPlayer?.gameObject;
            
            // Create damage information
            DamageInfo damageInfo = new DamageInfo(
                amount: damage,
                type: DamageType.Mental,
                sourcePosition: playerPosition,
                sourceObject: playerObject,
                description: "Resonance QTE Mental Damage"
            );
            
            // Apply damage through the enemy's damage system
            enemyMono.TakeDamage(damageInfo);
            
            Debug.Log($"ResonancePanel: Applied {damage:F1} mental damage to {enemyMono.name}");
            return true;
        }
        
        /// <summary>
        /// Show success feedback with damage information
        /// </summary>
        /// <param name="damage">Amount of damage dealt</param>
        /// <param name="accuracy">QTE accuracy value</param>
        private void ShowSuccessFeedback(float damage, float accuracy)
        {
            if (_instructionText != null)
            {
                string accuracyGrade = GetAccuracyGrade(accuracy);
                _instructionText.text = $"SUCCESS! {damage:F0} Mental Damage ({accuracyGrade})";
                _instructionText.color = Color.green;
            }
        }
        
        /// <summary>
        /// Show failure feedback
        /// </summary>
        /// <param name="message">Failure message to display</param>
        private void ShowFailureFeedback(string message)
        {
            if (_instructionText != null)
            {
                _instructionText.text = message;
                _instructionText.color = Color.red;
            }
        }
        
        /// <summary>
        /// Get accuracy grade based on QTE performance
        /// </summary>
        /// <param name="accuracy">Distance from perfect (0)</param>
        /// <returns>Grade string (Perfect, Excellent, Good, etc.)</returns>
        private string GetAccuracyGrade(float accuracy)
        {
            float targetWindow = _qteConfig?.targetWindow ?? 0.2f;
            
            if (accuracy <= targetWindow * 0.25f)
                return "PERFECT";
            else if (accuracy <= targetWindow * 0.5f)
                return "EXCELLENT";
            else if (accuracy <= targetWindow * 0.75f)
                return "GOOD";
            else
                return "OK";
        }
        
        /// <summary>
        /// Play success effects (audio/visual)
        /// </summary>
        private void PlaySuccessEffects()
        {
            // Play success audio
            var audioService = ServiceRegistry.Get<IAudioService>();
            if (audioService != null)
            {
                // TODO: Add specific resonance success audio clip
                audioService.PlaySFX2D(AudioClipType.PlayerHit, 0.8f, 1.2f); // Placeholder
            }
            
            // TODO: Add visual effects (screen flash, particles, etc.)
            Debug.Log("ResonancePanel: Playing success effects");
        }
        
        /// <summary>
        /// Play failure effects (audio/visual)
        /// </summary>
        private void PlayFailureEffects()
        {
            // Play failure audio
            var audioService = ServiceRegistry.Get<IAudioService>();
            if (audioService != null)
            {
                // TODO: Add specific resonance failure audio clip
                audioService.PlaySFX2D(AudioClipType.PlayerHit, 0.4f, 0.6f); // Placeholder - lower pitch
            }
            
            // TODO: Add visual effects (screen shake, red flash, etc.)
            Debug.Log("ResonancePanel: Playing failure effects");
        }
        
        #endregion
    }
}