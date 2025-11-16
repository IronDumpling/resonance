using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Actions
{
    /// <summary>
    /// Core exposed action node - handles the core exposed state after player wave execution
    /// 
    /// Architecture:
    /// - BehaviorTree (this node): Controls the core exposed flow and coordinates all phases
    /// - EnemyController: Provides data queries and manages balance recovery (no flow control)
    /// - EnemyAnimator: Plays animations and provides synchronization events
    /// - EnemyState: Calculates state based on balance values
    /// 
    /// Core Exposed Flow:
    /// 1. Initializing: Stop behaviors, reset flags, notify controller
    /// 2. Starting: Play start animation, wait for animation event
    /// 3. Recovering: Wait for balance restoration (handled by Controller.UpdateBalanceRecovery)
    /// 4. Completing: Play complete animation, wait for animation event
    /// 5. Done: Clean up and return Success
    /// 
    /// Timeout Calculation:
    /// - Start/Complete phase timeouts: Calculated from animation clip lengths
    /// - Recovery phase timeout: Calculated from maxBalance / balanceRecoveryRateInCoreExposed
    /// - Global timeout: Sum of all phase timeouts + safety margin
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Handles core exposed state after player wave execution")]
    public class CoreExposedAction : EnemyActionBase
    {
        /// <summary>
        /// Core exposed phase state machine
        /// </summary>
        private enum CoreExposedPhase
        {
            Initializing,   // Initial setup, disable enemy behaviors
            Starting,       // Playing start animation, waiting for animation event
            Recovering,     // Restoring balance (EnemyController handles this)
            Completing,     // Playing complete animation
            Done            // Core exposed complete
        }
        
        // Phase tracking
        private CoreExposedPhase _currentPhase;
        private float _phaseTimer;
        private float _globalTimer;
        
        // Dynamic timeout configuration (calculated in OnStart)
        private float _startPhaseTimeout;
        private float _recoveryPhaseTimeout;
        private float _completePhaseTimeout;
        private float _globalTimeout;
        
        // Safety margins
        private const float PHASE_TIMEOUT_MARGIN = 0.5f;  // Extra time for each animation phase
        private const float GLOBAL_TIMEOUT_MARGIN = 2f;    // Extra time for global timeout

        public override void OnStart()
        {
            base.OnStart();
            
            if (!ValidateComponents())
            {
                return;
            }
            
            // Calculate dynamic timeouts based on stats and animation lengths
            CalculateTimeouts();
            
            // Initialize phase state
            _currentPhase = CoreExposedPhase.Initializing;
            _phaseTimer = 0f;
            _globalTimer = 0f;
            
            // Execute initialization immediately
            InitializeCoreExposure();
        }
        
        /// <summary>
        /// Calculate timeout values dynamically based on enemy stats and animation lengths
        /// </summary>
        private void CalculateTimeouts()
        {
            var stats = Controller.Stats;
            
            // Start phase timeout: Get from animation clip length + margin
            _startPhaseTimeout = GetAnimationLength("EnemyCoreExposedStart") + PHASE_TIMEOUT_MARGIN;
            if (_startPhaseTimeout <= PHASE_TIMEOUT_MARGIN)
            {
                // Try fallback to old revival animation names for backward compatibility
                _startPhaseTimeout = GetAnimationLength("EnemyReviveStart") + PHASE_TIMEOUT_MARGIN;
                if (_startPhaseTimeout <= PHASE_TIMEOUT_MARGIN)
                {
                    _startPhaseTimeout = 2f; // Fallback default
                    Debug.LogWarning("[CoreExposedAction] Could not get start animation length, using default");
                }
            }
            
            // Recovery phase timeout: Based on balance recovery rate in core exposed state
            if (stats.balanceRecoveryRateInCoreExposed > 0f)
            {
                _recoveryPhaseTimeout = (stats.maxBalance / stats.balanceRecoveryRateInCoreExposed) + PHASE_TIMEOUT_MARGIN;
            }
            else
            {
                _recoveryPhaseTimeout = 5f; // Fallback if recovery rate is 0
                Debug.LogWarning("[CoreExposedAction] Balance recovery rate is 0, using default recovery timeout");
            }
            
            // Complete phase timeout: Get from animation clip length + margin
            _completePhaseTimeout = GetAnimationLength("EnemyCoreExposedComplete") + PHASE_TIMEOUT_MARGIN;
            if (_completePhaseTimeout <= PHASE_TIMEOUT_MARGIN)
            {
                // Try fallback to old revival animation names for backward compatibility
                _completePhaseTimeout = GetAnimationLength("EnemyReviveComplete") + PHASE_TIMEOUT_MARGIN;
                if (_completePhaseTimeout <= PHASE_TIMEOUT_MARGIN)
                {
                    _completePhaseTimeout = 2f; // Fallback default
                    Debug.LogWarning("[CoreExposedAction] Could not get complete animation length, using default");
                }
            }
            
            // Global timeout: Sum of all phases + global margin
            _globalTimeout = _startPhaseTimeout + _recoveryPhaseTimeout + _completePhaseTimeout + GLOBAL_TIMEOUT_MARGIN;
            
            Debug.Log($"[CoreExposedAction] Calculated timeouts - Start: {_startPhaseTimeout:F2}s, " +
                     $"Recovery: {_recoveryPhaseTimeout:F2}s, Complete: {_completePhaseTimeout:F2}s, " +
                     $"Global: {_globalTimeout:F2}s");
        }
        
        /// <summary>
        /// Get animation clip length by name from the animator
        /// </summary>
        private float GetAnimationLength(string clipName)
        {
            if (Animator == null || !Animator.isActiveAndEnabled)
            {
                return 0f;
            }
            
            // Get all animation clips from the animator
            AnimationClip[] clips = Animator.runtimeAnimatorController.animationClips;
            
            foreach (var clip in clips)
            {
                if (clip.name == clipName)
                {
                    return clip.length;
                }
            }
            
            return 0f;
        }
        
        /// <summary>
        /// Phase 1: Initialize core exposure process
        /// </summary>
        private void InitializeCoreExposure()
        {
            // Stop all behaviors
            Controller.StopPatrol();
            Controller.LosePlayer();
            Movement?.Stop();
            
            // Reset animation flags (use EnemyAnimator for custom methods)
            if (EnemyAnimator != null)
            {
                EnemyAnimator.ResetRevivalFlags(); // Reuse revival flags for backward compatibility
            }
            
            // Notify controller to start core exposure (triggers events, no flow control)
            Controller.StartCoreExposure();
            
            // Transition to Starting phase
            _currentPhase = CoreExposedPhase.Starting;
            _phaseTimer = 0f;
            
            // Trigger start animation (use Unity Animator for animation control)
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                // Set core exposed parameters (reuse revival animations for backward compatibility)
                Animator.SetBool("IsCoreExposed", true);
                Animator.SetBool("IsReviving", true); // Backward compatibility
                Animator.SetFloat("Speed", 0f);
                Animator.SetBool("HasTarget", false);
                Animator.SetBool("InAttackRange", false);
            }
            
            Debug.Log("[CoreExposedAction] Phase: Initializing → Starting");
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
            if (_globalTimer > _globalTimeout)
            {
                Debug.LogWarning($"[CoreExposedAction] Global timeout! Forcing completion. Timer: {_globalTimer:F2}s / {_globalTimeout:F2}s");
                ForceComplete();
                return TaskStatus.Success;
            }
            
            // Phase-based processing
            switch (_currentPhase)
            {
                case CoreExposedPhase.Starting:
                    return HandleStartingPhase();
                    
                case CoreExposedPhase.Recovering:
                    return HandleRecoveringPhase();
                    
                case CoreExposedPhase.Completing:
                    return HandleCompletingPhase();
                    
                case CoreExposedPhase.Done:
                    Debug.Log("[CoreExposedAction] Phase: Done");
                    return TaskStatus.Success;
                    
                default:
                    Debug.LogError($"[CoreExposedAction] Invalid phase: {_currentPhase}");
                    return TaskStatus.Failure;
            }
        }
        
        /// <summary>
        /// Phase 2: Handle Starting phase - wait for start animation
        /// </summary>
        private TaskStatus HandleStartingPhase()
        {
            // Priority: Check animation event (use EnemyAnimator for custom properties)
            if (EnemyAnimator != null && EnemyAnimator.IsRevivalStartTriggered)
            {
                TransitionToRecovering("Animation event received");
                return TaskStatus.Running;
            }
            
            // Phase timeout protection
            if (_phaseTimer >= _startPhaseTimeout)
            {
                Debug.LogWarning($"[CoreExposedAction] Starting phase timeout! Timer: {_phaseTimer:F2}s / {_startPhaseTimeout:F2}s");
                TransitionToRecovering("Timeout");
                return TaskStatus.Running;
            }
            
            return TaskStatus.Running;
        }
        
        private void TransitionToRecovering(string reason)
        {
            _currentPhase = CoreExposedPhase.Recovering;
            _phaseTimer = 0f;
            Debug.Log($"[CoreExposedAction] Phase: Starting → Recovering ({reason})");
        }
        
        /// <summary>
        /// Phase 3: Handle Recovering phase - wait for balance restoration
        /// Controller.UpdateBalanceRecovery() handles the actual balance restoration
        /// </summary>
        private TaskStatus HandleRecoveringPhase()
        {
            // Check if balance is fully restored (must reach max balance, not just > 0)
            if (Controller.Stats.currentBalance >= Controller.Stats.maxBalance)
            {
                TransitionToCompleting("Balance fully restored");
                return TaskStatus.Running;
            }
            
            // Phase timeout protection
            if (_phaseTimer >= _recoveryPhaseTimeout)
            {
                Debug.LogWarning($"[CoreExposedAction] Recovering phase timeout! Timer: {_phaseTimer:F2}s / {_recoveryPhaseTimeout:F2}s, " +
                               $"Balance: {Controller.Stats.currentBalance:F1}/{Controller.Stats.maxBalance:F1}, " +
                               $"RecoveryRate: {Controller.Stats.balanceRecoveryRateInCoreExposed:F1}");
                
                // Force restoration
                Controller.Stats.currentBalance = Controller.Stats.maxBalance;
                Controller.Stats.UpdateBalanceTier();
                TransitionToCompleting("Timeout - forced restoration");
                return TaskStatus.Running;
            }
            
            return TaskStatus.Running;
        }
        
        private void TransitionToCompleting(string reason)
        {
            _currentPhase = CoreExposedPhase.Completing;
            _phaseTimer = 0f;
            
            // Trigger complete animation
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                Animator.SetTrigger("CoreExposedComplete");
                Animator.SetTrigger("ReviveComplete"); // Backward compatibility
            }
            
            Debug.Log($"[CoreExposedAction] Phase: Recovering → Completing ({reason})");
        }
        
        /// <summary>
        /// Phase 4: Handle Completing phase - wait for complete animation
        /// </summary>
        private TaskStatus HandleCompletingPhase()
        {
            // Priority: Check animation event (use EnemyAnimator for custom properties)
            if (EnemyAnimator != null && EnemyAnimator.IsRevivalCompleteTriggered)
            {
                TransitionToDone("Animation complete event received");
                return TaskStatus.Success;
            }
            
            // Phase timeout protection
            if (_phaseTimer >= _completePhaseTimeout)
            {
                Debug.LogWarning($"[CoreExposedAction] Completing phase timeout! Timer: {_phaseTimer:F2}s / {_completePhaseTimeout:F2}s");
                TransitionToDone("Timeout");
                return TaskStatus.Success;
            }
            
            return TaskStatus.Running;
        }
        
        private void TransitionToDone(string reason)
        {
            _currentPhase = CoreExposedPhase.Done;
            
            // Clean up animation state
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                Animator.SetBool("IsCoreExposed", false);
                Animator.SetBool("IsReviving", false); // Backward compatibility
            }
            
            // Notify controller to complete core exposure
            Controller.CompleteCoreExposure();
            
            Debug.Log($"[CoreExposedAction] Phase: Completing → Done ({reason})");
            Debug.Log($"[CoreExposedAction] Core exposure completed successfully! Total time: {_globalTimer:F2}s");
        }
        
        /// <summary>
        /// Force complete all steps (used on global timeout)
        /// </summary>
        private void ForceComplete()
        {
            // Force restore full balance
            Controller.Stats.currentBalance = Controller.Stats.maxBalance;
            Controller.Stats.UpdateBalanceTier();
            
            // Clean up animation
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                Animator.SetBool("IsCoreExposed", false);
                Animator.SetBool("IsReviving", false);
            }
            
            // Notify controller
            Controller.CompleteCoreExposure();
            
            _currentPhase = CoreExposedPhase.Done;
            Debug.LogError($"[CoreExposedAction] Force completed due to global timeout! Total time: {_globalTimer:F2}s");
        }

        public override void OnEnd()
        {
            // Clean up animation state
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                Animator.SetBool("IsCoreExposed", false);
                Animator.SetBool("IsReviving", false);
            }
            
            _phaseTimer = 0f;
            _globalTimer = 0f;
            
            Debug.Log($"[CoreExposedAction] OnEnd - Phase: {_currentPhase}, cleaned up");
        }
    }
}

