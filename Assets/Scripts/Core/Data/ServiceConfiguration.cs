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

        public AudioMixerGroup weaponsMixerGroup;
        
        public AudioMixerGroup enemiesMixerGroup;
        public AudioMixerGroup enemiesMovementMixerGroup;
        public AudioMixerGroup enemiesHurtMixerGroup;
        public AudioMixerGroup enemiesDeathMixerGroup;
        public AudioMixerGroup enemiesAttackMixerGroup;

        public AudioMixerGroup playerMixerGroup;
        public AudioMixerGroup playerMovementMixerGroup;
        public AudioMixerGroup playerHurtMixerGroup;
        public AudioMixerGroup playerDeathMixerGroup;
        public AudioMixerGroup playerInteractionMixerGroup;

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
                case AudioClipType.GunFirePistol:
                case AudioClipType.GunFireRifle:
                case AudioClipType.GunReload:
                case AudioClipType.GunEmpty:
                case AudioClipType.GunCock:
                    return weaponsMixerGroup;
                
                // Enemy SFX
                case AudioClipType.EnemyHit:
                case AudioClipType.EnemyHitMetal:
                case AudioClipType.EnemyHitFlesh:
                    return enemiesHurtMixerGroup;
                case AudioClipType.EnemyDeath:
                case AudioClipType.EnemyDeathExplosion:
                    return enemiesDeathMixerGroup;
                case AudioClipType.EnemyAttack:
                    return enemiesAttackMixerGroup;

                // Player SFX
                case AudioClipType.PlayerHit:
                    return playerHurtMixerGroup;
                case AudioClipType.PlayerDeath:
                    return playerDeathMixerGroup;
                case AudioClipType.ItemPickup:
                case AudioClipType.ItemDrop:
                case AudioClipType.DoorOpen:
                case AudioClipType.DoorClose:
                case AudioClipType.ButtonPress:
                    return playerInteractionMixerGroup;
                
                // Player Movement SFX
                case AudioClipType.FootstepWalk:
                case AudioClipType.FootstepRun:
                case AudioClipType.FootstepStop:
                case AudioClipType.FootstepGrass:
                case AudioClipType.FootstepStone:
                case AudioClipType.FootstepMetal:
                    return playerMovementMixerGroup;
                
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
