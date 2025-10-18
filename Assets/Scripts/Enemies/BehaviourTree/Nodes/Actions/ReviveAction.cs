using UnityEngine;
using Resonance.Enemies.Core;
using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Actions
{
    /// <summary>
    /// Revive action node - handles the revival process when physical health reaches 0
    /// Simplified to follow BT design principles:
    /// - No internal condition checking (handled by ConditionNode)
    /// - Focuses only on executing the revival behavior
    /// Restores physical health over time
    /// </summary>
    public class ReviveAction : ActionNode
    {
        private float _reviveTimer = 0f;
        private float _maxReviveTime;
        private bool _initialized = false;

        public override BTNodeStatus Execute()
        {
            var animator = GetAnimator();
            
            // Initialize on first execution
            if (!_initialized)
            {
                _initialized = true;
                _reviveTimer = 0f;
                _maxReviveTime = 3f * Controller.Stats.maxHealth / Controller.Stats.revivalRate;

                // Stop all movement and behaviors
                Controller.StopPatrol();
                Controller.LosePlayer();
                Movement?.Stop();
                
                // ★ Set revival animation state - reset all parameters
                if (animator != null && animator.isActiveAndEnabled)
                {
                    animator.SetBool("IsReviving", true);      // Enter revival state
                    animator.SetFloat("Speed", 0f);            // No movement during revival
                    animator.SetBool("HasTarget", false);      // No target during revival
                    animator.SetBool("InAttackRange", false);  // Cannot attack during revival
                }
                
                Debug.Log("[BT Action] ReviveAction: Started revival - IsReviving=true, all other params reset");
            }

            _reviveTimer += Time.deltaTime;

            // Check if revival is complete (physical health restored)
            if (Controller.IsAlive)
            {
                // Complete revival animation
                if (animator != null && animator.isActiveAndEnabled)
                {
                    animator.SetBool("IsReviving", false);
                    animator.SetTrigger("ReviveComplete");
                }
                
                Debug.Log("ReviveAction: Revival completed - triggered ReviveComplete");
                return BTNodeStatus.Success;
            }

            // Safety timeout
            if (_reviveTimer > _maxReviveTime)
            {
                Controller.Stats.FullRestore();
                
                // Complete revival animation
                if (animator != null && animator.isActiveAndEnabled)
                {
                    animator.SetBool("IsReviving", false);
                    animator.SetTrigger("ReviveComplete");
                }
                
                return BTNodeStatus.Success;
            }

            return BTNodeStatus.Running;
        }

        /// <summary>
        /// Reset revival state for next execution
        /// ★ CRITICAL: Reset IsReviving to allow Animator to exit revival state
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            
            // ★ Clean up animation state - allow Animator to return to normal
            var animator = GetAnimator();
            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.SetBool("IsReviving", false);
                Debug.Log("[BT Action] ReviveAction: Reset - IsReviving=false");
            }
            
            _initialized = false;
            _reviveTimer = 0f;
        }
    }
}
