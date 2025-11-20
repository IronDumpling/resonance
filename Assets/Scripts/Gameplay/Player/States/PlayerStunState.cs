using UnityEngine;
using Resonance.Core;
using Resonance.Gameplay.Player.Core;

namespace Resonance.Gameplay.Player.States
{
    /// <summary>
    /// Stagger state where player is staggerned and cannot move.
    /// </summary>
    public class PlayerStaggerState : IState
    {
        private PlayerController _playerController;

        public string Name => "Stagger";

        public PlayerStaggerState(PlayerController playerController)
        {
            _playerController = playerController;
        }

        public void Enter()
        {
            Debug.Log("PlayerState: Entered Stagger state");
        }
        
        public void Update()
        {
            // Stagger state update logic
            // Could include things like checking for interaction opportunities, etc.
        }
        
        public void Exit()
        {
            Debug.Log("PlayerState: Exited Stagger state");
        }

        public bool CanTransitionTo(IState newState)
        {
            // Can transition to any state from Stagger
            return newState.Name == "Normal" || 
                   newState.Name == "Death";
        }
    }
}