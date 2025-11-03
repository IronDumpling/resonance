using UnityEngine;
using DG.Tweening;
using Resonance.Utilities.Types;

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
        public float frequency = 2.0f;
        [Tooltip("Amplitude")]
        public float amplitude = 1.0f;
        [Tooltip("Length")]
        public float length = 10.0f;
        [Tooltip("Waveform Resolution")]
        public int waveformResolution = 1024;
        
        [Header("Wave Visual Configuration")]
        [Tooltip("Wave Order Color")]
        public Color orderColor = Color.blue;
        [Tooltip("Wave Active Color")]
        public Color activeColor = Color.magenta;
        [Tooltip("Wave Material Path")]
        public string waveMaterialPath = "Art/Materials/Wave";
        
        /// <summary>
        /// 验证Wave配置
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
            
            if (length <= 0f)
            {
                Debug.LogError($"WaveConfig: {name} has invalid length: {length}");
                return false;
            }

            return true;
        }
        
        #region Unity Editor
        
        void OnValidate()
        {
            frequency = Mathf.Max(0.1f, frequency);
            amplitude = Mathf.Max(0.1f, amplitude);
            length = Mathf.Max(0.1f, length);
        }
        
        #endregion
    }
}