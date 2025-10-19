using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Actions
{
    /// <summary>
    /// True death action - triggers death animation and handles object destruction
    /// Behavior Designer Best Practices:
    /// - Inherits from EnemyActionBase for component access
    /// - This is triggered when crystal core is destroyed
    /// - Returns Running during death animation, Success when ready for destruction
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Handles true death sequence when core is destroyed")]
    public class DeathAction : EnemyActionBase
    {
        private bool _deathTriggered = false;
        private float _destructionTimer = 0f;
        private const float DESTRUCTION_DELAY = 2f;

        public override void OnStart()
        {
            base.OnStart();
            _deathTriggered = false;
            _destructionTimer = 0f;
        }

        public override TaskStatus OnUpdate()
        {
            // Validate components are ready
            if (!ValidateComponents())
            {
                return TaskStatus.Failure;
            }

            if (!_deathTriggered)
            {
                // Stop all movement
                Movement?.Stop();
                Controller.StopPatrol();
                Controller.LosePlayer();
                
                // Trigger death animation
                if (Animator != null && Animator.isActiveAndEnabled)
                {
                    Animator.SetTrigger("TrueDeath");
                }
                
                _deathTriggered = true;
            }

            // Wait for destruction delay
            _destructionTimer += Time.deltaTime;
            
            if (_destructionTimer >= DESTRUCTION_DELAY)
            {
                // Note: Actual destruction is handled by EnemyMonoBehaviour listening to death events
                return TaskStatus.Success;
            }

            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            _deathTriggered = false;
            _destructionTimer = 0f;
        }
    }
}

