using UnityEngine;
using Resonance.Gameplay.Player;

namespace Resonance.Environments
{
    /// <summary>
    /// 场景切换触发器。当Player进入时触发场景切换。
    /// </summary>
    public class SceneTransitionTrigger : MonoBehaviour
    {
        [Header("Transition Configuration")]
        [SerializeField] private string _targetSceneName;
        [SerializeField] private string _targetSpawnPointID = "default";
        
        [Header("Trigger Settings")]
        [SerializeField] private bool _triggerOnce = true;
        [SerializeField] private LayerMask _playerLayerMask = -1;
        
        private string _transitionID;
        private SceneTransitionManager _manager;
        private bool _hasTriggered = false;
        
        // 公共属性供Manager访问
        public string TargetSceneName => _targetSceneName;
        public string TargetSpawnPointID => _targetSpawnPointID;
        public string TransitionID => _transitionID;
        
        public void Initialize(string transitionID, SceneTransitionManager manager)
        {
            _transitionID = transitionID;
            _manager = manager;
            
            Debug.Log($"SceneTransitionTrigger: Initialized '{transitionID}' -> {_targetSceneName}:{_targetSpawnPointID}");
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // 检查是否已经触发过
            if (_triggerOnce && _hasTriggered)
            {
                return;
            }
            
            // 检查Layer
            if ((_playerLayerMask.value & (1 << other.gameObject.layer)) == 0)
            {
                return;
            }
            
            // 检查是否是Player
            var player = other.GetComponent<PlayerMonoBehaviour>();
            if (player != null && _manager != null)
            {
                if (string.IsNullOrEmpty(_targetSceneName))
                {
                    Debug.LogError($"SceneTransitionTrigger '{_transitionID}': Target scene name is empty!");
                    return;
                }
                
                Debug.Log($"SceneTransitionTrigger: Player entered '{_transitionID}', triggering transition to {_targetSceneName}:{_targetSpawnPointID}");
                
                _manager.TriggerTransition(_transitionID, _targetSceneName, _targetSpawnPointID);
                
                if (_triggerOnce)
                {
                    _hasTriggered = true;
                }
            }
        }
        
        /// <summary>
        /// 重置触发状态(用于调试或特殊情况)
        /// </summary>
        public void ResetTrigger()
        {
            _hasTriggered = false;
            Debug.Log($"SceneTransitionTrigger: Reset trigger state for '{_transitionID}'");
        }
        
        /// <summary>
        /// 手动触发场景切换
        /// </summary>
        public void ManualTrigger()
        {
            if (_manager != null && !string.IsNullOrEmpty(_targetSceneName))
            {
                Debug.Log($"SceneTransitionTrigger: Manual trigger for '{_transitionID}'");
                _manager.TriggerTransition(_transitionID, _targetSceneName, _targetSpawnPointID);
                
                if (_triggerOnce)
                {
                    _hasTriggered = true;
                }
            }
        }
    }
}