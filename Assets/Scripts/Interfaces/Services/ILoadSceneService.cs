using System;
using UnityEngine.SceneManagement;

namespace Resonance.Interfaces.Services
{
    public interface ILoadSceneService : IGameService
    {
        event Action<string> OnSceneLoadStarted;
        event Action<string> OnSceneLoadCompleted;
        event Action<string> OnSceneUnloadStarted;
        event Action<string> OnSceneUnloadCompleted;

        string CurrentSceneName { get; }
        bool IsLoading { get; }
        
        void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single);
        void LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single);
        void UnloadSceneAsync(string sceneName);
        void SetActiveScene(string sceneName);
    }
}
