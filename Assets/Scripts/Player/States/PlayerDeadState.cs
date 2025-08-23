using UnityEngine;
using Resonance.Core;
using Resonance.Player.Core;

namespace Resonance.Player.States
{
    /// <summary>
    /// Dead state where player cannot perform any actions.
    /// Can only be exited through respawn/reload mechanisms.
    /// </summary>
    public class PlayerDeadState : IState
    {
        private PlayerController _playerController;
        
        public string Name => "Dead";

        public PlayerDeadState(PlayerController playerController)
        {
            _playerController = playerController;
        }

        public void Enter()
        {
            Debug.Log("PlayerState: Entered Dead state");
            
            // Stop all movement
            _playerController.Movement.MovementSpeedModifier = 0f;
        }

        public void Update()
        {
            // Dead state update logic
            // Could handle death animations, respawn timer, etc.
        }

        public void Exit()
        {
            Debug.Log("PlayerState: Exited Dead state");
        }

        public bool CanTransitionTo(IState newState)
        {
            // Can only transition back to Normal through respawn
            return newState.Name == "Normal";
        }
    }
}
