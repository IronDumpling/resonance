using UnityEngine;
using Resonance.Core;
using Resonance.Player.Core;

namespace Resonance.Player.States
{
    /// <summary>
    /// True death state where player is completely dead (mental health = 0).
    /// This is a terminal state that can only be exited through game over/reload mechanisms.
    /// </summary>
    public class PlayerCoreState : IState
    {
        public string Name => "Core";

        public PlayerCoreState()
        {
            // TODO: Add core controller
        }

        public void Enter()
        {

        }

        public void Update()
        {

        }

        public void Exit()
        {

        }

        public bool CanTransitionTo(IState newState)
        {
            return newState.Name == "Normal" || 
                   newState.Name == "TrueDeath";
        }
    }
}