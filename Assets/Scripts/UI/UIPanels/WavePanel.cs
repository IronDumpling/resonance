using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Resonance.Core;
using Resonance.Interfaces;
using Resonance.Interfaces.Services;
using Resonance.Utilities.Waves;
using Resonance.Utilities.CrystalCore;
using Resonance.Utilities;
using Resonance.Enemies.Triggers;
using DG.Tweening;

namespace Resonance.UI
{
    public class WavePanel : UIPanel
    {
        [Header("QTE UI Elements")]
        [SerializeField] private TextMeshProUGUI _qteValueText;
        [SerializeField] private TextMeshProUGUI _instructionText;
        
        // Player damage configuration
        [Header("Player Damage Configuration")]
        [SerializeField] private float _baseCoreDamage = 50f;
        [SerializeField] private float _maxDamageMultiplier = 3f;
        [SerializeField] private float _damageScaleFactor = 10f;

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
        
        // Input filtering
        private float _panelOpenTime = 0f;
        private const float QTE_INPUT_DELAY = 0.2f; // Delay before accepting QTE input after panel opens

        protected override void Awake()
        {
            base.Awake();
            _panelName = "WavePanel";
            _layer = UILayer.Game;
            _hideOnStart = true;
        }
        
        #region UIPanel Overrides

        protected override void OnInitialize()
        {
            Debug.Log("WavePanel: OnInitialize called");
            
            // Auto-find UI elements if not assigned in Inspector
            ValidateUIElements();
            
            // Get input service
            _inputService = ServiceRegistry.Get<IInputService>();
            if (_inputService != null)
            {
                _inputService.OnQTE += OnQTEInput;
                Debug.Log("WavePanel: Subscribed to QTE input events");
            }
            
            _isInitialized = true;
        }

        protected override void OnShow()
        {
            Debug.Log("WavePanel: Shown");
            
            // Record panel open time to prevent immediate input
            _panelOpenTime = Time.time;
            Debug.Log($"WavePanel: Panel open time recorded: {_panelOpenTime}");
            
            // Don't start QTE immediately - wait for SetTargetCore to be called
        }

        protected override void OnHide()
        {
            Debug.Log("WavePanel: Hidden");
            
            // Stop QTE sequence
            StopQTE();
        }

        protected override void OnCleanup()
        {
            // Unsubscribe from events
            if (_inputService != null)
            {
                _inputService.OnQTE -= OnQTEInput;
                Debug.Log("WavePanel: Unsubscribed from QTE input events");
            }
            
            // Stop QTE and clean up DoTween
            StopQTE();
            
            // Kill any remaining tweens
            _qteTween?.Kill();
            _qteTween = null;
            
            _isInitialized = false;
            Debug.Log("WavePanel: Cleaned up");
        }

        #endregion
        
        #region UI Element Validation
        
        /// <summary>
        /// Validate and auto-find UI elements if not assigned in Inspector
        /// Follows Unity hierarchy: WavePanel -> Panel -> Text (TMPro)
        /// </summary>
        private void ValidateUIElements()
        {
            // Auto-find QTE Value Text if not assigned
            if (_qteValueText == null)
            {
                // Try to find: WavePanel/Panel/Text
                Transform panelChild = transform.Find("Panel");
                if (panelChild != null)
                {
                    Transform textChild = panelChild.Find("QTEText");
                    if (textChild != null)
                    {
                        _qteValueText = textChild.GetComponent<TextMeshProUGUI>();
                        if (_qteValueText != null)
                        {
                            Debug.Log("WavePanel: Auto-found QTE Value Text at Panel/Text");
                        }
                        else
                        {
                            Debug.LogWarning("WavePanel: Found Text GameObject but no TextMeshProUGUI component");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("WavePanel: Could not find Text child under Panel");
                    }
                }
                else
                {
                    Debug.LogWarning("WavePanel: Could not find Panel child");
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
                            Debug.Log("WavePanel: Auto-found Instruction Text");
                        }
                    }
                }
            }
            
            // Validate that essential elements are found
            TestQTETextDisplay();
        }
        
        /// <summary>
        /// Test the QTE text display to ensure it works correctly
        /// </summary>
        private void TestQTETextDisplay()
        {
            
            if (_qteValueText == null)
            {
                Debug.LogError("WavePanel: QTE Value Text (TextMeshProUGUI) is not assigned and could not be auto-found. " +
                              "Please assign it in Inspector or ensure hierarchy: WavePanel/Panel/Text");
            }
            else if (_instructionText == null)
            {
                Debug.LogError("WavePanel: Instruction Text (TextMeshProUGUI) is not assigned and could not be auto-found. " +
                              "Please assign it in Inspector or ensure hierarchy: WavePanel/Panel/InstructionText");
            }
            else
            {
                // Test initial display
                _qteValueText.text = "0.00";
                _qteValueText.color = Color.white;

                _instructionText.text = "Press F when the value is close to 0!";
                _instructionText.color = Color.white;

                Debug.Log($"WavePanel: QTE Text component validated - " +
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
                Debug.Log($"WavePanel: Set target core to {targetCore.name} with QTE config - " +
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
                Debug.LogWarning($"WavePanel: Target core invalid for QTE, using default configuration");
            }
            
            // Now that we have the configuration, start the QTE sequence
            StartQTE();
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
            // Kill any existing tween
            _qteTween?.Kill();
            
            // Create a looping tween that oscillates between 1 and -1
            _qteValue = 1f; // Start at 1
            
            // Force initial UI update before starting tween
            UpdateQTEUI();
            
            _qteTween = DOTween.To(() => _qteValue, x => _qteValue = x, -1f, _qteConfig.cycleDuration / 2f)
                .SetEase(_qteConfig.easeType)
                .SetLoops(-1, LoopType.Yoyo)
                .OnUpdate(() => UpdateQTEUI())
                .OnStart(() => {
                    Debug.Log("WavePanel: DoTween animation started");
                    UpdateQTEUI(); // Ensure UI is updated when tween starts
                });
            
            Debug.Log($"WavePanel: Started QTE sequence with {_qteConfig.easeType} ease, {_qteConfig.cycleDuration}s cycle");
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
            
            Debug.Log("WavePanel: Stopped QTE sequence");
        }
        
        /// <summary>
        /// Update method - DoTween handles the animation, we just need to check for timeouts
        /// </summary>
        private void Update()
        {
            if (!_isQTEActive) return;
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
                    Debug.LogWarning("WavePanel: QTE Value Text GameObject is not active");
                }
            }
            else
            {
                Debug.LogWarning("WavePanel: QTE Value Text (TextMeshProUGUI) is null - cannot update QTE display");
            }
        }
        
