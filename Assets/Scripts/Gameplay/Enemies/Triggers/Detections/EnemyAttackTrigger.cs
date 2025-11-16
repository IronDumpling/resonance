using UnityEngine;

namespace Resonance.Gameplay.Enemies.Triggers
{
    /// <summary>
    /// EnemyAttackTrigger:
    /// This component is used to identify and handle attack range trigger events.
    /// It is no longer used for detection range (detection is now handled by EnemyVision system).
    /// </summary>
    public class EnemyAttackTrigger : MonoBehaviour
    {
        private EnemyMonoBehaviour _enemyMono;
        private bool _isInitialized = false;

        /// <summary>
        /// Initialize the attack trigger
        /// </summary>
        /// <param name="enemyMono">EnemyMonoBehaviour reference</param>
        public void Initialize(EnemyMonoBehaviour enemyMono)
        {
            _enemyMono = enemyMono;
            _isInitialized = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!_isInitialized || _enemyMono == null)
            {
                Debug.LogWarning($"EnemyAttackTrigger: Not initialized on {gameObject.name}");
                return;
            }

            Debug.Log($"EnemyAttackTrigger: OnTriggerEnter from {other.name} (tag: {other.tag}) on {gameObject.name}");

            // Only detect player
            if (other.CompareTag("Player"))
            {
                Debug.Log($"EnemyAttackTrigger: Calling HandleTriggerEnter for Player on {_enemyMono.name}");
                _enemyMono.HandleTriggerEnter(other);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (!_isInitialized || _enemyMono == null) return;

            // Only detect player
            if (other.CompareTag("Player"))
            {
                _enemyMono.HandleTriggerExit(other);
            }
        }

        void OnTriggerStay(Collider other)
        {
            if (!_isInitialized || _enemyMono == null) return;

            // Only detect player
            if (other.CompareTag("Player"))
            {
                _enemyMono.HandleTriggerStay(other);
            }
        }
    }
}

