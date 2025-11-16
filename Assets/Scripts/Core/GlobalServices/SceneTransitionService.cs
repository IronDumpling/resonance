using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Resonance.Core;
using Resonance.Core.StateMachine;
using Resonance.Core.StateMachine.States;
using Resonance.Utilities;
using Resonance.Shared.Interfaces.Services;
using Resonance.Environments;

namespace Resonance.Core.GlobalServices
{
    public class SceneTransitionService : ISceneTransitionService
    {
        public int Priority => 5;
        public SystemState State { get; private set; } = SystemState.Uninitialized;
        
        // Scene Load Events
        public event Action<string> OnSceneLoadStarted;
        public event Action<string> OnSceneLoadCompleted;
        public event Action<string> OnSceneUnloadStarted;
        public event Action<string> OnSceneUnloadCompleted;
        
        // Scene Transition Events
        public event Action<string, string> OnTransitionRequested; // sceneName, spawnPointID
        public event Action<string> OnTransitionCompleted; // sceneName

        // Scene Load Properties
        public string CurrentSceneName { get; private set; }
        public bool IsLoading { get; private set; }
        
        // Scene Transition Properties
        public bool HasPendingTransition => _pendingTransition.HasValue;

        private MonoBehaviour _coroutineRunner;
        
        // Scene Transition State Management
        private struct PendingTransition
        {
            public string targetSceneName;
            public string targetSpawnPointID;
            public string sourceTransitionID;
        }
        
        private PendingTransition? _pendingTransition;
        private Dictionary<string, SceneTransitionManager> _sceneManagers = new Dictionary<string, SceneTransitionManager>();

        public SceneTransitionService(MonoBehaviour coroutineRunner)
        {
            _coroutineRunner = coroutineRunner;
            CurrentSceneName = SceneManager.GetActiveScene().name;
        }

        public void Initialize()
        {
            if (State != SystemState.Uninitialized)
            {
                Debug.LogWarning("SceneTransitionService already initialized");
                return;
            }

            State = SystemState.Initializing;
            Debug.Log("SceneTransitionService: Initializing");

            // Subscribe to Unity scene events
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            State = SystemState.Running;
            Debug.Log("SceneTransitionService: Initialized successfully");
        }

