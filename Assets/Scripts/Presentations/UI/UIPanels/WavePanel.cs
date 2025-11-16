using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Resonance.Core;
using Resonance.Shared.Interfaces;
using Resonance.Shared.Interfaces.Services;
using Resonance.Gameplay.Player.Triggers;
using Resonance.Gameplay.Enemies.Triggers;
using Resonance.Utilities;
using Resonance.Shared.Types;
using Resonance.Systems.Waves;
using Resonance.Systems.Waves.WavePhenomenons;
using Resonance.Systems.CrystalCore;

namespace Resonance.Presentations.UI
{
    public class WavePanel : UIPanel
    {
        [Header("Wave UI Elements")]
        [SerializeField] private RectTransform _waveScopeArea;
        [SerializeField] private LineRenderer _targetWaveLine;
        [SerializeField] private LineRenderer _sourceWaveLine;

        [Header("Wave Visual Configuration")]
        [SerializeField] private float _waveLineWidth = 0.002f;
        [SerializeField] private float _waveScrollSpeed = 1f;
        [SerializeField] private float _waveStopScrollDuration = 0.5f;

        [Header("Information UI Elements")]
        [SerializeField] private TextMeshProUGUI _instructionText;
        
        [Header("Wave Damage Configuration")]
        [SerializeField] private float _perfectMatchMultiplier = 3f;  // Perfect: >90%
        [SerializeField] private float _goodMatchMultiplier = 1f;     // Good: >75%
        [SerializeField] private float _missMatchMultiplier = 0f;     // Miss: <75%

        private Canvas _canvas;
        private Camera _renderCamera;
        private float _defaultPlaneDistance = 1f;
        private Vector3[] _scopeAreaCorners = new Vector3[4];
        
        // Wave system references
        private IInputService _inputService;
        private bool _isInitialized = false;
        private bool _isWaveActive = false;
        
        private IWavable _sourceWavable;  // The attacker
        private IWavable _targetWavable;  // The target being attacked
        private Wave _sourceWave;
        private Wave _targetWave;
        
        // Wave scrolling state
        private float _scrollOffset = 0f;
        private bool _isScrolling = true;
        private Coroutine _scrollStopCoroutine;
        
        // Input filtering
        private float _panelOpenTime = 0f;

        #region Unity Lifecycle

        /// <summary>
        /// Awake method - initializes the WavePanel
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            _panelName = "WavePanel";
            _layer = UILayer.Game;
            _hideOnStart = true;

            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                Debug.LogError("WavePanel: Canvas component not found on this GameObject");
                enabled = false;
                return;
            }
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