        /// <summary>
        /// Handle QTE input from player
        /// </summary>
        private void OnQTEInput()
        {
            Debug.Log($"WavePanel: OnQTEInput called - _isQTEActive: {_isQTEActive}, Current time: {Time.time}, Panel open time: {_panelOpenTime}");
            
            if (!_isQTEActive) 
            {
                Debug.Log("WavePanel: QTE input ignored - QTE not active");
                return;
            }
            
            // Check if enough time has passed since panel opened to accept input
            float timeSinceOpen = Time.time - _panelOpenTime;
            Debug.Log($"WavePanel: Time since panel open: {timeSinceOpen:F3}s, Required delay: {QTE_INPUT_DELAY}s");
            
            if (timeSinceOpen < QTE_INPUT_DELAY)
            {
                Debug.Log($"WavePanel: QTE input ignored - too soon after panel open ({timeSinceOpen:F3}s < {QTE_INPUT_DELAY}s)");
                return;
            }
            
            float proximityToZero = Mathf.Abs(_qteValue);
            float targetWindow = _qteConfig?.targetWindow ?? 0.2f;
            bool isSuccess = proximityToZero <= targetWindow;
            
            Debug.Log($"WavePanel: QTE input accepted. Value: {_qteValue:F2}, Target Window: {targetWindow:F2}, Success: {isSuccess}");
            
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
            float finalDamage = _baseCoreDamage * damageMultiplier;
            
            Debug.Log($"WavePanel: QTE Success! Accuracy: {accuracy:F3}, Multiplier: {damageMultiplier:F2}, Damage: {finalDamage:F1}");
            
            // Apply core damage to target enemy
            bool damageApplied = ApplyCoreDamageToEnemy(finalDamage);
            
            if (damageApplied)
            {
                // Provide visual feedback with damage info
                ShowSuccessFeedback(finalDamage, accuracy);
                
                // Play success effects
                PlaySuccessEffects();
            }
            else
            {
                Debug.LogWarning("WavePanel: Failed to apply core damage to enemy");
                ShowFailureFeedback("Failed to apply damage!");
            }
            
            // Continue QTE sequence instead of stopping - player can perform multiple QTEs
            // The QTE will only end when the WaveAttackAction itself ends (enemy state change, etc.)
            Debug.Log("WavePanel: QTE success processed, continuing sequence for more attempts");
        }
        
        /// <summary>
        /// Handle failed QTE input
        /// </summary>
        private void HandleQTEFailure()
        {
            float accuracy = Mathf.Abs(_qteValue);
            float targetWindow = _qteConfig?.targetWindow ?? 0.2f;
            
            Debug.Log($"WavePanel: QTE Failed! Accuracy: {accuracy:F3}, Required: {targetWindow:F3}");
            
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
        /// Apply core damage to the target enemy
        /// </summary>
        /// <param name="damage">Amount of core damage to apply</param>
        /// <returns>True if damage was successfully applied</returns>
        private bool ApplyCoreDamageToEnemy(float damage)
        {
            var enemyMono = _targetCore.GetEnemyMonoBehaviour();
            if (enemyMono == null)
            {
                Debug.LogError("WavePanel: Cannot apply damage - enemy MonoBehaviour is null");
                return false;
            }
            
            // Get player position for damage source
            var playerService = ServiceRegistry.Get<IPlayerService>();
            Vector3 playerPosition = playerService?.CurrentPlayer?.transform.position ?? Vector3.zero;
            GameObject playerObject = playerService?.CurrentPlayer?.gameObject;

            Damages damages = new Damages();
            damages.SetDamage(DamageType.CoreHealth, damage);
            
            // Create damage information
            DamageInfo damageInfo = new DamageInfo(
                damages: damages,
                sourcePosition: playerPosition,
                sourceObject: playerObject,
                description: "Wave QTE Core Damage"
            );
            
            // Apply damage through the enemy's damage system
            enemyMono.TakeDamage(damageInfo);
            
            Debug.Log($"WavePanel: Applied {damage:F1} core damage to {enemyMono.name}");
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
                _instructionText.text = $"SUCCESS! {damage:F0} Core Damage ({accuracyGrade})";
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
                audioService.PlaySFX2D(AudioClipType.EnemyHit, 0.8f, 1.2f); 
            }
            
            // TODO: Add visual effects (screen flash, particles, etc.)
            Debug.Log("WavePanel: Playing success effects");
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
                audioService.PlaySFX2D(AudioClipType.EnemyHit, 0.4f, 0.6f); 
            }
            
            // TODO: Add visual effects (screen shake, red flash, etc.)
            Debug.Log("WavePanel: Playing failure effects");
        }
        
        #endregion
    }
}