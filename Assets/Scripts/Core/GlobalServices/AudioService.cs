using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Resonance.Core;
using Resonance.Interfaces.Services;

namespace Resonance.Core.GlobalServices
{
    /// <summary>
    /// Global audio service
    /// Manage all audio playback, volume control, and audio mixer in the game
    /// </summary>
    public class AudioService : IAudioService
    {
        #region IGameService Properties
        
        public int Priority => 25; // After PlayerService, because it may need player position information
        public SystemState State { get; private set; } = SystemState.Uninitialized;

        #endregion

        #region Private Fields
        
        private ServiceConfiguration _configuration;
        private AudioMixer _audioMixer;
        private GameObject _audioManager;
        private AudioSource _musicAudioSource;
        
        // Audio source pooling
        private Queue<AudioSource> _availableAudioSources = new Queue<AudioSource>();
        private List<AudioSource> _usedAudioSources = new List<AudioSource>();
        private Dictionary<AudioClipType, List<AudioSource>> _playingClips = new Dictionary<AudioClipType, List<AudioSource>>();
        
        // Volume settings
        private float _masterVolume = 1f;
        private float _sfxVolume = 1f;
        private float _musicVolume = 0.7f;
        
        // Music fade in/out
        private Coroutine _musicFadeCoroutine;
        
        // MonoBehaviour reference for coroutines
        private MonoBehaviour _coroutineRunner;

        #endregion

        #region Constructor and Initialization
        
        public AudioService(ServiceConfiguration configuration, MonoBehaviour coroutineRunner)
        {
            _configuration = configuration;
            _coroutineRunner = coroutineRunner;
        }

        public void Initialize()
        {
            if (State != SystemState.Uninitialized)
            {
                Debug.LogWarning("AudioService already initialized");
                return;
            }

            State = SystemState.Initializing;
            Debug.Log("AudioService: Initializing");

            // Verify configuration
            if (_configuration == null)
            {
                Debug.LogError("AudioService: ServiceConfiguration is null");
                State = SystemState.Shutdown;
                return;
            }

            // Set audio mixer
            _audioMixer = _configuration.audioMixer;
            if (_audioMixer == null)
            {
                Debug.LogWarning("AudioService: AudioMixer is null in configuration");
            }

            // Create audio manager GameObject
            CreateAudioManager();
            
            // Initialize audio source pool
            InitializeAudioSourcePool();
            
            // Set default volumes
            SetDefaultVolumes();
            
            // Initialize playing clips dictionary
            InitializePlayingClipsDictionary();

            State = SystemState.Running;
            Debug.Log("AudioService: Initialized successfully");
        }

        public void Shutdown()
        {
            if (State == SystemState.Shutdown) return;

            Debug.Log("AudioService: Shutting down");

            // Stop all audio
            StopAllSFX();
            StopMusic(0f);

            // Stop all coroutines
            if (_musicFadeCoroutine != null)
            {
                _coroutineRunner.StopCoroutine(_musicFadeCoroutine);
                _musicFadeCoroutine = null;
            }

            // Cleanup audio source pool
            CleanupAudioSourcePool();

            // Destroy audio manager
            if (_audioManager != null)
            {
                Object.Destroy(_audioManager);
                _audioManager = null;
            }

            State = SystemState.Shutdown;
            Debug.Log("AudioService: Shutdown complete");
        }

        private void CreateAudioManager()
        {
            _audioManager = new GameObject("AudioManager");
            Object.DontDestroyOnLoad(_audioManager);

            // Create music audio source
            _musicAudioSource = _audioManager.AddComponent<AudioSource>();
            _musicAudioSource.loop = true;
            _musicAudioSource.playOnAwake = false;
            _musicAudioSource.outputAudioMixerGroup = _configuration.musicMixerGroup;

            Debug.Log("AudioService: Audio manager created");
        }

        private void InitializeAudioSourcePool()
        {
            if (!_configuration.enableAudioSourcePooling) return;

            int poolSize = _configuration.audioSourcePoolSize;
            for (int i = 0; i < poolSize; i++)
            {
                AudioSource audioSource = _audioManager.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.enabled = false;
                _availableAudioSources.Enqueue(audioSource);
            }

            Debug.Log($"AudioService: Initialized audio source pool with {poolSize} sources");
        }

