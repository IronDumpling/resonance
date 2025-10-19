using UnityEngine;
using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Actions
{
    /// <summary>
    /// True death action - triggers death animation and handles object destruction
    /// This is triggered when crystal core is destroyed
    /// </summary>
    public class TrueDeathAction : ActionNode
    {
        private bool _deathTriggered = false;
        private float _destructionTimer = 0f;
        private const float DESTRUCTION_DELAY = 2f;

        public override BTNodeStatus Execute()
        {
            if (!_deathTriggered)
            {
                // Stop all movement
                Movement?.Stop();
                Controller.StopPatrol();
                Controller.LosePlayer();
                
                // Trigger death animation
                var animator = GetAnimator();
                if (animator != null && animator.isActiveAndEnabled)
                {
                    animator.SetTrigger("TrueDeath");
                }
                
                _deathTriggered = true;
            }

            // Wait for destruction delay
            _destructionTimer += Time.deltaTime;
            
            if (_destructionTimer >= DESTRUCTION_DELAY)
            {
                // Note: Actual destruction is handled by EnemyMonoBehaviour listening to death events
                return BTNodeStatus.Success;
            }

            return BTNodeStatus.Running;
        }

        public override void Reset()
        {
            base.Reset();
            _deathTriggered = false;
            _destructionTimer = 0f;
        }
    }
}

