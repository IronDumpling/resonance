using UnityEngine;
using Resonance.Player;

namespace Resonance.Environments
{
    public class SceneTransitionTrigger : MonoBehaviour
    {
        [Header("Transition Configuration")]
        [SerializeField] private string _targetSceneName;
        [SerializeField] private string _targetSpawnPointID;
        
        private string _transitionID;
        private SceneTransitionManager _manager;
        
        public void Initialize(string transitionID, SceneTransitionManager manager)
        {
            _transitionID = transitionID;
            _manager = manager;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<PlayerMonoBehaviour>();
            if (player != null && _manager != null)
            {
                _manager.TriggerTransition(_transitionID, _targetSceneName, _targetSpawnPointID);
            }
        }
    }
}