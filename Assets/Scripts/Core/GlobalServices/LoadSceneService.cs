using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Resonance.Core;
using Resonance.Interfaces.Services;

namespace Resonance.Core.GlobalServices
{
    public class LoadSceneService : ILoadSceneService
    {
        public int Priority => 5;
        public SystemState State { get; private set; } = SystemState.Uninitialized;
        
        public event Action<string> OnSceneLoadStarted;
        public event Action<string> OnSceneLoadCompleted;
        public event Action<string> OnSceneUnloadStarted;
        public event Action<string> OnSceneUnloadCompleted;

        public string CurrentSceneName { get; private set; }
        public bool IsLoading { get; private set; }

        private MonoBehaviour _coroutineRunner;

        public LoadSceneService(MonoBehaviour coroutineRunner)
        {
            _coroutineRunner = coroutineRunner;
            CurrentSceneName = SceneManager.GetActiveScene().name;
        }

        public void Initialize()
        {
            if (State != SystemState.Uninitialized)
            {
                Debug.LogWarning("LoadSceneService already initialized");
                return;
            }

            State = SystemState.Initializing;
            Debug.Log("LoadSceneService: Initializing");

            // Subscribe to Unity scene events
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            State = SystemState.Running;
            Debug.Log("LoadSceneService: Initialized successfully");
        }

        public void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"LoadSceneService: Cannot load {sceneName}, already loading a scene");
                return;
            }

            Debug.Log($"LoadSceneService: Loading scene {sceneName}");
            OnSceneLoadStarted?.Invoke(sceneName);
            
            SceneManager.LoadScene(sceneName, mode);
        }

        public void LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"LoadSceneService: Cannot load {sceneName}, already loading a scene");
                return;
            }

            _coroutineRunner.StartCoroutine(LoadSceneAsyncCoroutine(sceneName, mode));
        }

        private IEnumerator LoadSceneAsyncCoroutine(string sceneName, LoadSceneMode mode)
        {
            IsLoading = true;
            Debug.Log($"LoadSceneService: Loading scene {sceneName} asynchronously");
            OnSceneLoadStarted?.Invoke(sceneName);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, mode);
            
            while (!operation.isDone)
            {
                yield return null;
            }

            IsLoading = false;
        }

        public void UnloadSceneAsync(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"LoadSceneService: Cannot unload {sceneName}, currently loading a scene");
                return;
            }

            _coroutineRunner.StartCoroutine(UnloadSceneAsyncCoroutine(sceneName));
        }

        private IEnumerator UnloadSceneAsyncCoroutine(string sceneName)
        {
            Debug.Log($"LoadSceneService: Unloading scene {sceneName}");
            OnSceneUnloadStarted?.Invoke(sceneName);

            AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);
            
            while (!operation.isDone)
            {
                yield return null;
            }
        }

        public void SetActiveScene(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
            {
                SceneManager.SetActiveScene(scene);
                CurrentSceneName = sceneName;
                Debug.Log($"LoadSceneService: Set active scene to {sceneName}");
            }
            else
            {
                Debug.LogError($"LoadSceneService: Cannot set active scene {sceneName}, scene is not loaded");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CurrentSceneName = scene.name;
            Debug.Log($"LoadSceneService: Scene {scene.name} loaded");
            OnSceneLoadCompleted?.Invoke(scene.name);
            IsLoading = false;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            Debug.Log($"LoadSceneService: Scene {scene.name} unloaded");
            OnSceneUnloadCompleted?.Invoke(scene.name);
        }

        public void Shutdown()
        {
            if (State == SystemState.Shutdown)
                return;

            Debug.Log("LoadSceneService: Shutting down");

            // Unsubscribe from Unity scene events
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            // Clear events
            OnSceneLoadStarted = null;
            OnSceneLoadCompleted = null;
            OnSceneUnloadStarted = null;
            OnSceneUnloadCompleted = null;

            State = SystemState.Shutdown;
        }
    }
}
