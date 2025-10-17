using UnityEngine;

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
        [Tooltip("最大紊乱值")]
        public float maxChaos = 20f;
        
        [Tooltip("紊乱阈值(低于此值进入秩序状态)")]
        public float chaosThreshold = 16f;
        
        [Header("Wave Visual Configuration")]
        [Tooltip("波纹秩序状态颜色")]
        public Color orderColor = Color.blue;
        
        [Tooltip("波纹紊乱状态颜色")]
        public Color chaosColor = Color.magenta;
        
        [Tooltip("波纹材质路径")]
        public string waveMaterialPath = "Art/Materials/Wave";
        
        [Header("Wave Audio Configuration")]
        [Tooltip("紊乱增加音效")]
        public AudioClip chaosAddSound;
        
        [Tooltip("进入紊乱状态音效")]
        public AudioClip enterChaosStateSound;
        
        [Tooltip("进入秩序状态音效")]
        public AudioClip enterOrderStateSound;
        
        [Header("QTE Configuration")]
        [Tooltip("QTE配置")]
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
}