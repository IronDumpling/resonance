using UnityEngine;
using Resonance.Core;
using Resonance.Player.Core;

namespace Resonance.Player.States
{
    /// <summary>
    /// Stun state where player is stunned and cannot move.
    /// </summary>
    public class PlayerStunState : IState
    {
        private PlayerController _playerController;

        public string Name => "Stun";

        public PlayerStunState(PlayerController playerController)
        {
            _playerController = playerController;
        }

        public void Enter()
        {
            Debug.Log("PlayerState: Entered Stun state");
        }
        
        public void Update()
        {
            // Stun state update logic
            // Could include things like checking for interaction opportunities, etc.
        }
        
        public void Exit()
        {
            Debug.Log("PlayerState: Exited Stun state");
        }

        public bool CanTransitionTo(IState newState)
        {
            // Can transition to any state from Stun
            return newState.Name == "Normal" || 
                   newState.Name == "Death";
        }
    }
}