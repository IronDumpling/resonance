using UnityEngine;
using Resonance.Core;
using Resonance.Player.Core;

namespace Resonance.Player.States
{
    /// <summary>
    /// Aiming state where player can move slowly, look around, and shoot.
    /// Cannot interact with objects while aiming.
    /// </summary>
    public class PlayerAimingState : IState
    {
        private PlayerController _playerController;
        
        public string Name => "Aiming";

        public PlayerAimingState(PlayerController playerController)
        {
            _playerController = playerController;
        }

        public void Enter()
        {
            Debug.Log("PlayerState: Entered Aiming state");
            
            // Safety check: Ensure player has a weapon
            if (!_playerController.HasEquippedWeapon)
            {
                Debug.LogWarning("PlayerAimingState: Player entered aiming state without weapon! This should not happen.");
                return;
            }
            
            // Slow down movement while aiming
            _playerController.Movement.MovementSpeedModifier = 0.5f;
        }

        public void Update()
        {
            // Safety check: If weapon is removed while aiming, exit aiming state
            if (!_playerController.HasEquippedWeapon)
            {
                Debug.Log("PlayerAimingState: Weapon removed while aiming, exiting aiming state");
                _playerController.StateMachine.StopAiming();
                return;
            }
            
            // Aiming state update logic
            // Could include auto-aim assistance, reticle updates, etc.
        }

        public void Exit()
        {
            Debug.Log("PlayerState: Exited Aiming state");
        }

        public bool CanTransitionTo(IState newState)
        {
            // Can transition back to Normal or to death states
            return newState.Name == "Normal" || 
                   newState.Name == "PhysicalDeath" ||
                   newState.Name == "TrueDeath";
        }
    }
}
