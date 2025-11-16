using System;
using UnityEngine.SceneManagement;
using Resonance.Environments;

namespace Resonance.Shared.Interfaces.Services
{
    public interface ISceneTransitionService : IGameService
    {
        // Scene Load Events
        event Action<string> OnSceneLoadStarted;
        event Action<string> OnSceneLoadCompleted;
        event Action<string> OnSceneUnloadStarted;
        event Action<string> OnSceneUnloadCompleted;
        
        // Scene Transition Events
        event Action<string, string> OnTransitionRequested; // sceneName, spawnPointID
        event Action<string> OnTransitionCompleted; // sceneName

        // Scene Load Properties
        string CurrentSceneName { get; }
        bool IsLoading { get; }
        
        // Scene Transition Properties
        bool HasPendingTransition { get; }
        
        // Scene Load Methods
        void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single);
        void LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single);
        void UnloadSceneAsync(string sceneName);
        void SetActiveScene(string sceneName);
        
        // Scene Transition Methods
        void RequestTransition(string targetScene, string spawnPointID, string transitionID);
        void CompleteTransition();
        void RegisterSceneManager(SceneTransitionManager manager);
        void UnregisterSceneManager(SceneTransitionManager manager);
    }
}