        private void SetDefaultVolumes()
        {
            _masterVolume = _configuration.defaultMasterVolume;
            _sfxVolume = _configuration.defaultSFXVolume;
            _musicVolume = _configuration.defaultMusicVolume;

            // Apply to mixer
            SetMasterVolume(_masterVolume);
            SetSFXVolume(_sfxVolume);
            SetMusicVolume(_musicVolume);
        }

        private void InitializePlayingClipsDictionary()
        {
            _playingClips.Clear();
            
            // Initialize list for each clip type
            var clipTypes = System.Enum.GetValues(typeof(AudioClipType));
            foreach (AudioClipType clipType in clipTypes)
            {
                _playingClips[clipType] = new List<AudioSource>();
            }
        }

        #endregion

        #region Audio Playback

        public AudioSource PlaySFX3D(AudioClipType clipType, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            AudioClipData clipData = _configuration.GetAudioClipData(clipType);
            if (clipData == null || clipData.clip == null)
            {
                Debug.LogWarning($"AudioService: No audio clip found for {clipType}");
                return null;
            }

            AudioSource audioSource = GetAudioSource();
            if (audioSource == null)
            {
                Debug.LogWarning("AudioService: No available audio source");
                return null;
            }

            ConfigureAudioSource(audioSource, clipData, clipType, volume, pitch, position, true);
            audioSource.Play();

            TrackPlayingClip(clipType, audioSource);

            Debug.Log($"AudioService: Playing 3D SFX {clipType} at position {position}");
            return audioSource;
        }

        public AudioSource PlaySFX2D(AudioClipType clipType, float volume = 1f, float pitch = 1f)
        {
            AudioClipData clipData = _configuration.GetAudioClipData(clipType);
            if (clipData == null || clipData.clip == null)
            {
                Debug.LogWarning($"AudioService: No audio clip found for {clipType}");
                return null;
            }

            AudioSource audioSource = GetAudioSource();
            if (audioSource == null)
            {
                Debug.LogWarning("AudioService: No available audio source");
                return null;
            }

            ConfigureAudioSource(audioSource, clipData, clipType, volume, pitch, Vector3.zero, false);
            audioSource.Play();

            TrackPlayingClip(clipType, audioSource);

            Debug.Log($"AudioService: Playing 2D SFX {clipType}");
            return audioSource;
        }

        public AudioSource PlayLoopingSFX(AudioClipType clipType, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            AudioClipData clipData = _configuration.GetAudioClipData(clipType);
            if (clipData == null || clipData.clip == null)
            {
                Debug.LogWarning($"AudioService: No audio clip found for {clipType}");
                return null;
            }

            AudioSource audioSource = GetAudioSource();
            if (audioSource == null)
            {
                Debug.LogWarning("AudioService: No available audio source");
                return null;
            }

            ConfigureAudioSource(audioSource, clipData, clipType, volume, pitch, position, true);
            audioSource.loop = true;
            audioSource.Play();

            TrackPlayingClip(clipType, audioSource);

            Debug.Log($"AudioService: Playing looping SFX {clipType} at position {position}");
            return audioSource;
        }

        public void StopLoopingSFX(AudioSource audioSource)
        {
            if (audioSource == null) return;

            audioSource.Stop();
            ReturnAudioSource(audioSource);

            Debug.Log("AudioService: Stopped looping SFX");
        }

        public void PlayMusic(AudioClip musicClip, bool loop = true, float fadeTime = 1f)
        {
            if (musicClip == null)
            {
                Debug.LogWarning("AudioService: Music clip is null");
                return;
            }

            if (_musicFadeCoroutine != null)
            {
                _coroutineRunner.StopCoroutine(_musicFadeCoroutine);
            }

            _musicFadeCoroutine = _coroutineRunner.StartCoroutine(PlayMusicWithFade(musicClip, loop, fadeTime));
        }

