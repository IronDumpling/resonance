using UnityEngine;
using Resonance.Core;
using Resonance.Player.Core;

namespace Resonance.Player.States
{
    /// <summary>
    /// Interacting state where player is engaged with an interactive object.
    /// Cannot move, aim, or perform other actions during interaction.
    /// </summary>
    public class PlayerInteractingState : IState
    {
        private PlayerController _playerController;
        
        public string Name => "Interacting";

        public PlayerInteractingState(PlayerController playerController)
        {
            _playerController = playerController;
        }

        public void Enter()
        {
            Debug.Log("PlayerState: Entered Interacting state");
            
            // Stop all movement during interaction
            _playerController.Movement.MovementSpeedModifier = 0f;
        }

        public void Update()
        {
            // Interaction update logic
            // Could handle interaction progress, animations, etc.
        }

        public void Exit()
        {
            Debug.Log("PlayerState: Exited Interacting state");
        }

        public bool CanTransitionTo(IState newState)
        {
            // Can only transition back to Normal or to death states
            return newState.Name == "Normal" || 
                   newState.Name == "PhysicalDeath" ||
                   newState.Name == "TrueDeath";
        }
    }
}
