using UnityEngine;
using DG.Tweening;

namespace Resonance.Utilities.Waves
{
    /// <summary>
    /// Wave configuration data structure
    /// Contains all wave-related settings including QTE
    /// </summary>
    [CreateAssetMenu(fileName = "New Wave Config", menuName = "Resonance/Core/Wave Config")]
    public class WaveConfig : ScriptableObject
    {
        [Header("Wave Properties")]
        [Tooltip("Waveform type")]
        public WaveformType waveformType = WaveformType.Sine;
        [Tooltip("Frequency")]
        public float frequency = 1.0f;
        [Tooltip("Amplitude")]
        public float amplitude = 1.0f;
        [Tooltip("Length")]
        public float length = 10.0f;
        [Tooltip("Waveform Resolution")]
        public int waveformResolution = 1024;
        
        [Header("Wave Chaos Configuration")]
        [Tooltip("Wave Chaos Max Value")]
        public float maxChaos = 20f;
        [Tooltip("Wave Chaos Threshold Value")]
        public float chaosThreshold = 16f;
        
        [Header("Wave Visual Configuration")]
        [Tooltip("Wave Order Color")]
        public Color orderColor = Color.blue;
        [Tooltip("Wave Chaos Color")]
        public Color chaosColor = Color.magenta;
        [Tooltip("Wave Material Path")]
        public string waveMaterialPath = "Art/Materials/Wave";
        
        [Header("Wave Audio Configuration")]
        [Tooltip("Wave Chaos Add Sound")]
        public AudioClip chaosAddSound;
        
        [Tooltip("Wave Enter Chaos State Sound")]
        public AudioClip enterChaosStateSound;
        
        [Tooltip("Wave Enter Order State Sound")]
        public AudioClip enterOrderStateSound;
        
        /// <summary>
        /// 验证Wave配置
        /// </summary>
        public bool ValidateConfig()
        {
            if (maxChaos <= 0f)
            {
                Debug.LogError($"WaveConfig: {name} has invalid maxChaos: {maxChaos}");
                return false;
            }
            
            if (chaosThreshold < 0f || chaosThreshold >= maxChaos)
            {
                Debug.LogError($"WaveConfig: {name} has invalid chaosThreshold: {chaosThreshold} (should be 0 <= chaosThreshold < maxChaos)");
                return false;
            }
            
            return true;
        }
        
        #region Unity Editor
        
        void OnValidate()
        {
            maxChaos = Mathf.Max(1f, maxChaos);
            chaosThreshold = Mathf.Clamp(chaosThreshold, 0f, maxChaos - 1f);
        }
        
        #endregion
    }
}