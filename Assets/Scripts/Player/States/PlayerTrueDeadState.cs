using UnityEngine;
using Resonance.Core;
using Resonance.Player.Core;

namespace Resonance.Player.States
{
    /// <summary>
    /// True death state where player is completely dead (mental health = 0).
    /// This is a terminal state that can only be exited through game over/reload mechanisms.
    /// </summary>
    public class PlayerTrueDeathState : IState
    {
        private PlayerController _playerController;
        
        public string Name => "TrueDeath";

        public PlayerTrueDeathState(PlayerController playerController)
        {
            _playerController = playerController;
        }

        public void Enter()
        {
            Debug.Log("PlayerState: Entered True Death state - Mental health depleted");
            
            // Stop all movement and actions
            _playerController.Movement.MovementSpeedModifier = 0f;
            
            // Trigger true death logic
            // This should trigger game over screen, save system, etc.
        }

        public void Update()
        {
            // True death state update logic
            // Could handle death animations, game over screen, etc.
        }

        public void Exit()
        {
            Debug.Log("PlayerState: Exited True Death state");
        }

        public bool CanTransitionTo(IState newState)
        {
            // True death is terminal - can only exit through external systems (save loading, restart)
            // In practice, this should rarely be called as true death triggers game over
            return newState.Name == "Normal";
        }
    }
}
