using UnityEngine;
using Resonance.Core;
using Resonance.Interfaces.Services;

namespace Resonance.Environments
{
    /// <summary>
    /// Manage scene transition triggers.
    /// Responsible for registering all TransitionTrigger in the scene, and handling pending scene transitions.
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        [Header("Transition Configuration")]
        [SerializeField] private TransitionEntry[] _transitionTriggers;
        [SerializeField] private bool _autoRegisterOnStart = true;
        [SerializeField] private bool _autoCheckPendingTransition = true;
        
        private ISceneTransitionService _transitionService;
        private IPlayerService _playerService;
        
        [System.Serializable]
        public class TransitionEntry
        {
            public string transitionID;
            public SceneTransitionTrigger trigger;
            [TextArea(2, 3)]
            public string description;
        }
        
        void Start()
        {
            // Get services
            _transitionService = ServiceRegistry.Get<ISceneTransitionService>();
            if (_transitionService == null)
            {
                Debug.LogError("SceneTransitionManager: SceneTransitionService not found!");
                return;
            }
            
            _playerService = ServiceRegistry.Get<IPlayerService>();
            if (_playerService == null)
            {
                Debug.LogError("SceneTransitionManager: PlayerService not found!");
                return;
            }
            
            // Register self to SceneTransitionService
            if (_autoRegisterOnStart)
            {
                _transitionService.RegisterSceneManager(this);
            }
            
            // Register all TransitionTrigger
            RegisterTransitionTriggers();
            
            // Check if there is a pending transition (delay one frame to ensure PlayerSpawnManager is initialized first)
            if (_autoCheckPendingTransition)
            {
                // Delay check, let PlayerSpawnManager complete spawn point registration first
                StartCoroutine(DelayedPendingTransitionCheck());
            }
            
            Debug.Log($"SceneTransitionManager: Initialized in scene {gameObject.scene.name}");
        }
        
        void OnDestroy()
        {
            // Unregister self
            if (_transitionService != null)
            {
                _transitionService.UnregisterSceneManager(this);
            }
        }
        
        private void RegisterTransitionTriggers()
        {
            if (_transitionTriggers == null || _transitionTriggers.Length == 0)
            {
                Debug.Log("SceneTransitionManager: No transition triggers to register");
                return;
            }
            
            int registeredCount = 0;
            
            // Iterate and initialize all TransitionTrigger
            foreach (var entry in _transitionTriggers)
            {
                if (entry?.trigger != null && !string.IsNullOrEmpty(entry.transitionID))
                {
                    entry.trigger.Initialize(entry.transitionID, this);
                    registeredCount++;
                    Debug.Log($"SceneTransitionManager: Registered transition trigger '{entry.transitionID}' - {entry.description}");
                }
                else
                {
                    Debug.LogWarning($"SceneTransitionManager: Invalid transition entry - trigger or ID is null");
                }
            }
            
            Debug.Log($"SceneTransitionManager: Registered {registeredCount} transition triggers");
        }
        
        private System.Collections.IEnumerator DelayedPendingTransitionCheck()
        {
            // Wait one frame, ensure PlayerSpawnManager is initialized
            yield return null;
            CheckPendingTransition();
        }
        
        private void CheckPendingTransition()
        {
            // If there is a pending transition, complete scene transition
            if (_transitionService != null && _transitionService.HasPendingTransition)
            {
                Debug.Log("SceneTransitionManager: Found pending transition, completing...");
                _transitionService.CompleteTransition();
            }
            else
            {
                Debug.Log("SceneTransitionManager: No pending transition found");
            }
        }
        
        /// <summary>
        /// Called by TransitionTrigger, trigger scene transition
        /// </summary>
        /// <param name="transitionID">Trigger ID</param>
        /// <param name="targetScene">Target scene</param>
        /// <param name="targetSpawnPoint">Target spawn point</param>
        public void TriggerTransition(string transitionID, string targetScene, string targetSpawnPoint)
        {
            if (_transitionService == null)
            {
                Debug.LogError("SceneTransitionManager: Cannot trigger transition - SceneTransitionService is null");
                return;
            }
            
            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogError($"SceneTransitionManager: Cannot trigger transition '{transitionID}' - target scene is empty");
                return;
            }
            
            Debug.Log($"SceneTransitionManager: Triggering transition '{transitionID}' to scene '{targetScene}', spawn point '{targetSpawnPoint}'");
            _transitionService.RequestTransition(targetScene, targetSpawnPoint, transitionID);
        }
        
        // Public method for external call
        public void TriggerTransitionByID(string transitionID)
        {
            var entry = GetTransitionEntry(transitionID);
            if (entry != null && entry.trigger != null)
            {
                TriggerTransition(transitionID, entry.trigger.TargetSceneName, entry.trigger.TargetSpawnPointID);
            }
            else
            {
                Debug.LogError($"SceneTransitionManager: Transition '{transitionID}' not found");
            }
        }
        
        private TransitionEntry GetTransitionEntry(string transitionID)
        {
            if (_transitionTriggers != null)
            {
                foreach (var entry in _transitionTriggers)
                {
                    if (entry?.transitionID == transitionID)
                    {
                        return entry;
                    }
                }
            }
            return null;
        }
        
        #region Editor Support
        
        void OnDrawGizmosSelected()
        {
            // Draw all transition trigger positions
            if (_transitionTriggers != null)
            {
                foreach (var entry in _transitionTriggers)
                {
                    if (entry?.trigger != null)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireCube(entry.trigger.transform.position, Vector3.one);
                        Gizmos.DrawRay(entry.trigger.transform.position, Vector3.up * 2f);
                    }
                }
            }
        }
        
        void OnDrawGizmos()
        {
            // Always show transition triggers
            if (_transitionTriggers != null)
            {
                foreach (var entry in _transitionTriggers)
                {
                    if (entry?.trigger != null)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawWireCube(entry.trigger.transform.position + Vector3.up * 0.1f, Vector3.one * 0.5f);
                    }
                }
            }
        }
        
        #endregion
    }
}

