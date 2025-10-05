using UnityEngine;
using Resonance.Core;
using Resonance.Player.Core;

namespace Resonance.Player.States
{
    /// <summary>
    /// Death state where player is completely dead (core health = 0).
    /// This is a terminal state that can only be exited through game over/reload mechanisms.
    /// </summary>
    public class PlayerDeathState : IState
    {
        private PlayerController _playerController;
        
        public string Name => "Death";

        public PlayerDeathState(PlayerController playerController)
        {
            _playerController = playerController;
        }

        public void Enter()
        {
            Debug.Log("PlayerState: Entered Death state - Core health depleted");
            
            // Stop all movement and actions
            _playerController.Movement.MovementSpeedModifier = 0f;
            
            // Trigger death logic
            GameManager.Instance.StateMachine.ChangeState("OutGame");
        }

        public void Update()
        {
            // Death state update logic
            // Could handle death animations, game over screen, etc.
        }

        public void Exit()
        {
            Debug.Log("PlayerState: Exited Death state");
        }

        public bool CanTransitionTo(IState newState)
        {
            // Death is terminal - can only exit through external systems
            return false;
        }
    }
}
