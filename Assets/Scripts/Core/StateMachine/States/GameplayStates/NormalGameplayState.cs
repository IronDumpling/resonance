using UnityEngine;
using Resonance.Core;

namespace Resonance.Core.StateMachine.States
{
    /// <summary>
    /// Normal gameplay substate - default substate of GameplayState
    /// Handles regular gameplay when not in special modes like Wave
    /// </summary>
    public class NormalGameplayState : IState
    {
        public string Name => "Normal";

        public void Enter()
        {
            Debug.Log("NormalGameplayState: Entering normal gameplay substate");
            
            // Normal gameplay initialization
            // Most gameplay logic is handled by other systems, this is just the default state
        }

        public void Update()
        {
            // Normal gameplay update logic
            // Most updates are handled by other systems (Player, Enemies, etc.)
        }

        public void Exit()
        {
            Debug.Log("NormalGameplayState: Exiting normal gameplay substate");
            
            // Normal gameplay cleanup
        }

        public bool CanTransitionTo(IState newState)
        {
            // Within GameplayState substate machine, allow transitions to all other substates
            // This includes: Normal, Wave, InfoReading, or any future Gameplay substates
            return true;
        }
    }
}
