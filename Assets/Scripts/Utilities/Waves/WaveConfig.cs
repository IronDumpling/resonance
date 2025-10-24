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
        
        [Header("QTE Configuration")]
        [Tooltip("QTE Configuration")]
        public QTEConfig qteConfig = new QTEConfig();
        
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
            
            if (qteConfig != null && !qteConfig.ValidateConfig())
            {
                Debug.LogError($"WaveConfig: {name} has invalid QTE config");
                return false;
            }
            
            return true;
        }
        
        #region Unity Editor
        
        void OnValidate()
        {
            maxChaos = Mathf.Max(1f, maxChaos);
            chaosThreshold = Mathf.Clamp(chaosThreshold, 0f, maxChaos - 1f);
            
            if (qteConfig != null)
            {
                qteConfig.cycleDuration = Mathf.Max(0.5f, qteConfig.cycleDuration);
                qteConfig.targetWindow = Mathf.Clamp(qteConfig.targetWindow, 0.05f, 0.5f);
            }
        }
        
        #endregion
    }

    /// <summary>
    /// QTE configuration data structure
    /// Used for wave wave QTE mechanics
    /// </summary>
    [System.Serializable]
    public class QTEConfig
    {
        [Header("QTE Animation")]
        [Tooltip("QTE Animation Ease Type")]
        public Ease easeType = Ease.InOutSine;
        
        [Tooltip("QTE Cycle Duration (seconds)")]
        public float cycleDuration = 3f;
        
        [Tooltip("QTE Target Window Size (0-1)")]
        [Range(0.05f, 0.5f)]
        public float targetWindow = 0.2f;
        
        [Header("QTE Visual")]
        [Tooltip("QTE Success Color")]
        public Color successColor = Color.green;
        
        [Tooltip("QTE Failure Color")]
        public Color failureColor = Color.red;
        
        [Tooltip("QTE Target Window Color")]
        public Color targetColor = Color.yellow;
        
        [Header("QTE Audio")]
        [Tooltip("QTE Success Sound")]
        public AudioClip successSound;
        
        [Tooltip("QTE Failure Sound")]
        public AudioClip failureSound;

        public QTEConfig()
        {
            easeType = Ease.InOutSine;
            cycleDuration = 3f;
            targetWindow = 0.2f;
            successColor = Color.green;
            failureColor = Color.red;
            targetColor = Color.yellow;
        }

        public QTEConfig(Ease easeType, float cycleDuration, float targetWindow)
        {
            this.easeType = easeType;
            this.cycleDuration = cycleDuration;
            this.targetWindow = targetWindow;
            successColor = Color.green;
            failureColor = Color.red;
            targetColor = Color.yellow;
        }
        
        /// <summary>
        /// Validate QTE Configuration
        /// </summary>
        public bool ValidateConfig()
        {
            if (cycleDuration <= 0f)
            {
                Debug.LogError($"QTEConfig: Invalid cycleDuration: {cycleDuration}");
                return false;
            }
            
            if (targetWindow <= 0f || targetWindow >= 1f)
            {
                Debug.LogError($"QTEConfig: Invalid targetWindow: {targetWindow} (should be 0 < targetWindow < 1)");
                return false;
            }
            
            return true;
        }
    }
}