        public void StopMusic(float fadeTime = 1f)
        {
            if (_musicFadeCoroutine != null)
            {
                _coroutineRunner.StopCoroutine(_musicFadeCoroutine);
            }

            _musicFadeCoroutine = _coroutineRunner.StartCoroutine(StopMusicWithFade(fadeTime));
        }

        #endregion

        #region Volume Control

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            if (_audioMixer != null)
            {
                // Convert linear volume to decibels (-80dB to 0dB)
                float dbValue = _masterVolume > 0 ? Mathf.Log10(_masterVolume) * 20f : -80f;
                _audioMixer.SetFloat("MasterVolume", dbValue);
            }
            Debug.Log($"AudioService: Set master volume to {_masterVolume:F2}");
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            if (_audioMixer != null)
            {
                float dbValue = _sfxVolume > 0 ? Mathf.Log10(_sfxVolume) * 20f : -80f;
                _audioMixer.SetFloat("SFXVolume", dbValue);
            }
            Debug.Log($"AudioService: Set SFX volume to {_sfxVolume:F2}");
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            if (_audioMixer != null)
            {
                float dbValue = _musicVolume > 0 ? Mathf.Log10(_musicVolume) * 20f : -80f;
                _audioMixer.SetFloat("MusicVolume", dbValue);
            }
            Debug.Log($"AudioService: Set music volume to {_musicVolume:F2}");
        }

        public float GetMasterVolume() => _masterVolume;
        public float GetSFXVolume() => _sfxVolume;
        public float GetMusicVolume() => _musicVolume;

        #endregion

        #region Audio Mixer Control

        public void SetMixerParameter(string parameterName, float value)
        {
            if (_audioMixer != null)
            {
                _audioMixer.SetFloat(parameterName, value);
                Debug.Log($"AudioService: Set mixer parameter {parameterName} to {value}");
            }
        }

        public float GetMixerParameter(string parameterName)
        {
            if (_audioMixer != null && _audioMixer.GetFloat(parameterName, out float value))
            {
                return value;
            }
            return 0f;
        }

        #endregion

        #region Utility

        public bool IsPlaying(AudioClipType clipType)
        {
            if (_playingClips.ContainsKey(clipType))
            {
                var sources = _playingClips[clipType];
                for (int i = sources.Count - 1; i >= 0; i--)
                {
                    if (sources[i] != null && sources[i].isPlaying)
                    {
                        return true;
                    }
                    else
                    {
                        // Cleanup stopped audio sources
                        sources.RemoveAt(i);
                    }
                }
            }
            return false;
        }

