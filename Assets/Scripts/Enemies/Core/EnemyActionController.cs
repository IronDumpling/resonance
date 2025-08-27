using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Resonance.Interfaces.Objects;
using Resonance.Enemies.Core;

namespace Resonance.Enemies.Core
{
    /// <summary>
    /// Controls enemy actions and manages their execution.
    /// Handles action priority, interruption, and state management.
    /// Similar to Player ActionController but designed for enemy AI.
    /// </summary>
    public class EnemyActionController
    {
        // Core references
        private EnemyController _enemyController;
        private IEnemyAction _currentAction;
        private List<IEnemyAction> _availableActions;

        // Events
        public System.Action<IEnemyAction> OnActionStarted;
        public System.Action<IEnemyAction> OnActionFinished;
        public System.Action<IEnemyAction> OnActionCancelled;
        public System.Action<IEnemyAction> OnActionInterrupted;

        // Properties
        public bool IsActive => _currentAction != null;
        public string CurrentActionName => _currentAction?.Name ?? "None";
        public IEnemyAction CurrentAction => _currentAction;
        public int CurrentActionPriority => _currentAction?.Priority ?? 0;

        public EnemyActionController(EnemyController enemyController)
        {
            _enemyController = enemyController;
            _availableActions = new List<IEnemyAction>();
        }

        /// <summary>
        /// Initialize the action controller
        /// </summary>
        public void Initialize()
        {
            // Actions will be registered by the EnemyController or states
            Debug.Log("EnemyActionController: Initialized");
        }

        /// <summary>
        /// Register a new action to be available for execution
        /// </summary>
        /// <param name="action">The action to register</param>
        public void RegisterAction(IEnemyAction action)
        {
            if (action == null)
            {
                Debug.LogError("EnemyActionController: Cannot register null action");
                return;
            }

            if (_availableActions.Any(a => a.Name == action.Name))
            {
                Debug.LogWarning($"EnemyActionController: Action {action.Name} is already registered");
                return;
            }

            _availableActions.Add(action);
            Debug.Log($"EnemyActionController: Registered action: {action.Name} (Priority: {action.Priority})");
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
                Debug.Log($"EnemyActionController: Unregistered action: {actionName}");
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
                _currentAction.Update(_enemyController, deltaTime);

                // Check if action finished
                if (_currentAction.IsFinished)
                {
                    FinishCurrentAction();
                }
            }
        }

        /// <summary>
        /// Try to start an action by name
        /// </summary>
        /// <param name="actionName">Name of the action to start</param>
        /// <returns>True if action was started successfully</returns>
        public bool TryStartAction(string actionName)
        {
            var action = _availableActions.FirstOrDefault(a => a.Name == actionName);
            if (action == null)
            {
                Debug.LogWarning($"EnemyActionController: Action {actionName} not found");
                return false;
            }

            return TryStartAction(action);
        }

        /// <summary>
        /// Try to start a specific action
        /// </summary>
        /// <param name="action">The action to start</param>
        /// <returns>True if action was started successfully</returns>
        public bool TryStartAction(IEnemyAction action)
        {
            if (action == null) return false;

            // Check if action can start
            if (!action.CanStart(_enemyController))
            {
                return false;
            }

            // Check if we need to interrupt current action
            if (_currentAction != null)
            {
                // If new action has higher priority and current can be interrupted
                if (action.Priority > _currentAction.Priority && _currentAction.CanInterrupt)
                {
                    InterruptCurrentAction();
                }
                // If new action has same/lower priority, don't start
                else
                {
                    return false;
                }
            }

            // Start the action
            _currentAction = action;
            _currentAction.Start(_enemyController);
            OnActionStarted?.Invoke(_currentAction);
            
            Debug.Log($"EnemyActionController: Started action: {action.Name} (Priority: {action.Priority})");
            return true;
        }

        /// <summary>
        /// Try to start the best available action based on priority and conditions
        /// </summary>
        /// <returns>True if an action was started</returns>
        public bool TryStartBestAction()
        {
            // Get all actions that can start, sorted by priority (highest first)
            var availableActions = _availableActions
                .Where(a => a.CanStart(_enemyController))
                .OrderByDescending(a => a.Priority)
                .ToList();

            foreach (var action in availableActions)
            {
                if (TryStartAction(action))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Cancel the currently running action
        /// </summary>
        public void CancelCurrentAction()
        {
            if (_currentAction != null)
            {
                var actionName = _currentAction.Name;
                _currentAction.Cancel(_enemyController);
                OnActionCancelled?.Invoke(_currentAction);
                _currentAction = null;
                
                Debug.Log($"EnemyActionController: Cancelled action: {actionName}");
            }
        }

        /// <summary>
        /// Interrupt the currently running action (higher priority action takes over)
        /// </summary>
        private void InterruptCurrentAction()
        {
            if (_currentAction != null)
            {
                var actionName = _currentAction.Name;
                _currentAction.Cancel(_enemyController);
                OnActionInterrupted?.Invoke(_currentAction);
                _currentAction = null;
                
                Debug.Log($"EnemyActionController: Interrupted action: {actionName}");
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
                
                Debug.Log($"EnemyActionController: Finished action: {actionName}");
            }
        }

        /// <summary>
        /// Called when the enemy takes damage - handles action response
        /// </summary>
        public void OnEnemyDamageTaken()
        {
            if (_currentAction != null)
            {
                _currentAction.OnDamageTaken(_enemyController);
            }
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
            var action = _availableActions.FirstOrDefault(a => a.Name == actionName);
            return action != null && action.CanStart(_enemyController);
        }

        /// <summary>
        /// Get action by name
        /// </summary>
        /// <param name="actionName">Name of the action</param>
        /// <returns>The action if found, null otherwise</returns>
        public IEnemyAction GetAction(string actionName)
        {
            return _availableActions.FirstOrDefault(a => a.Name == actionName);
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
            OnActionInterrupted = null;
            
            Debug.Log("EnemyActionController: Cleaned up");
        }
    }
}
