using UnityEngine;
using Resonance.Enemies.Core;

namespace Resonance.Enemies
{
    /// <summary>
    /// Enemy animation event relay - receives animation events and forwards them to the enemy controller
    /// Should be attached to the enemy root GameObject or Visual child
    /// Called by Animation Events in the Animator Controller
    /// </summary>
    public class EnemyAnimRelay : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;
        [Tooltip("Enable debug logging for animation events")]

        // References
        private EnemyController _enemyController;
        private EnemyDamageHitbox _damageHitbox;
        private bool _isInitialized = false;

        #region Initialization

        /// <summary>
        /// Initialize the animation relay (called by EnemyMonoBehaviour, similar to EnemyDetectionTrigger)
        /// This component is attached to the Visual child GameObject, not the root enemy GameObject
        /// </summary>
        /// <param name="enemyMono">Enemy MonoBehaviour reference</param>
        /// <param name="damageHitbox">Optional damage hitbox reference</param>
        public void Initialize(EnemyMonoBehaviour enemyMono, EnemyDamageHitbox damageHitbox = null)
        {
            if (enemyMono == null || !enemyMono.IsInitialized)
            {
                Debug.LogError("EnemyAnimRelay: Cannot initialize with null or uninitialized EnemyMonoBehaviour!");
                return;
            }

            _enemyController = enemyMono.Controller;
            _damageHitbox = damageHitbox;
            _isInitialized = true;
            
            if (_debugMode)
            {
                Debug.Log($"EnemyAnimRelay: Initialized on {gameObject.name} with enemy controller from {enemyMono.name}");
            }
        }

        /// <summary>
        /// Initialize with specific components (alternative method)
        /// </summary>
        public void Initialize(EnemyController enemyController, EnemyDamageHitbox damageHitbox = null)
        {
            _enemyController = enemyController;
            _damageHitbox = damageHitbox;
            _isInitialized = true;
            
            if (_debugMode)
            {
                Debug.Log($"EnemyAnimRelay: Manually initialized with enemy controller");
            }
        }

        #endregion

        #region Animation Events

        /// <summary>
        /// Called by animation event when attack damage window starts
        /// Usually placed on the frame where the attack should start dealing damage
        /// </summary>
        public void OnAttackCommit()
        {
            if (!_isInitialized)
            {
                Debug.LogError("EnemyAnimRelay: OnAttackCommit called but not initialized!");
                return;
            }

            if (_debugMode)
            {
                Debug.Log("EnemyAnimRelay: OnAttackCommit - enabling hitbox");
            }

            // Enable damage hitbox through controller (which now handles GameObject activation)
            _enemyController?.EnableHitbox();
        }

        /// <summary>
        /// Called by animation event when attack damage window ends
        /// Usually placed on the frame where the attack should stop dealing damage
        /// </summary>
        public void OnAttackEnd()
        {
            if (!_isInitialized)
            {
                Debug.LogError("EnemyAnimRelay: OnAttackEnd called but not initialized!");
                return;
            }

            if (_debugMode)
            {
                Debug.Log("EnemyAnimRelay: OnAttackEnd - disabling hitbox");
            }

            // Disable damage hitbox through controller (which now handles GameObject deactivation)
            _enemyController?.DisableHitbox();
        }

        /// <summary>
        /// Called by animation event when attack sequence finishes
        /// Usually placed on the frame where the attack sequence should finish
        /// </summary>
        public void OnAttackSequenceFinished()
        {
            if (!_isInitialized)
            {
                Debug.LogError("EnemyAnimRelay: OnAttackSequenceFinished called but not initialized!");
                return;
            }

            _enemyController?.AttackSequenceFinished();
        }

        /// <summary>
        /// Called by animation event when fall down animation finishes
        /// Used to transition from physical death to revival state
        /// </summary>
        public void OnFallDownFinished()
        {
            if (!_isInitialized)
            {
                Debug.LogError("EnemyAnimRelay: OnFallDownFinished called but not initialized!");
                return;
            }

            if (_debugMode)
            {
                Debug.Log("EnemyAnimRelay: OnFallDownFinished - fall down animation complete");
            }
        }

        /// <summary>
        /// Called by animation event when revival animation starts
        /// </summary>
        public void OnRevivalStart()
        {
            if (!_isInitialized)
            {
                Debug.LogError("EnemyAnimRelay: OnRevivalStart called but not initialized!");
                return;
            }

            if (_debugMode)
            {
                Debug.Log("EnemyAnimRelay: OnRevivalStart - revival animation started");
            }

            // Additional revival start effects can be added here
        }

        /// <summary>
        /// Called by animation event when revival animation completes
        /// </summary>
        public void OnRevivalComplete()
        {
            if (!_isInitialized)
            {
                Debug.LogError("EnemyAnimRelay: OnRevivalComplete called but not initialized!");
                return;
            }

            if (_debugMode)
            {
                Debug.Log("EnemyAnimRelay: OnRevivalComplete - revival animation complete");
            }

            // The actual revival completion is handled by the EnemyController
            // This event can be used for additional visual/audio effects
        }

        /// <summary>
        /// Called by animation event for footstep sounds during locomotion
        /// </summary>
        public void OnFootstep()
        {
            if (!_isInitialized) return;

            if (_debugMode)
            {
                Debug.Log("EnemyAnimRelay: OnFootstep - footstep sound event");
            }

            // Footstep audio can be played here
            // Could integrate with audio service if needed
        }

        /// <summary>
        /// Called by animation event for attack sound effects
        /// </summary>
        public void OnAttackSound()
        {
            if (!_isInitialized) return;

            if (_debugMode)
            {
                Debug.Log("EnemyAnimRelay: OnAttackSound - attack sound event");
            }

            // Attack audio can be played here
            // Could integrate with audio service if needed
        }

        /// <summary>
        /// Generic animation event handler for custom events
        /// </summary>
        /// <param name="eventName">Name of the custom event</param>
        public void OnCustomEvent(string eventName)
        {
            if (!_isInitialized) return;

            if (_debugMode)
            {
                Debug.Log($"EnemyAnimRelay: OnCustomEvent - {eventName}");
            }

            // Handle custom animation events here
            switch (eventName)
            {
                case "AttackWindupComplete":
                    // Attack windup finished, ready for damage window
                    break;
                    
                case "AttackRecoveryStart":
                    // Attack recovery phase started
                    break;
                    
                default:
                    if (_debugMode)
                    {
                        Debug.LogWarning($"EnemyAnimRelay: Unknown custom event: {eventName}");
                    }
                    break;
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Check if the relay is properly initialized
        /// </summary>
        public bool IsInitialized => _isInitialized && _enemyController != null;

        /// <summary>
        /// Get the associated enemy controller
        /// </summary>
        public EnemyController EnemyController => _enemyController;

        /// <summary>
        /// Get the associated damage hitbox
        /// </summary>
        public EnemyDamageHitbox DamageHitbox => _damageHitbox;

        #endregion
    }
}
