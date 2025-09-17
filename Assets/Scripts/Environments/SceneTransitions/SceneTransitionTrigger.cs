using UnityEngine;
using Resonance.Player;

namespace Resonance.Environments
{
    public class SceneTransitionTrigger : MonoBehaviour
    {
        [Header("Transition Configuration")]
        [SerializeField] private string _targetSceneName;
        [SerializeField] private string _targetSpawnPointID;
        [SerializeField] private string _transitionID; 
        
        [Header("Transition Settings")]
        [SerializeField] private bool _savePlayerStateOnTransition = true;
        [SerializeField] private float _transitionDelay = 0f;
        
        // 当Player进入触发器时调用
        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<PlayerMonoBehaviour>();
            if (player != null)
            {
                // TriggerSceneTransition(player);
            }
        }
    }
}