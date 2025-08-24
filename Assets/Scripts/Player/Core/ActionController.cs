using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Resonance.Interfaces.Objects;
using Resonance.Player.Core;
using Resonance.Interfaces.Services;
using Resonance.Core;
using Resonance.Utilities;

namespace Resonance.Player.Core
{
    /// <summary>
    /// Controls player actions and manages their execution.
    /// Handles action priority, blocking, and interruption logic.
    /// </summary>
    public class ActionController
    {
        // Core references
        private PlayerController _playerController;
        private IPlayerAction _currentAction;
        private List<IPlayerAction> _availableActions;

        // Events
        public System.Action<IPlayerAction> OnActionStarted;
        public System.Action<IPlayerAction> OnActionFinished;
        public System.Action<IPlayerAction> OnActionCancelled;

        // Properties
        public bool IsActive => _currentAction != null;
        public bool IsBlocking => _currentAction?.BlocksMovement ?? false;
        public bool IsInvulnerable => _currentAction?.ProvidesInvulnerability ?? false;
        public string CurrentActionName => _currentAction?.Name ?? "None";
        public IPlayerAction CurrentAction => _currentAction;

        public ActionController(PlayerController playerController)
        {
            _playerController = playerController;
            _availableActions = new List<IPlayerAction>();
        }

        /// <summary>
        /// Initialize the action controller and register available actions
        /// </summary>
        public void Initialize()
        {
            // Actions will be registered by the PlayerController
            Debug.Log("ActionController: Initialized");
        }

        /// <summary>
        /// Register a new action to be available for execution
        /// </summary>
        /// <param name="action">The action to register</param>
        public void RegisterAction(IPlayerAction action)
        {
            if (action == null)
            {
                Debug.LogError("ActionController: Cannot register null action");
                return;
            }

            if (_availableActions.Any(a => a.Name == action.Name))
            {
                Debug.LogWarning($"ActionController: Action {action.Name} is already registered");
                return;
            }

            _availableActions.Add(action);
            Debug.Log($"ActionController: Registered action: {action.Name}");
        }

        /// <summary>
        /// Unregister an action
        /// </summary>
        /// <param name="actionName">Name of the action to unregister</param>
        public void UnregisterAction(string actionName)
        {
            var action = _availableActions.FirstOrDefault(a => a.Name == actionName);
            if (action != null)
            {
                // Cancel if currently running
                if (_currentAction == action)
                {
                    CancelCurrentAction();
                }

                _availableActions.Remove(action);
                Debug.Log($"ActionController: Unregistered action: {actionName}");
            }
        }

        /// <summary>
        /// Update the action controller each frame
        /// </summary>
        /// <param name="deltaTime">Time since last frame</param>
        public void Update(float deltaTime)
        {
            // Update current action
            if (_currentAction != null)
            {
                _currentAction.Update(_playerController, deltaTime);

                // Check if action finished
                if (_currentAction.IsFinished)
                {
                    FinishCurrentAction();
                }
            }
            else
            {
                // Try to start priority action if no action is running
                TryStartPriorityAction();
            }
        }

        /// <summary>
        /// Try to start an action by name
        /// </summary>
        /// <param name="actionName">Name of the action to start</param>
        /// <returns>True if action was started successfully</returns>
        public bool TryStartAction(string actionName)
        {
            if (_currentAction != null)
            {
                Debug.LogWarning($"ActionController: Cannot start {actionName} - action {_currentAction.Name} is already running");
                return false;
            }

            var action = _availableActions.FirstOrDefault(a => a.Name == actionName);
            if (action == null)
            {
                Debug.LogWarning($"ActionController: Action {actionName} not found");
                return false;
            }

            return TryStartAction(action);
        }

        /// <summary>
        /// Try to start a specific action
        /// </summary>
        /// <param name="action">The action to start</param>
        /// <returns>True if action was started successfully</returns>
        public bool TryStartAction(IPlayerAction action)
        {
            if (action == null) return false;

            if (_currentAction != null)
            {
                Debug.LogWarning($"ActionController: Cannot start {action.Name} - action {_currentAction.Name} is already running");
                return false;
            }

            if (!action.CanStart(_playerController))
            {
                Debug.Log($"ActionController: Action {action.Name} cannot start - conditions not met");
                return false;
            }

            // Start the action
            _currentAction = action;
            _currentAction.Start(_playerController);
            OnActionStarted?.Invoke(_currentAction);
            
            Debug.Log($"ActionController: Started action: {action.Name}");
            return true;
        }

