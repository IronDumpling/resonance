using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Resonance.Core;
using Resonance.Interfaces;
using Resonance.Interfaces.Services;
using Resonance.Utilities;
using Resonance.Utilities.Waves;
using Resonance.Utilities.CrystalCore;
using Resonance.Utilities.Types;
using Resonance.Enemies.Triggers;

namespace Resonance.UI
{
    public class WavePanel : UIPanel
    {
        [Header("Wave UI Elements")]
        [SerializeField] private RectTransform _waveScopeArea;
        [SerializeField] private LineRenderer _targetWaveLine;
        [SerializeField] private LineRenderer _sourceWaveLine;

        [Header("Wave Visual Configuration")]
        [SerializeField] private float _waveScrollSpeed = 1f;
        [SerializeField] private float _waveStopScrollDuration = 1f;
        [SerializeField] private int _waveLinePointCount = 128; // Number of points to sample from wave
        [SerializeField] private float _waveDisplayWidth = 10f; // Display width in world units
        [SerializeField] private float _waveDisplayHeight = 2f; // Display height multiplier

        [SerializeField] private TextMeshProUGUI _instructionText;
        
        // Wave attack damage configuration
        [Header("Wave Damage Configuration")]
        [SerializeField] private float _baseCoreDamage = 10f;
        [SerializeField] private float _perfectMatchMultiplier = 3f;  // Perfect: >90%
        [SerializeField] private float _goodMatchMultiplier = 1f;     // Good: >75%
        [SerializeField] private float _missMatchMultiplier = 0f;     // Miss: <75%

        // Wave system references
        private IInputService _inputService;
        private bool _isInitialized = false;
        private bool _isWaveActive = false;
        
        private IWavable _sourceWavable;  // The attacker
        private IWavable _targetWavable;  // The target being attacked
        private Wave _sourceWave;
        private Wave _targetWave;
        
        private EnemyHitbox _targetCore;  // Reference to target core for damage application
        
        // Wave scrolling state
        private float _scrollOffset = 0f;
        private bool _isScrolling = true;
        private Coroutine _scrollStopCoroutine;
        
        // Input filtering
        private float _panelOpenTime = 0f;

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
            
            // Initialize LineRenderers
            InitializeLineRenderers();
            
            _isInitialized = true;
        }

        protected override void OnShow()
        {
            Debug.Log("WavePanel: Shown");
            
            // Record panel open time to prevent immediate input
            _panelOpenTime = Time.time;
            Debug.Log($"WavePanel: Panel open time recorded: {_panelOpenTime}");
            
            // Reset scrolling state
            _scrollOffset = 0f;
            _isScrolling = true;
            
            // Don't start wave display immediately - wait for SetWaveAttackContext to be called
        }

        protected override void OnHide()
        {
            Debug.Log("WavePanel: Hidden");
            
            // Stop wave display
            StopWaveDisplay();
        }

        protected override void OnCleanup()
        {
            // Unsubscribe from events
            if (_inputService != null)
            {
                _inputService.OnQTE -= OnQTEInput;
                Debug.Log("WavePanel: Unsubscribed from QTE input events");
            }
            
            // Stop wave display
            StopWaveDisplay();
            
            // Stop any running coroutines
            if (_scrollStopCoroutine != null)
            {
                StopCoroutine(_scrollStopCoroutine);
                _scrollStopCoroutine = null;
            }
            
            _isInitialized = false;
            Debug.Log("WavePanel: Cleaned up");
        }

        #endregion
        
        #region UI Element Validation
        
