using UnityEngine;
using Resonance.Enemies;
using Resonance.Enemies.Triggers;

namespace Resonance.Enemies.Core
{
    /// <summary>
    /// Enemy animation event relay - receives animation events and forwards them to the enemy controller
    /// Should be attached to the enemy Visual child GameObject
    /// Called by Animation Events in the Animator Controller
    /// </summary>
    public class EnemyAnimator : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;
        [Tooltip("Enable debug logging for animation events")]

        // References
        private EnemyController _enemyController;
        private EnemyDamageHitbox _damageHitbox;
        private bool _isInitialized = false;

        void OnEnable()
        {
            if (_isInitialized)
            {
                Debug.Log($"EnemyAnimator: Component enabled on {gameObject.name}, ready for animation events");
            }
        }

        #region Initialization

        /// <summary>
        /// Initialize the animator component (called by EnemyMonoBehaviour)
        /// This component is attached to the Visual child GameObject, not the root enemy GameObject
        /// Handles all animation events and forwards them to the BehaviorTree via event flags
        /// </summary>
        /// <param name="enemyMono">Enemy MonoBehaviour reference</param>
        /// <param name="damageHitbox">Optional damage hitbox reference</param>
        public void Initialize(EnemyMonoBehaviour enemyMono, EnemyDamageHitbox damageHitbox = null)
        {
            if (enemyMono == null || !enemyMono.IsInitialized)
            {
                Debug.LogError("EnemyAnimator: Cannot initialize with null or uninitialized EnemyMonoBehaviour!");
                return;
            }

            _enemyController = enemyMono.Controller;
            _damageHitbox = damageHitbox;
            _isInitialized = true;
            
            if (_debugMode)
            {
                Debug.Log($"EnemyAnimator: Initialized on {gameObject.name} with enemy controller from {enemyMono.name}");
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
                Debug.Log($"EnemyAnimator: Manually initialized with enemy controller");
            }
        }

        #endregion

        #region Animation Events

        /// <summary>
        /// Called by animation event when attack damage window starts
        /// Usually placed on the frame where the attack should start dealing damage
        /// Works for both NormalAttack and WaveAttack
        /// </summary>
        public void OnAttackCommit()
        {
            if (!_isInitialized)
            {
                Debug.LogError("EnemyAnimator: OnAttackCommit called but not initialized!");
                return;
            }

            string attackType = _enemyController?.CurrentAttackType.ToString() ?? "Unknown";
            Debug.Log($"EnemyAnimator: OnAttackCommit ({attackType}) - enabling hitbox");

            // Set flag for BehaviorTree
            _attackCommitTriggered = true;

            // Enable damage hitbox through controller (which now handles GameObject activation)
            _enemyController?.EnableHitbox();
        }

        /// <summary>
        /// Called by animation event when attack damage window ends
        /// Usually placed on the frame where the attack should stop dealing damage
        /// Works for both NormalAttack and WaveAttack
        /// </summary>
        public void OnAttackEnd()
        { 
            if (!_isInitialized)
            {
                Debug.LogError("EnemyAnimator: OnAttackEnd called but not initialized!");
                return;
            }

            string attackType = _enemyController?.CurrentAttackType.ToString() ?? "Unknown";
            Debug.Log($"EnemyAnimator: OnAttackEnd ({attackType}) - disabling hitbox");

            // Set flag for BehaviorTree
            _attackEndTriggered = true;

            // Disable damage hitbox through controller (which now handles GameObject deactivation)
            _enemyController?.DisableHitbox();
        }

        /// <summary>
        /// Called by animation event when attack sequence finishes
        /// Usually placed on the frame where the attack sequence should finish
        /// Works for both NormalAttack and WaveAttack
        /// </summary>
        public void OnAttackSequenceFinished()
        {
            if (!_isInitialized)
            {
                Debug.LogError("EnemyAnimator: OnAttackSequenceFinished called but not initialized!");
                return;
            }

            string attackType = _enemyController?.CurrentAttackType.ToString() ?? "Unknown";
            Debug.Log($"EnemyAnimator: OnAttackSequenceFinished ({attackType}) - attack sequence complete");

            // Set flag for BehaviorTree
            _attackSequenceFinishedTriggered = true;

            _enemyController?.AttackSequenceFinished();
        }

        /// <summary>
        /// Called by animation event when fall down animation finishes
        /// Used to transition from health death to revival state
        /// </summary>
        public void OnFallDownFinished()
        {
            if (!_isInitialized)
            {
                Debug.LogError("EnemyAnimator: OnFallDownFinished called but not initialized!");
                return;
            }

            if (_debugMode)
            {
                Debug.Log("EnemyAnimator: OnFallDownFinished - fall down animation complete");
            }
        }

        /// <summary>
        /// Called by animation event when revival animation starts
        /// </summary>
        public void OnRevivalStart()
        {
            if (!_isInitialized)
            {
                Debug.LogError("EnemyAnimator: OnRevivalStart called but not initialized!");
                return;
            }

            if (_debugMode)
            {
                Debug.Log("EnemyAnimator: OnRevivalStart - revival animation started");
            }

            // Set flag for BehaviorTree
            _revivalStartTriggered = true;

            // Additional revival start effects can be added here
        }

