using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Gameplay.Enemies.BTNodes.Actions
{
    /// <summary>
    /// Unbalanced action node - handles the unbalanced state when balance reaches 0
    /// 
    /// Architecture:
    /// - BehaviorTree (this node): Controls the unbalanced flow and coordinates all phases
    /// - EnemyController: Provides data queries and manages balance (no flow control)
    /// - EnemyAnimator: Plays animations and provides synchronization events
    /// - EnemyState: Calculates state based on balance values
    /// 
    /// Unbalanced Flow:
    /// 1. Initializing: Stop behaviors, reset flags, notify controller
    /// 2. Waiting: Wait for unbalancedDuration timer (vulnerable to player wave execution)
    /// 3. Completing: Restore balance to max, return to normal state
    /// 
    /// Note: If player executes wave attack during Waiting phase, this action will be
    /// interrupted and CoreExposedAction will take over
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Handles unbalanced state when balance reaches 0")]
    public class UnbalancedAction : EnemyActionBase
    {
        /// <summary>
        /// Unbalanced phase state machine
        /// </summary>
        private enum UnbalancedPhase
        {
            Initializing,   // Initial setup, disable enemy behaviors
            Waiting,        // Waiting for unbalanced duration, vulnerable to execution
            Completing,     // Restoring balance and returning to normal
            Done            // Unbalanced complete
        }
        
        // Phase tracking
        private UnbalancedPhase _currentPhase;
        private float _phaseTimer;
        private float _globalTimer;
        
        // Dynamic timeout configuration
        private float _unbalancedDuration;
        private const float GLOBAL_TIMEOUT_MARGIN = 2f;

        public override void OnStart()
        {
            base.OnStart();
            
            if (!ValidateComponents())
            {
                return;
            }
            
            // Get unbalanced duration from stats
            _unbalancedDuration = Controller.Stats.unbalancedDuration;
            
            // Initialize phase state
            _currentPhase = UnbalancedPhase.Initializing;
            _phaseTimer = 0f;
            _globalTimer = 0f;
            
            // Execute initialization immediately
            InitializeUnbalanced();
        }
        
        /// <summary>
        /// Phase 1: Initialize unbalanced process
        /// </summary>
        private void InitializeUnbalanced()
        {
            // Stop all behaviors
            Controller.StopPatrol();
            Controller.LosePlayer();
            Movement?.Stop();
            
            // Notify controller to start unbalanced state (triggers events, no flow control)
            Controller.StartUnbalanced();
            
            // Transition to Waiting phase
            _currentPhase = UnbalancedPhase.Waiting;
            _phaseTimer = 0f;
            
            // Trigger unbalanced animation
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                Animator.SetBool("IsUnbalanced", true);
                Animator.SetFloat("Speed", 0f);
                Animator.SetBool("HasTarget", false);
                Animator.SetBool("InAttackRange", false);
            }
            
            Debug.Log($"[UnbalancedAction] Phase: Initializing → Waiting (Duration: {_unbalancedDuration:F2}s)");
        }

        public override TaskStatus OnUpdate()
        {
            if (!ValidateComponents())
            {
                return TaskStatus.Failure;
            }
            
            _phaseTimer += Time.deltaTime;
            _globalTimer += Time.deltaTime;
            
            // Global timeout protection
            float globalTimeout = _unbalancedDuration + GLOBAL_TIMEOUT_MARGIN;
            if (_globalTimer > globalTimeout)
            {
                Debug.LogWarning($"[UnbalancedAction] Global timeout! Forcing completion. Timer: {_globalTimer:F2}s / {globalTimeout:F2}s");
                ForceComplete();
                return TaskStatus.Success;
            }
            
            // Check if enemy was executed (entered CoreExposed state)
            // If so, interrupt this action and let CoreExposedAction take over
            if (Controller.CurrentState == Data.EnemyState.CoreExposed)
            {
                Debug.Log("[UnbalancedAction] Enemy was executed by player, interrupting");
                CleanupAnimation();
                return TaskStatus.Failure; // Failure causes behavior tree to re-evaluate
            }
            
            // Phase-based processing
            switch (_currentPhase)
            {
                case UnbalancedPhase.Waiting:
                    return HandleWaitingPhase();
                    
                case UnbalancedPhase.Completing:
                    return HandleCompletingPhase();
                    
                case UnbalancedPhase.Done:
                    Debug.Log("[UnbalancedAction] Phase: Done");
                    return TaskStatus.Success;
                    
                default:
                    Debug.LogError($"[UnbalancedAction] Invalid phase: {_currentPhase}");
                    return TaskStatus.Failure;
            }
        }
        
        /// <summary>
        /// Phase 2: Handle Waiting phase - wait for unbalanced duration
        /// </summary>
        private TaskStatus HandleWaitingPhase()
        {
            // Check if unbalanced duration has elapsed
            if (_phaseTimer >= _unbalancedDuration)
            {
                TransitionToCompleting("Duration elapsed");
                return TaskStatus.Running;
            }
            
            // Still waiting
            return TaskStatus.Running;
        }
        
        private void TransitionToCompleting(string reason)
        {
            _currentPhase = UnbalancedPhase.Completing;
            _phaseTimer = 0f;
            
            Debug.Log($"[UnbalancedAction] Phase: Waiting → Completing ({reason})");
            
            // Complete immediately
            CompleteUnbalanced();
        }
        
        /// <summary>
        /// Phase 3: Complete unbalanced state
        /// </summary>
        private void CompleteUnbalanced()
        {
            _currentPhase = UnbalancedPhase.Done;
            
            // Clean up animation state
            CleanupAnimation();
            
            // Notify controller to complete unbalanced state
            // This will restore balance to max
            Controller.CompleteUnbalanced();
            
            Debug.Log($"[UnbalancedAction] Phase: Completing → Done");
            Debug.Log($"[UnbalancedAction] Unbalanced completed successfully! Total time: {_globalTimer:F2}s");
        }
        
        private TaskStatus HandleCompletingPhase()
        {
            // Should transition to Done immediately in CompleteUnbalanced()
            // This is just a safety fallback
            _currentPhase = UnbalancedPhase.Done;
            return TaskStatus.Success;
        }
        
        /// <summary>
        /// Force complete all steps (used on global timeout)
        /// </summary>
        private void ForceComplete()
        {
            // Clean up animation
            CleanupAnimation();
            
            // Notify controller
            Controller.CompleteUnbalanced();
            
            _currentPhase = UnbalancedPhase.Done;
            Debug.LogError($"[UnbalancedAction] Force completed due to global timeout! Total time: {_globalTimer:F2}s");
        }
        
        /// <summary>
        /// Clean up animation state
        /// </summary>
        private void CleanupAnimation()
        {
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                Animator.SetBool("IsUnbalanced", false);
            }
        }

        public override void OnEnd()
        {
            // Clean up animation state
            CleanupAnimation();
            
            _phaseTimer = 0f;
            _globalTimer = 0f;
            
            Debug.Log($"[UnbalancedAction] OnEnd - Phase: {_currentPhase}, cleaned up");
        }
    }
}

