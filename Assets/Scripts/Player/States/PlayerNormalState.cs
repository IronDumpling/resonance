using UnityEngine;
using Resonance.Core;
using Resonance.Player.Core;

namespace Resonance.Player.States
{
    /// <summary>
    /// Normal state where player can move, run, interact, and enter aiming mode.
    /// </summary>
    public class PlayerNormalState : IState
    {
        private PlayerController _playerController;
        
        public string Name => "Normal";

        public PlayerNormalState(PlayerController playerController)
        {
            _playerController = playerController;
        }

        public void Enter()
        {
            Debug.Log("PlayerState: Entered Normal state");
            
            // Reset movement speed modifier
            _playerController.Movement.MovementSpeedModifier = 1f;
        }

        public void Update()
        {
            // Normal state update logic
            // Could include things like checking for interaction opportunities, etc.
        }

        public void Exit()
        {
            Debug.Log("PlayerState: Exited Normal state");
        }

        public bool CanTransitionTo(IState newState)
        {
            // Can transition to any state from Normal
            return newState.Name == "Aiming" || 
                   newState.Name == "Interacting" || 
                   newState.Name == "PhysicalDeath" ||
                   newState.Name == "TrueDeath";
        }
    }
}