        public void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"SceneTransitionService: Cannot load {sceneName}, already loading a scene");
                return;
            }

            Debug.Log($"SceneTransitionService: Loading scene {sceneName}");
            OnSceneLoadStarted?.Invoke(sceneName);
            
            SceneManager.LoadScene(sceneName, mode);
        }

        public void LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"SceneTransitionService: Cannot load {sceneName}, already loading a scene");
                return;
            }

            _coroutineRunner.StartCoroutine(LoadSceneAsyncCoroutine(sceneName, mode));
        }

        private IEnumerator LoadSceneAsyncCoroutine(string sceneName, LoadSceneMode mode)
        {
            IsLoading = true;
            Debug.Log($"SceneTransitionService: Loading scene {sceneName} asynchronously");
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
                Debug.LogWarning($"SceneTransitionService: Cannot unload {sceneName}, currently loading a scene");
                return;
            }

            _coroutineRunner.StartCoroutine(UnloadSceneAsyncCoroutine(sceneName));
        }

        private IEnumerator UnloadSceneAsyncCoroutine(string sceneName)
        {
            Debug.Log($"SceneTransitionService: Unloading scene {sceneName}");
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
                Debug.Log($"SceneTransitionService: Set active scene to {sceneName}");
            }
            else
            {
                Debug.LogError($"SceneTransitionService: Cannot set active scene {sceneName}, scene is not loaded");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CurrentSceneName = scene.name;
            Debug.Log($"SceneTransitionService: Scene {scene.name} loaded");
            OnSceneLoadCompleted?.Invoke(scene.name);
            IsLoading = false;
            
            // Trigger UI refresh for gameplay scenes after a short delay
            // This ensures all UI panels are registered before we try to show them
            if (scene.name.Contains("Level") || scene.name.Contains("Room") || scene.name.Contains("Test"))
            {
                Debug.Log($"SceneTransitionService: Scheduling UI refresh for gameplay scene {scene.name}");
                _coroutineRunner.StartCoroutine(DelayedUIRefresh());
            }
        }
        
        private IEnumerator DelayedUIRefresh()
        {
            // Wait a few frames to ensure all UI panels are registered
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            
            // Get GameManager and refresh gameplay UI
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                var stateMachine = gameManager.GetComponent<GameStateMachine>();
                if (stateMachine != null)
                {
                    var gameplayState = stateMachine.GetState<GameplayState>("Gameplay");
                    if (gameplayState != null)
                    {
                        Debug.Log("SceneTransitionService: Triggering gameplay UI refresh");
                        gameplayState.RefreshGameplayUI();
                    }
                    else
                    {
                        Debug.LogWarning("SceneTransitionService: GameplayState not found in state machine");
                    }
                }
                else
                {
                    Debug.LogWarning("SceneTransitionService: GameStateMachine not found on GameManager");
                }
            }
            else
            {
                Debug.LogWarning("SceneTransitionService: GameManager instance not found");
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            Debug.Log($"SceneTransitionService: Scene {scene.name} unloaded");
            OnSceneUnloadCompleted?.Invoke(scene.name);
        }

        #region Scene Transition Methods
        
        /// <summary>
        /// Request scene transition
        /// </summary>
        /// <param name="targetScene">Target scene name</param>
        /// <param name="spawnPointID">Target spawn point ID</param>
        /// <param name="transitionID">Transition trigger ID</param>
        public void RequestTransition(string targetScene, string spawnPointID, string transitionID)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"SceneTransitionService: Cannot start transition to {targetScene}, already loading a scene");
                return;
            }
            
            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogError("SceneTransitionService: Target scene name cannot be empty");
                return;
            }

            Debug.Log($"SceneTransitionService: Requesting transition to {targetScene}, spawn point: {spawnPointID}");
            
            // Save current Player state to PlayerService
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService != null && playerService.HasPlayer)
            {
                playerService.SavePlayerState($"transition_{transitionID}");
                Debug.Log("SceneTransitionService: Player state saved for transition");
            }
            else
            {
                Debug.LogWarning("SceneTransitionService: No player service or player found, skipping save");
            }
            
            // Record pending transition
            _pendingTransition = new PendingTransition
            {
                targetSceneName = targetScene,
                targetSpawnPointID = spawnPointID,
                sourceTransitionID = transitionID
            };
            
            // Trigger event
            OnTransitionRequested?.Invoke(targetScene, spawnPointID);
            
            // Start scene loading
            Debug.Log($"SceneTransitionService: Starting scene load...");
            LoadScene(targetScene);
        }
        
        /// <summary>
        /// Complete scene transition (called in new scene)
        /// </summary>
        public void CompleteTransition()
        {
            if (!_pendingTransition.HasValue)
            {
                Debug.Log("SceneTransitionService: No pending transition to complete");
                return;
            }
            
            var transition = _pendingTransition.Value;
            Debug.Log($"SceneTransitionService: Completing transition to {transition.targetSceneName}, spawn point: {transition.targetSpawnPointID}");
            
            // Trigger Player spawn via PlayerService
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService != null)
            {
                if (!string.IsNullOrEmpty(transition.targetSpawnPointID))
                {
                    playerService.SpawnPlayerAtPoint(transition.targetSpawnPointID);
                }
                else
                {
                    Debug.LogWarning("SceneTransitionService: No spawn point specified, using default spawn");
                    playerService.SpawnPlayerAtPoint("default");
                }
            }
            else
            {
                Debug.LogError("SceneTransitionService: PlayerService not found, cannot spawn player");
            }
            
            // Trigger completion event
            OnTransitionCompleted?.Invoke(transition.targetSceneName);
            
            // Clear pending transition
            _pendingTransition = null;
            
            Debug.Log("SceneTransitionService: Transition completed successfully");
        }
        
        /// <summary>
        /// Register scene manager
        /// </summary>
        public void RegisterSceneManager(SceneTransitionManager manager)
        {
            if (manager == null) return;
            
            string sceneName = manager.gameObject.scene.name;
            _sceneManagers[sceneName] = manager;
            Debug.Log($"SceneTransitionService: Registered SceneTransitionManager for scene {sceneName}");
        }
        
        /// <summary>
        /// Unregister scene manager
        /// </summary>
        public void UnregisterSceneManager(SceneTransitionManager manager)
        {
            if (manager == null) return;
            
            string sceneName = manager.gameObject.scene.name;
            if (_sceneManagers.ContainsKey(sceneName))
            {
                _sceneManagers.Remove(sceneName);
                Debug.Log($"SceneTransitionService: Unregistered SceneTransitionManager for scene {sceneName}");
            }
        }
        
        #endregion

        public void Shutdown()
        {
            if (State == SystemState.Shutdown)
                return;

            Debug.Log("SceneTransitionService: Shutting down");

            // Unsubscribe from Unity scene events
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            // Clear scene transition data
            _pendingTransition = null;
            _sceneManagers.Clear();

            // Clear events
            OnSceneLoadStarted = null;
            OnSceneLoadCompleted = null;
            OnSceneUnloadStarted = null;
            OnSceneUnloadCompleted = null;
            OnTransitionRequested = null;
            OnTransitionCompleted = null;

            State = SystemState.Shutdown;
        }
    }
}
