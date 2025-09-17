using UnityEngine;
using Resonance.Utilities;
using Resonance.Interfaces.Services;

namespace Resonance.Environments
{
    public class SceneTransitionManager : MonoBehaviour
    {
        [Header("Transition Configuration")]
        [SerializeField] private TransitionEntry[] _transitionTriggers;
        [SerializeField] private bool _autoRegisterOnStart = true;
        
        private ISceneTransitionService _transitionService;
        private IPlayerService _playerService;
        
        [System.Serializable]
        public class TransitionEntry
        {
            public string transitionID;
            public SceneTransitionTrigger trigger;
            public string description;
        }
        
        void Start()
        {
            // 获取服务
            _transitionService = ServiceRegistry.Get<ISceneTransitionService>();
            _playerService = ServiceRegistry.Get<IPlayerService>();
            
            // 注册自己到SceneTransitionService
            // _transitionService?.RegisterSceneManager(this);
            
            // 注册所有TransitionTrigger
            RegisterTransitionTriggers();
            
            // 检查是否有pending transition
            CheckPendingTransition();
        }
        
        private void RegisterTransitionTriggers()
        {
            // 遍历并初始化所有TransitionTrigger
            foreach (var entry in _transitionTriggers)
            {
                if (entry?.trigger != null)
                {
                    entry.trigger.Initialize(entry.transitionID, this);
                }
            }
        }
        
        private void CheckPendingTransition()
        {
            // 如果有pending transition，协调PlayerSpawnManager完成spawn
            // if (_transitionService.HasPendingTransition)
            // {
            //     _transitionService.CompleteTransition();
            // }
        }
        
        // 被TransitionTrigger调用
        public void TriggerTransition(string transitionID, string targetScene, string targetSpawnPoint)
        {
            // _transitionService?.RequestTransition(targetScene, targetSpawnPoint, transitionID);
        }
    }
}

