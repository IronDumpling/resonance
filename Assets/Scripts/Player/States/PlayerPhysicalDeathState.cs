using UnityEngine;
using Resonance.Core;
using Resonance.Player.Core;

namespace Resonance.Player.States
{
    /// <summary>
    /// Physical death state where player's physical health has reached 0.
    /// Player enters "core mode" where they are vulnerable but can still move.
    /// Mental health starts decaying in this state.
    /// </summary>
    public class PlayerPhysicalDeathState : IState
    {
        private PlayerController _playerController;
        
        public string Name => "PhysicalDeath";

        public PlayerPhysicalDeathState(PlayerController playerController)
        {
            _playerController = playerController;
        }

        public void Enter()
        {
            Debug.Log("PlayerState: Entered Physical Death state - Physical health depleted, entering core mode");
            
            // Disable normal movement and combat
            _playerController.Movement.MovementSpeedModifier = 0.3f; // Slower movement in core mode
            
            // TODO: Visual effects for core exposure
            // TODO: Start mental health decay (handled in PlayerController)
            
            Debug.Log("PlayerState: Core exposed - mental health will start decaying");
        }

        public void Update()
        {
            // Physical death state update logic
            // Could handle core visual effects, vulnerability indicators, etc.
            
            // Mental health decay is handled in PlayerController.UpdateHealthRegeneration()
            // State transition to TrueDeath is handled by PlayerController when mental health reaches 0
        }

        public void Exit()
        {
            Debug.Log("PlayerState: Exited Physical Death state");
        }

        public bool CanTransitionTo(IState newState)
        {
            // Can transition to:
            // - Normal (through respawn at save point)
            // - TrueDeath (when mental health reaches 0)
            // - Core (future feature for enhanced core control)
            return newState.Name == "Normal" || 
                   newState.Name == "TrueDeath" ||
                   newState.Name == "Core";
        }
    }
}