        /// <summary>
        /// Validate and auto-find UI elements if not assigned in Inspector
        /// </summary>
        private void ValidateUIElements()
        {
            // Auto-find Instruction Text if not assigned
            if (_instructionText == null)
            {
                Transform panelChild = transform.Find("Panel");
                if (panelChild != null)
                {
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
            if (_instructionText == null)
            {
                Debug.LogError("WavePanel: Instruction Text (TextMeshProUGUI) is not assigned and could not be auto-found. " +
                              "Please assign it in Inspector or ensure hierarchy: WavePanel/Panel/InstructionText");
            }
            else
            {
                // Test initial display
                _instructionText.text = "Align the waves and press F!";
                _instructionText.color = Color.white;
                Debug.Log("WavePanel: Instruction Text validated");
            }
            
            // Validate LineRenderers
            if (_sourceWaveLine == null)
            {
                Debug.LogError("WavePanel: Source Wave LineRenderer is not assigned!");
            }
            
            if (_targetWaveLine == null)
            {
                Debug.LogError("WavePanel: Target Wave LineRenderer is not assigned!");
            }
        }
        
        #endregion
        
        #region Wave System Logic
        
        /// <summary>
        /// Set the wave attack context (source and target IWavables)
        /// </summary>
        /// <param name="sourceWavable">The attacker (player or enemy)</param>
        /// <param name="targetWavable">The target being attacked</param>
        /// <param name="targetCore">The target core hitbox for damage application</param>
        public void SetWaveAttackContext(IWavable sourceWavable, IWavable targetWavable, EnemyHitbox targetCore)
        {
            if (sourceWavable == null || targetWavable == null)
            {
                Debug.LogError("WavePanel: Cannot set wave attack context with null wavables");
                return;
            }
            
            _sourceWavable = sourceWavable;
            _targetWavable = targetWavable;
            _targetCore = targetCore;
            
            _sourceWave = sourceWavable.GetWave();
            _targetWave = targetWavable.GetWave();
            
            if (_sourceWave == null || _targetWave == null)
            {
                Debug.LogError("WavePanel: Cannot get waves from wavables");
                return;
            }
            
            Debug.Log($"WavePanel: Set wave attack context - Source wave: {_sourceWave.WaveformType}, Target wave: {_targetWave.WaveformType}");
            
            // Start wave display
            StartWaveDisplay();
        }
        
        /// <summary>
        /// Start the wave display
        /// </summary>
        private void StartWaveDisplay()
        {
            if (!_isInitialized || _sourceWave == null || _targetWave == null) return;
            
            _isWaveActive = true;
            _scrollOffset = 0f;
            _isScrolling = true;
            
            // Update wave lines immediately
            UpdateWaveLines();
            
            Debug.Log("WavePanel: Started wave display");
        }
        
        /// <summary>
        /// Stop the wave display
        /// </summary>
        private void StopWaveDisplay()
        {
            _isWaveActive = false;
            _isScrolling = false;
            
            // Stop scroll coroutine if running
            if (_scrollStopCoroutine != null)
            {
                StopCoroutine(_scrollStopCoroutine);
                _scrollStopCoroutine = null;
            }
            
            // Clear wave line renderers
            if (_sourceWaveLine != null)
            {
                _sourceWaveLine.positionCount = 0;
            }
            
            if (_targetWaveLine != null)
            {
                _targetWaveLine.positionCount = 0;
            }
            
            Debug.Log("WavePanel: Stopped wave display");
        }
        
        /// <summary>
        /// Update method - updates wave scrolling and rendering
        /// </summary>
        private void Update()
        {
            if (!_isWaveActive || !_isInitialized) return;
            
            // Update scroll offset if scrolling
            if (_isScrolling)
            {
                _scrollOffset += _waveScrollSpeed * Time.deltaTime;
                // Wrap around when offset exceeds 1 (one full wavelength)
                if (_scrollOffset >= 1f)
                {
                    _scrollOffset -= 1f;
                }
            }
            
            // Update wave line rendering
            UpdateWaveLines();
        }
        
        /// <summary>
        /// Initialize LineRenderers with proper configuration
        /// </summary>
        private void InitializeLineRenderers()
        {
            if (_sourceWaveLine != null)
            {
                _sourceWaveLine.positionCount = _waveLinePointCount;
                _sourceWaveLine.useWorldSpace = false;
                Debug.Log("WavePanel: Initialized source wave LineRenderer");
            }
            
            if (_targetWaveLine != null)
            {
                _targetWaveLine.positionCount = _waveLinePointCount;
                _targetWaveLine.useWorldSpace = false;
                Debug.Log("WavePanel: Initialized target wave LineRenderer");
            }
        }
        
        /// <summary>
        /// Update wave line rendering based on current waves and scroll offset
        /// </summary>
        private void UpdateWaveLines()
        {
            if (_sourceWave == null || _targetWave == null) return;
            if (_sourceWaveLine == null || _targetWaveLine == null) return;
            
            // Sample points from both waves
            for (int i = 0; i < _waveLinePointCount; i++)
            {
                float t = (float)i / (_waveLinePointCount - 1); // 0 to 1
                float x = t * _waveDisplayWidth - _waveDisplayWidth * 0.5f; // Center at 0
                
                // Source wave: apply scroll offset
                float sourceNormalizedPos = (t + _scrollOffset) % 1f;
                float sourceValue = _sourceWave.GetWaveValue(sourceNormalizedPos);
                Vector3 sourcePosition = new Vector3(x, sourceValue * _waveDisplayHeight, 0f);
                _sourceWaveLine.SetPosition(i, sourcePosition);
                
                // Target wave: no scroll (stationary)
                float targetValue = _targetWave.GetWaveValue(t);
                Vector3 targetPosition = new Vector3(x, targetValue * _waveDisplayHeight, 0f);
                _targetWaveLine.SetPosition(i, targetPosition);
            }
        }
        
        /// <summary>
        /// Handle QTE input from player - calculate wave match and apply damage
        /// </summary>
        private void OnQTEInput()
        {
            Debug.Log($"WavePanel: OnQTEInput called - _isWaveActive: {_isWaveActive}");
            
            if (!_isWaveActive) 
            {
                Debug.Log("WavePanel: Input ignored - wave not active");
                return;
            }
            
            if (_sourceWave == null || _targetWave == null)
            {
                Debug.LogError("WavePanel: Cannot process input - waves are null");
                return;
            }
            
            // Calculate wave match percentage
            float matchPercentage = CalculateWaveMatch();
            WaveInteractionResult result = GetInteractionResult(matchPercentage);
            
            Debug.Log($"WavePanel: Wave match: {matchPercentage:F1}%, Result: {result}");
            
            // Stop scrolling temporarily
            StopScrollingTemporarily();
            
            // Apply damage based on match quality
            ApplyWaveDamage(matchPercentage, result);
            
            // Show feedback
            ShowMatchFeedback(matchPercentage, result);
        }
        
        /// <summary>
        /// Calculate wave match percentage (0-100%)
        /// Compares source and target wave values at all sample points
        /// </summary>
        private float CalculateWaveMatch()
        {
            float totalDifference = 0f;
            int sampleCount = _waveLinePointCount;
            
            // Sample both waves at the same points
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / (sampleCount - 1); // 0 to 1
                
                // Source wave with current scroll offset
                float sourceNormalizedPos = (t + _scrollOffset) % 1f;
                float sourceValue = _sourceWave.GetWaveValue(sourceNormalizedPos);
                
                // Target wave (stationary)
                float targetValue = _targetWave.GetWaveValue(t);
                
                // Calculate absolute difference (normalized to 0-2 range since values are -1 to 1)
                float difference = Mathf.Abs(sourceValue - targetValue);
                totalDifference += difference;
            }
            
            // Calculate average difference
            float avgDifference = totalDifference / sampleCount;
            
            // Convert to match percentage
            // avgDifference ranges from 0 (perfect match) to 2 (complete mismatch)
            // Convert to 0-100% where 0 difference = 100% match
            float matchPercentage = Mathf.Clamp01(1f - (avgDifference / 2f)) * 100f;
            
            return matchPercentage;
        }
        
        /// <summary>
        /// Get wave interaction result based on match percentage
        /// </summary>
        private WaveInteractionResult GetInteractionResult(float matchPercentage)
        {
            if (matchPercentage > 90f)
                return WaveInteractionResult.Perfect;
            else if (matchPercentage > 75f)
                return WaveInteractionResult.Good;
            else
                return WaveInteractionResult.Miss;
        }
        
        /// <summary>
        /// Stop scrolling temporarily, then resume after delay
        /// </summary>
        private void StopScrollingTemporarily()
        {
            _isScrolling = false;
            
            // Stop any existing coroutine
            if (_scrollStopCoroutine != null)
            {
                StopCoroutine(_scrollStopCoroutine);
            }
            
            // Start new coroutine to resume scrolling
            _scrollStopCoroutine = StartCoroutine(ResumeScrollingAfterDelay());
        }
        
        /// <summary>
        /// Coroutine to resume scrolling after delay
        /// </summary>
        private IEnumerator ResumeScrollingAfterDelay()
        {
            yield return new WaitForSeconds(_waveStopScrollDuration);
            _isScrolling = true;
            _scrollStopCoroutine = null;
        }
        
        /// <summary>
        /// Apply wave damage to target based on match quality
        /// </summary>
        private void ApplyWaveDamage(float matchPercentage, WaveInteractionResult result)
        {
            if (_targetCore == null)
            {
                Debug.LogError("WavePanel: Cannot apply damage - target core is null");
                return;
            }
            
            // Calculate damage multiplier based on result
            float damageMultiplier = GetDamageMultiplier(result);
            float finalDamage = _baseCoreDamage * damageMultiplier;
            
            Debug.Log($"WavePanel: Applying {finalDamage:F1} core damage (multiplier: {damageMultiplier}x)");
            
            // Apply core damage
            bool damageApplied = ApplyCoreDamageToEnemy(finalDamage);
            
            if (!damageApplied)
            {
                Debug.LogWarning("WavePanel: Failed to apply core damage to enemy");
            }
        }
        
        /// <summary>
        /// Get damage multiplier based on interaction result
        /// </summary>
        private float GetDamageMultiplier(WaveInteractionResult result)
        {
            switch (result)
            {
                case WaveInteractionResult.Perfect:
                    return _perfectMatchMultiplier;
                case WaveInteractionResult.Good:
                    return _goodMatchMultiplier;
                case WaveInteractionResult.Miss:
                    return _missMatchMultiplier;
                default:
                    return 1f;
            }
        }
        
        /// <summary>
        /// Show feedback based on match quality
        /// </summary>
        private void ShowMatchFeedback(float matchPercentage, WaveInteractionResult result)
        {
            if (_instructionText == null) return;
            
            string resultText = GetResultText(result);
            float damage = _baseCoreDamage * GetDamageMultiplier(result);
            
            _instructionText.text = $"{resultText}! Match: {matchPercentage:F0}% - {damage:F0} Core Damage";
            _instructionText.color = GetResultColor(result);
            
            // Play audio/visual effects
            PlayMatchEffects(result);
        }
        
        /// <summary>
        /// Get result text string
        /// </summary>
        private string GetResultText(WaveInteractionResult result)
        {
            switch (result)
            {
                case WaveInteractionResult.Perfect:
                    return "PERFECT";
                case WaveInteractionResult.Good:
                    return "GOOD";
                case WaveInteractionResult.Miss:
                    return "MISS";
                default:
                    return "UNKNOWN";
            }
        }
        
        /// <summary>
        /// Get result color
        /// </summary>
        private Color GetResultColor(WaveInteractionResult result)
        {
            switch (result)
            {
                case WaveInteractionResult.Perfect:
                    return Color.cyan;
                case WaveInteractionResult.Good:
                    return Color.green;
                case WaveInteractionResult.Miss:
                    return Color.red;
                default:
                    return Color.white;
            }
        }
        
        /// <summary>
        /// Play match effects based on result
        /// </summary>
        private void PlayMatchEffects(WaveInteractionResult result)
        {
            var audioService = ServiceRegistry.Get<IAudioService>();
            if (audioService == null) return;
            
            switch (result)
            {
                case WaveInteractionResult.Perfect:
                    audioService.PlaySFX2D(AudioClipType.EnemyHit, 1.0f, 1.2f);
                    break;
                case WaveInteractionResult.Good:
                    audioService.PlaySFX2D(AudioClipType.EnemyHit, 0.8f, 1.0f);
                    break;
                case WaveInteractionResult.Miss:
                    audioService.PlaySFX2D(AudioClipType.EnemyHit, 0.4f, 0.6f);
                    break;
            }
        }
        
        #endregion
        
        #region Damage System
        
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
        
        #endregion
    }
}