using UnityEngine;
using Resonance.Core;
using Resonance.Enemies.Core;

namespace Resonance.Enemies.States
{
    /// <summary>
    /// Stun state where enemy is stunned and cannot move.
    /// </summary>
    public class EnemyStunState : IState
    {
        private EnemyController _enemyController;

        public string Name => "Stun";

        public EnemyStunState(EnemyController enemyController)
        {
            _enemyController = enemyController;
        }

        public void Enter()
        {
            Debug.Log("EnemyState: Entered Stun state");
            
            // Stop all movement when entering stun
            _enemyController.Movement?.Stop();
            
            // Cancel any ongoing actions
            // This will interrupt Chase, Patrol, NormalAttack, and CoreAttack actions
            _enemyController.ActionController?.CancelCurrentAction();
        }
        
        public void Update()
        {
            // Stun state update logic
            // Could include things like checking for interaction opportunities, etc.
        }
        
        public void Exit()
        {
            Debug.Log("EnemyState: Exited Stun state");
        }

        public bool CanTransitionTo(IState newState)
        {
            // Can transition to any state from Stun
            return newState.Name == "Normal" || 
                   newState.Name == "Death";
        }
    }
}