        public void StopAllSFX()
        {
            foreach (var audioSource in _usedAudioSources)
            {
                if (audioSource != null && audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
            }

            // Cleanup playing list
            foreach (var clipType in _playingClips.Keys)
            {
                _playingClips[clipType].Clear();
            }

            Debug.Log("AudioService: Stopped all SFX");
        }

        public void PauseAll()
        {
            foreach (var audioSource in _usedAudioSources)
            {
                if (audioSource != null && audioSource.isPlaying)
                {
                    audioSource.Pause();
                }
            }

            if (_musicAudioSource != null && _musicAudioSource.isPlaying)
            {
                _musicAudioSource.Pause();
            }

            Debug.Log("AudioService: Paused all audio");
        }

        public void ResumeAll()
        {
            foreach (var audioSource in _usedAudioSources)
            {
                if (audioSource != null)
                {
                    audioSource.UnPause();
                }
            }

            if (_musicAudioSource != null)
            {
                _musicAudioSource.UnPause();
            }

            Debug.Log("AudioService: Resumed all audio");
        }

        #endregion

        #region Private Helper Methods

        private AudioSource GetAudioSource()
        {
            AudioSource audioSource = null;

            if (_configuration.enableAudioSourcePooling && _availableAudioSources.Count > 0)
            {
                audioSource = _availableAudioSources.Dequeue();
            }
            else
            {
                // Create new audio source
                audioSource = _audioManager.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            if (audioSource != null)
            {
                audioSource.enabled = true;
                _usedAudioSources.Add(audioSource);
            }

            return audioSource;
        }

        private void ReturnAudioSource(AudioSource audioSource)
        {
            if (audioSource == null) return;

            audioSource.Stop();
            audioSource.clip = null;
            audioSource.loop = false;
            audioSource.enabled = false;

            _usedAudioSources.Remove(audioSource);

            if (_configuration.enableAudioSourcePooling)
            {
                _availableAudioSources.Enqueue(audioSource);
            }
            else
            {
                Object.Destroy(audioSource);
            }

            // Remove from playing list
            foreach (var clipType in _playingClips.Keys)
            {
                _playingClips[clipType].Remove(audioSource);
            }
        }

        private void ConfigureAudioSource(AudioSource audioSource, AudioClipData clipData, AudioClipType clipType, float volume, float pitch, Vector3 position, bool is3D)
        {
            audioSource.clip = clipData.clip;
            audioSource.volume = clipData.volume * volume;
            audioSource.pitch = pitch > 0 ? pitch * clipData.GetRandomPitch() : clipData.GetRandomPitch();
            audioSource.loop = clipData.loop;
            audioSource.priority = clipData.priority;

            // Set mixer group
            // If clipData specifies mixerGroup, use it, otherwise select automatically based on clipType
            AudioMixerGroup mixerGroup = clipData.mixerGroup;
            if (mixerGroup == null)
            {
                mixerGroup = _configuration.GetMixerGroupForClipType(clipType);
            }
            audioSource.outputAudioMixerGroup = mixerGroup;

            if (is3D)
            {
                audioSource.spatialBlend = clipData.spatialBlend;
                audioSource.minDistance = clipData.minDistance;
                audioSource.maxDistance = clipData.maxDistance;
                audioSource.transform.position = position;
            }
            else
            {
                audioSource.spatialBlend = 0f; // 2D
            }
        }

        private void TrackPlayingClip(AudioClipType clipType, AudioSource audioSource)
        {
            if (_playingClips.ContainsKey(clipType))
            {
                _playingClips[clipType].Add(audioSource);
            }

            // Start coroutine to track audio completion
            _coroutineRunner.StartCoroutine(TrackAudioCompletion(audioSource));
        }

        private IEnumerator TrackAudioCompletion(AudioSource audioSource)
        {
            while (audioSource != null && audioSource.isPlaying)
            {
                yield return null;
            }

            if (audioSource != null)
            {
                ReturnAudioSource(audioSource);
            }
        }

        private IEnumerator PlayMusicWithFade(AudioClip musicClip, bool loop, float fadeTime)
        {
            // Fade out current music
            if (_musicAudioSource.isPlaying)
            {
                float startVolume = _musicAudioSource.volume;
                for (float t = 0; t < fadeTime; t += Time.deltaTime)
                {
                    _musicAudioSource.volume = Mathf.Lerp(startVolume, 0, t / fadeTime);
                    yield return null;
                }
            }

            // Set new music
            _musicAudioSource.clip = musicClip;
            _musicAudioSource.loop = loop;
            _musicAudioSource.volume = 0;
            _musicAudioSource.Play();

            // Fade in new music
            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                _musicAudioSource.volume = Mathf.Lerp(0, _musicVolume, t / fadeTime);
                yield return null;
            }

            _musicAudioSource.volume = _musicVolume;
            _musicFadeCoroutine = null;

            Debug.Log($"AudioService: Music {musicClip.name} started with fade");
        }

        private IEnumerator StopMusicWithFade(float fadeTime)
        {
            if (!_musicAudioSource.isPlaying) yield break;

            float startVolume = _musicAudioSource.volume;
            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                _musicAudioSource.volume = Mathf.Lerp(startVolume, 0, t / fadeTime);
                yield return null;
            }

            _musicAudioSource.Stop();
            _musicAudioSource.volume = _musicVolume;
            _musicFadeCoroutine = null;

            Debug.Log("AudioService: Music stopped with fade");
        }

        private void CleanupAudioSourcePool()
        {
            _availableAudioSources.Clear();
            _usedAudioSources.Clear();
            _playingClips.Clear();
        }

        #endregion
    }
}
