using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Audio;
using Resonance.Interfaces.Services;

namespace Resonance.Core
{
    [CreateAssetMenu(fileName = "ServiceConfiguration", menuName = "Resonance/Service Configuration")]
    public class ServiceConfiguration : ScriptableObject
    {
        [Header("Input System Configuration")]
        public InputActionAsset inputActions;
        
        [Header("Audio System Configuration")]
        public AudioMixer audioMixer;
        public AudioMixerGroup masterMixerGroup;
        public AudioMixerGroup sfxMixerGroup;
        public AudioMixerGroup musicMixerGroup;
        public AudioMixerGroup combatMixerGroup;
        public AudioMixerGroup movementMixerGroup;
        public AudioMixerGroup uiMixerGroup;
        
        [Header("Audio Clip Configuration")]
        public AudioClipEntry[] audioClipEntries;
        
        [Header("Audio Settings")]
        [Range(0f, 1f)]
        public float defaultMasterVolume = 1f;
        [Range(0f, 1f)]
        public float defaultSFXVolume = 1f;
        [Range(0f, 1f)]
        public float defaultMusicVolume = 0.7f;
        
        [Header("Audio Pool Settings")]
        [Range(5, 50)]
        public int audioSourcePoolSize = 20;
        public bool enableAudioSourcePooling = true;
        
        [Header("Save System Configuration")]
        public string saveFilePath = "SaveData";
        
        [Header("Resource System Configuration")]
        public bool useAddressables = true;
        
        // Future service configurations can be added here
        // This centralizes all service configuration in one asset
        
        #region Audio Helper Methods
        
        /// <summary>
        /// 根据音效类型获取音频剪辑数据
        /// </summary>
        /// <param name="clipType">音效类型</param>
        /// <returns>音频剪辑数据，如果没找到返回null</returns>
        public AudioClipData GetAudioClipData(AudioClipType clipType)
        {
            if (audioClipEntries == null) return null;
            
            // 直接通过clipType匹配，不依赖索引顺序
            for (int i = 0; i < audioClipEntries.Length; i++)
            {
                if (audioClipEntries[i].clipType == clipType)
                {
                    return audioClipEntries[i].clipData;
                }
            }
            
            Debug.LogWarning($"ServiceConfiguration: Audio clip data not found for {clipType}. Please add an AudioClipEntry for this type in the Inspector.");
            return null;
        }
        
        /// <summary>
        /// 根据音效类型获取对应的混合器组
        /// </summary>
        /// <param name="clipType">音效类型</param>
        /// <returns>音频混合器组</returns>
        public AudioMixerGroup GetMixerGroupForClipType(AudioClipType clipType)
        {
            // 根据音效类型返回对应的混合器组
            switch (clipType)
            {
                // 射击音效 -> 战斗组
                case AudioClipType.GunFirePistol:
                case AudioClipType.GunFireRifle:
                case AudioClipType.GunReload:
                case AudioClipType.GunEmpty:
                case AudioClipType.GunCock:
                    return combatMixerGroup;
                
                // 战斗音效 -> 战斗组
                case AudioClipType.EnemyHit:
                case AudioClipType.EnemyHitMetal:
                case AudioClipType.EnemyHitFlesh:
                case AudioClipType.EnemyDeath:
                case AudioClipType.EnemyDeathExplosion:
                case AudioClipType.PlayerHit:
                case AudioClipType.PlayerDeath:
                    return combatMixerGroup;
                
                // 移动音效 -> 移动组
                case AudioClipType.FootstepWalk:
                case AudioClipType.FootstepRun:
                case AudioClipType.FootstepStop:
                case AudioClipType.FootstepGrass:
                case AudioClipType.FootstepStone:
                case AudioClipType.FootstepMetal:
                    return movementMixerGroup;
                
                // UI音效 -> UI组
                case AudioClipType.UIButtonClick:
                case AudioClipType.UIButtonHover:
                case AudioClipType.UIMenuOpen:
                case AudioClipType.UIMenuClose:
                case AudioClipType.UINotification:
                    return uiMixerGroup;
                
                // 其他音效 -> SFX组
                default:
                    return sfxMixerGroup;
            }
        }
        
        #endregion
    }
}
