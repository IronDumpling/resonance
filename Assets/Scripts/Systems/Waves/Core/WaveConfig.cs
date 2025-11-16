using UnityEngine;
using DG.Tweening;
using Resonance.Shared.Types;

namespace Resonance.Systems.Waves
{
    /// <summary>
    /// Wave configuration data structure
    /// Contains all wave-related settings including QTE
    /// </summary>
    [CreateAssetMenu(fileName = "New Wave Config", menuName = "Resonance/Core/Wave Config")]
    public class WaveConfig : ScriptableObject
    {
        [Header("Primary Wave Properties")]
        [Tooltip("Waveform type")]
        public WaveformType waveformType = WaveformType.Sine;
        
        [Tooltip("Frequency - cycles per unit length (0.1-10 typical range)")]
        [Range(0.1f, 20.0f)]
        public float frequency = 2.0f;
        
        [Tooltip("Amplitude - peak deviation (0.1-10 typical range)")]
        [Range(0.1f, 20.0f)]
        public float amplitude = 1.0f;
        
        [Tooltip("Unit - how many unit waves (affects total energy)")]
        [Range(0.1f, 10.0f)]
        public float unit = 1.0f;
        
        [Tooltip("Waveform Resolution - samples per unit wave")]
        [Range(64, 2048)]
        public int waveformResolution = WaveConstants.DEFAULT_WAVEFORM_RESOLUTION;
        
        [Header("Wave Visual Configuration")]
        [Tooltip("Wave Order Color")]
        public Color orderColor = Color.blue;
        
        [Tooltip("Wave Active Color")]
        public Color activeColor = Color.magenta;
        
        [Tooltip("Wave Material Path")]
        public string waveMaterialPath = "Art/Materials/Wave";
        
        [Header("Secondary Properties Preview (Read-Only)")]
        [Tooltip("These are calculated automatically - shown for design reference")]
        [SerializeField, HideInInspector] private float _previewEnergyStrength;
        [SerializeField, HideInInspector] private float _previewAttenuationFactor;
        [SerializeField, HideInInspector] private float _previewReflectionFactor;
        [SerializeField, HideInInspector] private float _previewPenetrationFactor;
        [SerializeField, HideInInspector] private float _previewDiffractionFactor;
        [SerializeField, HideInInspector] private float _previewAbsorptionFactor;
        [SerializeField, HideInInspector] private float _previewSpeed;
        [SerializeField, HideInInspector] private float _previewEffectiveRange;
        
        /// <summary>
        /// Validate Wave configuration
        /// </summary>
        public bool ValidateConfig()
        {
            if (frequency <= 0f)
            {
                Debug.LogError($"WaveConfig: {name} has invalid frequency: {frequency}");
                return false;
            }
            
            if (amplitude <= 0f)
            {
                Debug.LogError($"WaveConfig: {name} has invalid amplitude: {amplitude}");
                return false;
            }
            
            if (unit <= 0f)
            {
                Debug.LogError($"WaveConfig: {name} has invalid unit: {unit}");
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Create a preview wave to show calculated properties in inspector
        /// </summary>
        public void UpdatePreviewProperties()
        {
            Wave previewWave = new Wave(this);
            _previewEnergyStrength = previewWave.EnergyStrength;
            _previewAttenuationFactor = previewWave.EnergyAttenuationFactor;
            _previewReflectionFactor = previewWave.ReflectionFactor;
            _previewPenetrationFactor = previewWave.PenetrationFactor;
            _previewDiffractionFactor = previewWave.DiffractionFactor;
            _previewAbsorptionFactor = previewWave.AbsorptionFactor;
            _previewSpeed = previewWave.Speed;
            _previewEffectiveRange = previewWave.GetEffectiveRange();
            previewWave.Cleanup();
        }
        
        #region Unity Editor
        
        void OnValidate()
        {
            frequency = Mathf.Max(WaveConstants.MIN_FREQUENCY, frequency);
            amplitude = Mathf.Max(WaveConstants.MIN_AMPLITUDE, amplitude);
            unit = Mathf.Max(WaveConstants.MIN_UNIT, unit);
            waveformResolution = Mathf.Clamp(waveformResolution, WaveConstants.MIN_WAVEFORM_RESOLUTION, WaveConstants.MAX_WAVEFORM_RESOLUTION);
            
            // Update preview in editor
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UpdatePreviewProperties();
            }
            #endif
        }
        
        #endregion
    }
}
