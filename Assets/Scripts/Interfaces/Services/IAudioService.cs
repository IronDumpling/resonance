using UnityEngine;
using UnityEngine.Audio;

namespace Resonance.Interfaces.Services
{
    /// <summary>
    /// 音频服务接口
    /// 管理游戏中所有音效播放、音量控制和音频混合器
    /// </summary>
    public interface IAudioService : IGameService
    {
        #region Audio Playback
        
        /// <summary>
        /// 播放3D音效，具有空间定位
        /// </summary>
        /// <param name="clipType">音效类型</param>
        /// <param name="position">播放位置</param>
        /// <param name="volume">音量 (0-1)</param>
        /// <param name="pitch">音调 (0.1-3)</param>
        /// <returns>音频源引用，可用于控制播放</returns>
        AudioSource PlaySFX3D(AudioClipType clipType, Vector3 position, float volume = 1f, float pitch = 1f);
        
        /// <summary>
        /// 播放2D音效，无空间定位
        /// </summary>
        /// <param name="clipType">音效类型</param>
        /// <param name="volume">音量 (0-1)</param>
        /// <param name="pitch">音调 (0.1-3)</param>
        /// <returns>音频源引用，可用于控制播放</returns>
        AudioSource PlaySFX2D(AudioClipType clipType, float volume = 1f, float pitch = 1f);
        
        /// <summary>
        /// 播放循环音效（如脚步声）
        /// </summary>
        /// <param name="clipType">音效类型</param>
        /// <param name="position">播放位置</param>
        /// <param name="volume">音量 (0-1)</param>
        /// <param name="pitch">音调 (0.1-3)</param>
        /// <returns>音频源引用，可用于控制播放</returns>
        AudioSource PlayLoopingSFX(AudioClipType clipType, Vector3 position, float volume = 1f, float pitch = 1f);
        
        /// <summary>
        /// 停止循环音效
        /// </summary>
        /// <param name="audioSource">要停止的音频源</param>
        void StopLoopingSFX(AudioSource audioSource);
        
        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="musicClip">音乐片段</param>
        /// <param name="loop">是否循环</param>
        /// <param name="fadeTime">淡入时间</param>
        void PlayMusic(AudioClip musicClip, bool loop = true, float fadeTime = 1f);
        
        /// <summary>
        /// 停止背景音乐
        /// </summary>
        /// <param name="fadeTime">淡出时间</param>
        void StopMusic(float fadeTime = 1f);

        #endregion

        #region Volume Control
        
        /// <summary>
        /// 设置主音量
        /// </summary>
        /// <param name="volume">音量 (0-1)</param>
        void SetMasterVolume(float volume);
        
        /// <summary>
        /// 设置音效音量
        /// </summary>
        /// <param name="volume">音量 (0-1)</param>
        void SetSFXVolume(float volume);
        
        /// <summary>
        /// 设置音乐音量
        /// </summary>
        /// <param name="volume">音量 (0-1)</param>
        void SetMusicVolume(float volume);
        
        /// <summary>
        /// 获取主音量
        /// </summary>
        /// <returns>当前主音量 (0-1)</returns>
        float GetMasterVolume();
        
        /// <summary>
        /// 获取音效音量
        /// </summary>
        /// <returns>当前音效音量 (0-1)</returns>
        float GetSFXVolume();
        
        /// <summary>
        /// 获取音乐音量
        /// </summary>
        /// <returns>当前音乐音量 (0-1)</returns>
        float GetMusicVolume();

        #endregion

        #region Audio Mixer Control
        
        /// <summary>
        /// 设置音频混合器参数
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="value">参数值</param>
        void SetMixerParameter(string parameterName, float value);
        
        /// <summary>
        /// 获取音频混合器参数
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <returns>参数值</returns>
        float GetMixerParameter(string parameterName);

        #endregion

        #region Utility
        
        /// <summary>
        /// 是否有音效正在播放
        /// </summary>
        /// <param name="clipType">音效类型</param>
        /// <returns>是否正在播放</returns>
        bool IsPlaying(AudioClipType clipType);
        
        /// <summary>
        /// 停止所有音效
        /// </summary>
        void StopAllSFX();
        
        /// <summary>
        /// 暂停所有音频
        /// </summary>
        void PauseAll();
        
        /// <summary>
        /// 恢复所有音频
        /// </summary>
        void ResumeAll();

        #endregion
    }

    /// <summary>
    /// 音效类型枚举
    /// 定义所有游戏中使用的音效类型
    /// </summary>
    public enum AudioClipType
    {
        // 射击音效
        GunFirePistol,
        GunFireRifle,
        GunReload,
        GunEmpty,
        GunCock,
        
        // 战斗音效
        EnemyHit,
        EnemyHitMetal,
        EnemyHitFlesh,
        EnemyDeath,
        EnemyDeathExplosion,
        EnemyAttack,
        PlayerHit,
        PlayerDeath,
        
        // 移动音效
        FootstepWalk,
        FootstepRun,
        FootstepStop,
        FootstepGrass,
        FootstepStone,
        FootstepMetal,
        
        // 交互音效
        ItemPickup,
        ItemDrop,
        DoorOpen,
        DoorClose,
        ButtonPress,
        
        // UI音效
        UIButtonClick,
        UIButtonHover,
        UIMenuOpen,
        UIMenuClose,
        UINotification,
        
        // 环境音效
        AmbientWind,
        AmbientRain,
        AmbientBirds,
        
        // 特殊音效
        PowerUp,
        LevelComplete,
        GameOver
    }

    /// <summary>
    /// 音频剪辑数据结构
    /// 用于配置音频剪辑的属性
    /// </summary>
    [System.Serializable]
    public class AudioClipData
    {
        [Header("Audio Clip")]
        public AudioClip clip;
        
        [Header("Volume Settings")]
        [Range(0f, 1f)]
        public float volume = 1f;
        
        [Header("Pitch Settings")]
        [Range(0.1f, 3f)]
        public float pitch = 1f;
        
        [Range(0f, 0.5f)]
        public float pitchVariation = 0f;
        
        [Header("3D Audio Settings")]
        [Range(0f, 1f)]
        public float spatialBlend = 1f; // 0 = 2D, 1 = 3D
        
        public float minDistance = 1f;
        public float maxDistance = 500f;
        
        [Header("Mixer Group")]
        public AudioMixerGroup mixerGroup;
        
        [Header("Playback Settings")]
        public bool loop = false;
        public int priority = 128; // 0 = highest priority, 256 = lowest
        
        /// <summary>
        /// 获取带有随机变化的音调
        /// </summary>
        /// <returns>随机音调值</returns>
        public float GetRandomPitch()
        {
            if (pitchVariation <= 0f) return pitch;
            
            float variation = Random.Range(-pitchVariation, pitchVariation);
            return Mathf.Clamp(pitch + variation, 0.1f, 3f);
        }
    }

    /// <summary>
    /// 音频剪辑字典条目
    /// 用于在Inspector中配置AudioClipType到AudioClipData的映射
    /// </summary>
    [System.Serializable]
    public class AudioClipEntry
    {
        [Header("Audio Configuration")]
        public AudioClipType clipType;
        public AudioClipData clipData;
    }
}
