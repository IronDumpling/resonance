namespace Resonance.Interfaces.Objects
{
    using Resonance.Enemies.Core;

    /// <summary>
    /// Interface for enemy actions that can be executed by the EnemyActionController.
    /// Actions define specific behaviors like attacking, patrolling, chasing, etc.
    /// </summary>
    public interface IEnemyAction
    {
        /// <summary>
        /// Name of the action for identification
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Priority level of this action (higher priority actions interrupt lower ones)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Whether this action can be interrupted by higher priority actions
        /// </summary>
        bool CanInterrupt { get; }

        /// <summary>
        /// Whether this action is currently finished
        /// </summary>
        bool IsFinished { get; }

        /// <summary>
        /// Check if this action can be started given the current enemy state
        /// </summary>
        /// <param name="enemy">Reference to the enemy controller</param>
        /// <returns>True if the action can start</returns>
        bool CanStart(EnemyController enemy);

        /// <summary>
        /// Start executing the action
        /// </summary>
        /// <param name="enemy">Reference to the enemy controller</param>
        void Start(EnemyController enemy);

        /// <summary>
        /// Update the action each frame
        /// </summary>
        /// <param name="enemy">Reference to the enemy controller</param>
        /// <param name="deltaTime">Time since last frame</param>
        void Update(EnemyController enemy, float deltaTime);

        /// <summary>
        /// Cancel the action (called when interrupted or manually stopped)
        /// </summary>
        /// <param name="enemy">Reference to the enemy controller</param>
        void Cancel(EnemyController enemy);

        /// <summary>
        /// Called when the enemy takes damage while this action is active
        /// </summary>
        /// <param name="enemy">Reference to the enemy controller</param>
        void OnDamageTaken(EnemyController enemy);
    }
}