        /// <summary>
        /// Called by animation event when revival animation completes
        /// </summary>
        public void OnRevivalComplete()
        {
            if (!_isInitialized)
            {
                Debug.LogError("EnemyAnimator: OnRevivalComplete called but not initialized!");
                return;
            }

            if (_debugMode)
            {
                Debug.Log("EnemyAnimator: OnRevivalComplete - revival animation complete");
            }

            // Set flag for BehaviorTree
            _revivalCompleteTriggered = true;

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
                Debug.Log("EnemyAnimator: OnFootstep - footstep sound event");
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
                Debug.Log("EnemyAnimator: OnAttackSound - attack sound event");
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
                Debug.Log($"EnemyAnimator: OnCustomEvent - {eventName}");
            }

            // Handle custom animation events here
            switch (eventName)
            {
                case "AttackWindupComplete":
                    // Attack windup finished, ready for damage window
                    if (_debugMode)
                    {
                        string attackType = _enemyController?.CurrentAttackType.ToString() ?? "Unknown";
                        Debug.Log($"EnemyAnimator: AttackWindupComplete ({attackType}) - ready for damage window");
                    }
                    break;
                    
                case "AttackRecoveryStart":
                    // Attack recovery phase started
                    if (_debugMode)
                    {
                        string attackType = _enemyController?.CurrentAttackType.ToString() ?? "Unknown";
                        Debug.Log($"EnemyAnimator: AttackRecoveryStart ({attackType}) - recovery phase started");
                    }
                    break;
                    
                case "WaveAttackWindupComplete":
                    // Wave attack windup finished, ready for damage window
                    if (_debugMode)
                    {
                        Debug.Log("EnemyAnimator: WaveAttackWindupComplete - wave attack ready for damage window");
                    }
                    break;
                    
                case "WaveAttackRecoveryStart":
                    // Wave attack recovery phase started
                    if (_debugMode)
                    {
                        Debug.Log("EnemyAnimator: WaveAttackRecoveryStart - wave attack recovery phase started");
                    }
                    break;
                    
                default:
                    if (_debugMode)
                    {
                        Debug.LogWarning($"EnemyAnimator: Unknown custom event: {eventName}");
                    }
                    break;
            }
        }

        #endregion

        #region Animation Event Tracking (for BehaviorTree)

        // Track animation events for BehaviorTree nodes
        private bool _attackCommitTriggered = false;
        private bool _attackEndTriggered = false;
        private bool _attackSequenceFinishedTriggered = false;
        private bool _revivalStartTriggered = false;
        private bool _revivalCompleteTriggered = false;

        /// <summary>
        /// Reset all animation event flags (called by action nodes when starting)
        /// </summary>
        public void ResetEventFlags()
        {
            _attackCommitTriggered = false;
            _attackEndTriggered = false;
            _attackSequenceFinishedTriggered = false;
            _revivalStartTriggered = false;
            _revivalCompleteTriggered = false;
        }

        /// <summary>
        /// Check if a specific animation event was triggered (consumed after check)
        /// </summary>
        public bool IsEventTriggered(string eventName)
        {
            switch (eventName)
            {
                case "OnAttackCommit":
                    if (_attackCommitTriggered)
                    {
                        _attackCommitTriggered = false; // Consume the event
                        return true;
                    }
                    return false;

                case "OnAttackEnd":
                    if (_attackEndTriggered)
                    {
                        _attackEndTriggered = false; // Consume the event
                        return true;
                    }
                    return false;

                case "OnAttackSequenceFinished":
                    if (_attackSequenceFinishedTriggered)
                    {
                        _attackSequenceFinishedTriggered = false; // Consume the event
                        return true;
                    }
                    return false;

                case "OnRevivalStart":
                    if (_revivalStartTriggered)
                    {
                        _revivalStartTriggered = false; // Consume the event
                        return true;
                    }
                    return false;

                case "OnRevivalComplete":
                    if (_revivalCompleteTriggered)
                    {
                        _revivalCompleteTriggered = false; // Consume the event
                        return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Set animation trigger (helper method for BT nodes)
        /// </summary>
        public void SetTrigger(string triggerName)
        {
            var animator = GetComponent<Animator>();
            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.SetTrigger(triggerName);
                if (_debugMode)
                {
                    Debug.Log($"EnemyAnimator: Set trigger '{triggerName}'");
                }
            }
        }

        /// <summary>
        /// Set animation bool (helper method for BT nodes)
        /// </summary>
        public void SetBool(string paramName, bool value)
        {
            var animator = GetComponent<Animator>();
            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.SetBool(paramName, value);
                if (_debugMode)
                {
                    Debug.Log($"EnemyAnimator: Set bool '{paramName}' = {value}");
                }
            }
        }

        /// <summary>
        /// Reset animation trigger (helper method for BT nodes)
        /// </summary>
        public void ResetTrigger(string triggerName)
        {
            var animator = GetComponent<Animator>();
            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.ResetTrigger(triggerName);
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
