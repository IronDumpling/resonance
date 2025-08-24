namespace Resonance.Interfaces.Objects
{
    using Resonance.Player.Core;

    /// <summary>
    /// Interface for player actions that can be executed by the ActionController.
    /// Actions can block movement, provide invulnerability, and be interrupted.
    /// </summary>
    public interface IPlayerAction
    {
        /// <summary>
        /// Name of the action for identification
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Whether this action blocks player movement
        /// </summary>
        bool BlocksMovement { get; }

        /// <summary>
        /// Whether this action provides invulnerability to physical damage
        /// </summary>
        bool ProvidesInvulnerability { get; }

        /// <summary>
        /// Whether this action can be interrupted by damage or other events
        /// </summary>
        bool CanInterrupt { get; }

        /// <summary>
        /// Check if this action can be started given the current player state
        /// </summary>
        /// <param name="player">Reference to the player controller</param>
        /// <returns>True if the action can start</returns>
        bool CanStart(PlayerController player);

        /// <summary>
        /// Start executing the action
        /// </summary>
        /// <param name="player">Reference to the player controller</param>
        void Start(PlayerController player);

        /// <summary>
        /// Update the action each frame
        /// </summary>
        /// <param name="player">Reference to the player controller</param>
        /// <param name="deltaTime">Time since last frame</param>
        void Update(PlayerController player, float deltaTime);

        /// <summary>
        /// Check if the action has finished executing
        /// </summary>
        bool IsFinished { get; }

        /// <summary>
        /// Cancel the action (called when interrupted or manually stopped)
        /// </summary>
        /// <param name="player">Reference to the player controller</param>
        void Cancel(PlayerController player);

        /// <summary>
        /// Called when the player takes damage while this action is active
        /// Only called if CanInterrupt is true
        /// </summary>
        /// <param name="player">Reference to the player controller</param>
        void OnDamageTaken(PlayerController player);
    }
}