        /// <summary>
        /// Cancel the currently running action
        /// </summary>
        public void CancelCurrentAction()
        {
            if (_currentAction != null)
            {
                var actionName = _currentAction.Name;
                _currentAction.Cancel(_playerController);
                OnActionCancelled?.Invoke(_currentAction);
                _currentAction = null;
                
                Debug.Log($"ActionController: Cancelled action: {actionName}");
            }
        }

        /// <summary>
        /// Finish the currently running action normally
        /// </summary>
        private void FinishCurrentAction()
        {
            if (_currentAction != null)
            {
                var actionName = _currentAction.Name;
                OnActionFinished?.Invoke(_currentAction);
                _currentAction = null;
                
                Debug.Log($"ActionController: Finished action: {actionName}");
            }
        }

        /// <summary>
        /// Called when the player takes damage - handles action interruption
        /// </summary>
        public void OnPlayerDamageTaken()
        {
            if (_currentAction != null && _currentAction.CanInterrupt)
            {
                Debug.Log($"ActionController: Action {_currentAction.Name} interrupted by damage");
                _currentAction.OnDamageTaken(_playerController);
                CancelCurrentAction();
            }
        }

        /// <summary>
        /// Try to start priority action based on current conditions
        /// This implements the priority logic defined in requirements
        /// </summary>
        private void TryStartPriorityAction()
        {
            // Priority logic:
            // 1. If there are dead enemies in mental attack range -> ResonanceAction
            // 2. If no dead enemies in range -> RecoverAction
            // 3. InteractAction has its own trigger conditions

            // Check for dead enemies in range (placeholder logic)
            bool hasDeadEnemiesInRange = HasDeadEnemiesInRange();

            IPlayerAction priorityAction = null;

            if (hasDeadEnemiesInRange)
            {
                // Priority 1: ResonanceAction
                priorityAction = _availableActions.FirstOrDefault(a => a.Name == "Resonance");
            }
            else
            {
                // Priority 2: RecoverAction
                priorityAction = _availableActions.FirstOrDefault(a => a.Name == "Recover");
            }

            // Try to start the priority action if conditions are met
            if (priorityAction != null && priorityAction.CanStart(_playerController))
            {
                // Check for input triggers (this will be implemented when we add input handling)
                // For now, actions need to be triggered manually
            }
        }

        /// <summary>
        /// Check if there are dead enemies in mental attack range
        /// Uses PlayerMonoBehaviour's MentalAttackTrigger system
        /// </summary>
        /// <returns>True if dead enemies are in range</returns>
        private bool HasDeadEnemiesInRange()
        {
            // Access PlayerMonoBehaviour through the PlayerController
            // This requires a way to get the PlayerMonoBehaviour from PlayerController
            // For now, we'll use a simpler approach through static access or service registry
            
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer != null)
            {
                return playerService.CurrentPlayer.HasDeadEnemiesInMentalAttackRange();
            }
            
            return false;
        }

        /// <summary>
        /// Get all available action names
        /// </summary>
        /// <returns>List of action names</returns>
        public List<string> GetAvailableActionNames()
        {
            return _availableActions.Select(a => a.Name).ToList();
        }

        /// <summary>
        /// Check if a specific action is available and can start
        /// </summary>
        /// <param name="actionName">Name of the action to check</param>
        /// <returns>True if action is available and can start</returns>
        public bool CanStartAction(string actionName)
        {
            if (_currentAction != null) return false;

            var action = _availableActions.FirstOrDefault(a => a.Name == actionName);
            return action != null && action.CanStart(_playerController);
        }

        /// <summary>
        /// Cleanup the action controller
        /// </summary>
        public void Cleanup()
        {
            CancelCurrentAction();
            _availableActions.Clear();
            
            OnActionStarted = null;
            OnActionFinished = null;
            OnActionCancelled = null;
            
            Debug.Log("ActionController: Cleaned up");
        }
    }
}