        #endregion

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
                _inputService.OnAttackQTE += OnAttackQTEInput;
                _inputService.OnLogicBind1 += OnLogicBind1Input;
                Debug.Log("WavePanel: Subscribed to Attack QTE and Logic Bind 1 input events");
            }

            // Initialize canvas camera
            InitializeCanvasCamera();
            
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
            
            // Ensure player input is re-enabled when panel is hidden
            if (_inputService != null)
            {
                _inputService.EnablePlayerInput();
                Debug.Log("WavePanel: Player input re-enabled on panel hide");
            }
            
            // Stop wave display
            StopWaveDisplay();
        }

        protected override void OnCleanup()
        {
            // Ensure player input is re-enabled during cleanup
            if (_inputService != null)
            {
                _inputService.EnablePlayerInput();
                _inputService.OnAttackQTE -= OnAttackQTEInput;
                _inputService.OnLogicBind1 -= OnLogicBind1Input;
                Debug.Log("WavePanel: Player input re-enabled and unsubscribed from Attack QTE and Logic Bind 1 input events");
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
        
        #region UI Elements Initialization
        
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

        /// <summary>
        /// Initialize the canvas camera
        /// </summary>
        private void InitializeCanvasCamera()
        {
            if (_canvas == null) return;

            // 1. Set render mode
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;

            // 2. Find and set render camera
            GameObject cameraGO = GameObject.FindWithTag("MainCamera");
            if (cameraGO != null)
            {
                _renderCamera = cameraGO.GetComponent<Camera>();
                if (_renderCamera != null)
                {
                    _canvas.worldCamera = _renderCamera;
                    Debug.Log("WavePanel: Found and assigned Render Camera: " + _renderCamera.name);
                }
                else
                {
                    Debug.LogError("WavePanel: GameObject tagged 'MainCamera' does not have a Camera component!");
                }
            }
            else
            {
                Debug.LogError("WavePanel: Could not find GameObject tagged 'MainCamera'!");
            }

            // 3. Set Plane Distance
            // Set a value close to the camera's near clip plane to ensure the UI is in front of most objects
            _canvas.planeDistance = (_renderCamera != null) ? _renderCamera.nearClipPlane + 0.1f : _defaultPlaneDistance;
            Debug.Log($"WavePanel: Set Plane Distance to: {_canvas.planeDistance}");
        }
        
        #endregion
        
        #region Wave Line Logic

        /// <summary>
        /// Initialize LineRenderers with proper configuration
        /// </summary>
        private void InitializeLineRenderers()
        {
            int waveformCount = Wave.WaveformResolution;
            
            if (_sourceWaveLine != null)
            {
                _sourceWaveLine.positionCount = waveformCount;
                _sourceWaveLine.useWorldSpace = true;
                _sourceWaveLine.startWidth = _waveLineWidth;
                _sourceWaveLine.endWidth = _waveLineWidth;
                Debug.Log("WavePanel: Initialized source wave LineRenderer");
            }
            
            if (_targetWaveLine != null)
            {
                _targetWaveLine.positionCount = waveformCount;
                _targetWaveLine.useWorldSpace = true;
                _targetWaveLine.startWidth = _waveLineWidth;
                _targetWaveLine.endWidth = _waveLineWidth;
                Debug.Log("WavePanel: Initialized target wave LineRenderer");
            }
        }
        
        /// <summary>
        /// Set the wave attack context (source and target IWavables)
        /// </summary>
        /// <param name="sourceWavable">The attacker (player or enemy)</param>
        /// <param name="targetWavable">The target being attacked</param>
        public void SetWaveAttackContext(IWavable sourceWavable, IWavable targetWavable)
        {
            if (sourceWavable == null || targetWavable == null)
            {
                Debug.LogError("WavePanel: Cannot set wave attack context with null wavables");
                return;
            }
            
            _sourceWavable = sourceWavable;
            _targetWavable = targetWavable;
            
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

            if (_sourceWaveLine != null)
            {
                _sourceWaveLine.enabled = true;
                _sourceWaveLine.positionCount = Wave.WaveformResolution;
            }

            if (_targetWaveLine != null)
            {
                _targetWaveLine.enabled = true;
                _targetWaveLine.positionCount = Wave.WaveformResolution;
            }
            
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
                _sourceWaveLine.enabled = false;
            }
            
            if (_targetWaveLine != null)
            {
                _targetWaveLine.positionCount = 0;
                _targetWaveLine.enabled = false;
            }
            
            Debug.Log("WavePanel: Stopped wave display");
        }
        
        /// <summary>
        /// Update wave line rendering based on current waves and scroll offset
        /// </summary>
        private void UpdateWaveLines()
        {
            if (_sourceWave == null || _targetWave == null) return;
            if (_sourceWaveLine == null || _targetWaveLine == null) return;

            _waveScopeArea.GetWorldCorners(_scopeAreaCorners);
            float worldWidth = Vector3.Distance(_scopeAreaCorners[0], _scopeAreaCorners[3]);
            float worldHeight = Vector3.Distance(_scopeAreaCorners[0], _scopeAreaCorners[1]);

            Vector3 bottomLeftOrigin = _scopeAreaCorners[0];
            float worldZ = bottomLeftOrigin.z;

            int waveformCount = Wave.WaveformResolution;

            // Sample points from both waves
            for (int i = 0; i < waveformCount; i++)
            {
                float t = (float)i / (waveformCount - 1); // 0 to 1
                float x = bottomLeftOrigin.x + t * worldWidth;
                
                // Source wave Y Position: Scroll
                float sourceScrollPos = (t + _scrollOffset) % 1f;
                float sourceValueRaw = _sourceWave.GetWaveValue(sourceScrollPos);
                float sourceNormalizedY = (_sourceWave.Amplitude > 0) ? (sourceValueRaw / _sourceWave.Amplitude + 1f) * 0.5f : 0.5f;
                float sourceY = bottomLeftOrigin.y + sourceNormalizedY * worldHeight;
                Vector3 sourcePosition = new (x, sourceY, worldZ);
                _sourceWaveLine.SetPosition(i, sourcePosition);
                
                // Target wave Y Position: Stationary
                float targetValueRaw = _targetWave.GetWaveValue(t);
                float targetNormalizedY = (_targetWave.Amplitude > 0) ? (targetValueRaw / _targetWave.Amplitude + 1f) * 0.5f : 0.5f;
                float targetY = bottomLeftOrigin.y + targetNormalizedY * worldHeight;
                Vector3 targetPosition = new (x, targetY, worldZ);
                _targetWaveLine.SetPosition(i, targetPosition);
            }

            Debug.Log($"WavePanel: Source wave amplitude: {_sourceWave.Amplitude}, Target wave amplitude: {_targetWave.Amplitude}");
        }
        
        #endregion

        #region Input Logic

        /// <summary>
        /// Handle QTE input from player - calculate wave match and apply damage
        /// </summary>
        private void OnAttackQTEInput()
        {
            Debug.Log($"WavePanel: OnAttackQTEInput called - _isWaveActive: {_isWaveActive}");

            if (_sourceWavable is EnemyCrystalCoreHitbox)
            {
                Debug.Log("WavePanel: Attack QTE input ignored - Enemy is attacking the player");
                return;
            }

            if (!_isWaveActive) 
            {
                Debug.Log("WavePanel: Input ignored - wave not active");
                return;
            }

            if (!_isScrolling)
            {
                Debug.Log("WavePanel: Input ignored - wave is not scrolling");
                return;
            }
            
            // Trigger Attack QTE
            ProcessWaveTrigger(null);
        }

        private void OnLogicBind1Input()
        {
            Debug.Log("WavePanel: OnLogicBind1Input called");

            PlayerCrystalCoreHitbox playerCore = _sourceWavable as PlayerCrystalCoreHitbox;
            if (playerCore == null)
            {
                playerCore = _targetWavable as PlayerCrystalCoreHitbox;
                if (playerCore == null)
                {
                    Debug.LogWarning("WavePanel: OnLogicBind1Input ignored - source and target wavables are not PlayerCrystalCoreHitboxes");
                    return;
                }
            }

            WaveModifier.Modify(playerCore.GetWave().WaveformTable, WaveModifierType.Inverter);
        }

        #endregion
        
        #region Public QTE Interface
        
        /// <summary>
        /// Get current wave match percentage (for AI polling)
        /// </summary>
        /// <returns>Match percentage (0-100), or -1 if wave not active</returns>
        public float GetCurrentMatchPercentage()
        {
            if (!_isWaveActive || _sourceWave == null || _targetWave == null)
            {
                return -1f;
            }
            
            return CalculateWaveMatch();
        }
        
        /// <summary>
        /// Process wave trigger - calculate/apply damage and show feedback
        /// Can be called by player input or enemy AI
        /// </summary>
        /// <param name="forcedResult">If provided, use this result instead of calculating from match percentage</param>
        /// <returns>The effective interaction result</returns>
        public WaveInteractionResult ProcessWaveTrigger(WaveInteractionResult? forcedResult = null)
        {
            if (_sourceWave == null || _targetWave == null)
            {
                Debug.LogError("WavePanel: Cannot process trigger - waves are null");
                return WaveInteractionResult.Miss;
            }
            
            if (_targetWavable == null)
            {
                Debug.LogError("WavePanel: Cannot process trigger - target wavable is null");
                return WaveInteractionResult.Miss;
            }

            // Determine the effective result
            WaveInteractionResult effectiveResult;
            float matchPercentage = 0f;
            
            if (forcedResult.HasValue)
            {
                // AI forced result (e.g., Perfect)
                effectiveResult = forcedResult.Value;
                matchPercentage = 100f; // Use max for display
                Debug.Log($"WavePanel: Using forced result: {effectiveResult}");
            }
            else
            {
                // Calculate from current wave alignment
                matchPercentage = CalculateWaveMatch();
                WaveInteractionResult result = GetInteractionResult(matchPercentage);
                
                effectiveResult = result;
                
                Debug.Log($"WavePanel: Calculated match: {matchPercentage:F1}%, Result: {result}, Effective Result: {effectiveResult}");
            }
            
            // Process wave interaction (apply damage)
            float damageMultiplier = GetDamageMultiplier(effectiveResult);
            Damages finalDamages = CalculateFinalDamages(damageMultiplier);
            bool damageApplied = _targetWavable.ApplyWaveDamages(finalDamages, _sourceWavable, "Wave QTE Damage");
            if (!damageApplied)
            {
                Debug.LogWarning("WavePanel: Failed to process wave interaction");
            }
            
            // Show visual/audio feedback
            ShowMatchFeedback(matchPercentage, effectiveResult);

            // Stop scrolling temporarily
            StopScrollingTemporarily();
            
            return effectiveResult;
        }

        #endregion
        
        #region Wave Calculation
        
        /// <summary>
        /// Calculate wave match percentage (0-100%)
        /// Uses WaveSuperposition to calculate match between source and target waves
        /// </summary>
        private float CalculateWaveMatch()
        {
            if (_sourceWave == null || _targetWave == null)
            {
                Debug.LogWarning("WavePanel: Cannot calculate wave match - waves are null");
                return 0f;
            }
            
            // Use WaveSuperposition to calculate match percentage
            // _scrollOffset is used as phase offset between the two waves
            return WaveSuperposition.CalculateMatchPercentage(_sourceWave, _targetWave, _scrollOffset);
        }
        
        /// <summary>
        /// Stop scrolling temporarily, then resume after delay
        /// Also disables wave input during the pause
        /// </summary>
        private void StopScrollingTemporarily()
        {
            _isScrolling = false;
            
            // Disable wave input during wave animation pause
            if (_inputService != null)
            {
                _inputService.DisableWaveInput();
                Debug.Log("WavePanel: Wave input disabled during wave animation pause");
            }
            
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
        /// Also re-enables wave input when resuming
        /// </summary>
        private IEnumerator ResumeScrollingAfterDelay()
        {
            yield return new WaitForSeconds(_waveStopScrollDuration);
            
            // Re-enable wave input when resuming scrolling
            if (_inputService != null)
            {
                _inputService.EnableWaveInput();
                Debug.Log("WavePanel: Wave input re-enabled after wave animation pause");
            }
            
            _isScrolling = true;
            _scrollStopCoroutine = null;
        }

        #endregion

        #region Visual Feedback Logic
        
        /// <summary>
        /// Show feedback based on match quality
        /// </summary>
        private void ShowMatchFeedback(float matchPercentage, WaveInteractionResult result)
        {
            if (_instructionText == null) return;
            
            string resultText = GetResultText(result);
            float baseCoreDamage = GetBaseCoreDamage();
            float damage = baseCoreDamage * GetDamageMultiplier(result);
            
            _instructionText.text = $"{resultText}! Match: {matchPercentage:F0}% - {damage:F0} Core Damage";
            _instructionText.color = GetResultColor(result);
            
            // Play audio/visual effects
            PlayMatchEffects(result);
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
        /// Get the base core damage from the source wavable (attacker)
        /// </summary>
        private float GetBaseCoreDamage()
        {
            if (_sourceWavable != null)
            {
                Damages damages = _sourceWavable.GetWaveBaseDamages();
                return damages.GetDamage(DamageType.CoreHealth);
            }
            
            Debug.LogWarning("WavePanel: Source wavable is null, returning 0 damage");
            return 0f;
        }

        /// <summary>
        /// Get the base damages from the source wavable (attacker)
        /// </summary>
        private Damages GetBaseDamages()
        {
            if (_sourceWavable != null)
            {
                return _sourceWavable.GetWaveBaseDamages();
            }
            
            Debug.LogWarning("WavePanel: Source wavable is null, returning empty damages");
            return new Damages();
        }

        /// <summary>
        /// Calculate final damages by applying multiplier to both CoreHealth damage
        /// </summary>
        private Damages CalculateFinalDamages(float damageMultiplier)
        {
            Damages baseDamages = GetBaseDamages();
            float baseCoreDamage = baseDamages.GetDamage(DamageType.CoreHealth);
            float baseBalanceDamage = baseDamages.GetDamage(DamageType.Balance);
            
            Damages finalDamages = new Damages();
            finalDamages.SetDamage(DamageType.CoreHealth, baseCoreDamage * damageMultiplier);
            finalDamages.SetDamage(DamageType.Balance, baseBalanceDamage * damageMultiplier);
            
            Debug.Log($"WavePanel: Calculated damages - " +
                      $"CoreHealth: {finalDamages.GetDamage(DamageType.CoreHealth):F1} " +
                      $"(base: {baseCoreDamage:F1}), " +
                      $"Balance: {finalDamages.GetDamage(DamageType.Balance):F1} " +
                      $"(base: {baseBalanceDamage:F1}), " +
                      $"Multiplier: {damageMultiplier}x");
            
            return finalDamages;
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
        
        #endregion
    }
}