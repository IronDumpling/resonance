using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;

namespace Resonance.Enemies.BTNodes.Actions
{
    /// <summary>
    /// Revive action node - handles the revival process when physical health reaches 0
    /// 
    /// Architecture:
    /// - BehaviorTree (this node): Controls the revival flow and coordinates all phases
    /// - EnemyController: Provides data queries and restores health (no flow control)
    /// - EnemyAnimator: Plays animations and provides synchronization events
    /// - EnemyState: Calculates state based on health values
    /// 
    /// Revival Flow:
    /// 1. Initializing: Stop behaviors, reset flags, notify controller
    /// 2. Starting: Play start animation, wait for animation event
    /// 3. Recovering: Wait for health restoration (handled by Controller.UpdateRevivalTimer)
    /// 4. Completing: Play complete animation, wait for animation event
    /// 5. Done: Clean up and return Success
    /// 
    /// Timeout Calculation:
    /// - Start/Complete phase timeouts: Calculated from animation clip lengths
    /// - Recovery phase timeout: Calculated from revivalDelay + maxHealth / revivalRate
    /// - Global timeout: Sum of all phase timeouts + safety margin
    /// </summary>
    [TaskCategory("Resonance/Enemy")]
    [TaskDescription("Revives the enemy when physical health depletes but core is intact")]
    public class ReviveAction : EnemyActionBase
    {
        /// <summary>
        /// Revival phase state machine
        /// </summary>
        private enum RevivePhase
        {
            Initializing,   // Initial setup, disable enemy behaviors
            Starting,       // Playing start animation, waiting for animation event
            Recovering,     // Restoring health (EnemyController handles this)
            Completing,     // Playing complete animation
            Done            // Revival complete
        }
        
        // Phase tracking
        private RevivePhase _currentPhase;
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
            _currentPhase = RevivePhase.Initializing;
            _phaseTimer = 0f;
            _globalTimer = 0f;
            
            // Execute initialization immediately
            InitializeRevival();
        }
        
        /// <summary>
        /// Calculate timeout values dynamically based on enemy stats and animation lengths
        /// </summary>
        private void CalculateTimeouts()
        {
            var stats = Controller.Stats;
            
            // Start phase timeout: Get from animation clip length + margin
            _startPhaseTimeout = GetAnimationLength("EnemyReviveStart") + PHASE_TIMEOUT_MARGIN;
            if (_startPhaseTimeout <= PHASE_TIMEOUT_MARGIN)
            {
                _startPhaseTimeout = 2f; // Fallback default
                Debug.LogWarning("[ReviveAction] Could not get EnemyReviveStart animation length, using default");
            }
            
            // Recovery phase timeout: Based on revival rate calculation
            if (stats.revivalRate > 0f)
            {
                _recoveryPhaseTimeout = stats.revivalDelay + (stats.maxHealth / stats.revivalRate) + PHASE_TIMEOUT_MARGIN;
            }
            else
            {
                _recoveryPhaseTimeout = 5f; // Fallback if revival rate is 0
                Debug.LogWarning("[ReviveAction] Revival rate is 0, using default recovery timeout");
            }
            
            // Complete phase timeout: Get from animation clip length + margin
            _completePhaseTimeout = GetAnimationLength("EnemyReviveComplete") + PHASE_TIMEOUT_MARGIN;
            if (_completePhaseTimeout <= PHASE_TIMEOUT_MARGIN)
            {
                _completePhaseTimeout = 2f; // Fallback default
                Debug.LogWarning("[ReviveAction] Could not get EnemyReviveComplete animation length, using default");
            }
            
            // Global timeout: Sum of all phases + global margin
            _globalTimeout = _startPhaseTimeout + _recoveryPhaseTimeout + _completePhaseTimeout + GLOBAL_TIMEOUT_MARGIN;
            
            Debug.Log($"[ReviveAction] Calculated timeouts - Start: {_startPhaseTimeout:F2}s, " +
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
        /// Phase 1: Initialize revival process
        /// </summary>
        private void InitializeRevival()
        {
            // Stop all behaviors
            Controller.StopPatrol();
            Controller.LosePlayer();
            Movement?.Stop();
            
            // Reset animation flags (use EnemyAnimator for custom methods)
            if (EnemyAnimator != null)
            {
                EnemyAnimator.ResetRevivalFlags();
            }
            
            // Notify controller to start revival (triggers events, no flow control)
            Controller.StartRevival();
            
            // Transition to Starting phase
            _currentPhase = RevivePhase.Starting;
            _phaseTimer = 0f;
            
            // Trigger start animation (use Unity Animator for animation control)
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                // First trigger PhysicalDeath to exit from NormalSM sub-state machine
                Animator.SetTrigger("PhysicalDeath");
                
                // Then set revival parameters
                Animator.SetBool("IsReviving", true);
                Animator.SetFloat("Speed", 0f);
                Animator.SetBool("HasTarget", false);
                Animator.SetBool("InAttackRange", false);
            }
            
            Debug.Log("[ReviveAction] Phase: Initializing → Starting");
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
                Debug.LogWarning($"[ReviveAction] Global timeout! Forcing completion. Timer: {_globalTimer:F2}s / {_globalTimeout:F2}s");
                ForceComplete();
                return TaskStatus.Success;
            }
            
            // Phase-based processing
            switch (_currentPhase)
            {
                case RevivePhase.Starting:
                    return HandleStartingPhase();
                    
                case RevivePhase.Recovering:
                    return HandleRecoveringPhase();
                    
                case RevivePhase.Completing:
                    return HandleCompletingPhase();
                    
                case RevivePhase.Done:
                    Debug.Log("[ReviveAction] Phase: Done");
                    return TaskStatus.Success;
                    
                default:
                    Debug.LogError($"[ReviveAction] Invalid phase: {_currentPhase}");
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
                Debug.LogWarning($"[ReviveAction] Starting phase timeout! Timer: {_phaseTimer:F2}s / {_startPhaseTimeout:F2}s");
                TransitionToRecovering("Timeout");
                return TaskStatus.Running;
            }
            
            return TaskStatus.Running;
        }
        
        private void TransitionToRecovering(string reason)
        {
            _currentPhase = RevivePhase.Recovering;
            _phaseTimer = 0f;
            Debug.Log($"[ReviveAction] Phase: Starting → Recovering ({reason})");
        }
        
        /// <summary>
        /// Phase 3: Handle Recovering phase - wait for health restoration
        /// Controller.UpdateRevivalTimer() handles the actual health restoration
        /// </summary>
        private TaskStatus HandleRecoveringPhase()
        {
            // Check if health is fully restored (must reach max health, not just > 0)
            if (Controller.Stats.currentHealth >= Controller.Stats.maxHealth)
            {
                TransitionToCompleting("Health fully restored");
                return TaskStatus.Running;
            }
            
            // Phase timeout protection
            if (_phaseTimer >= _recoveryPhaseTimeout)
            {
                Debug.LogWarning($"[ReviveAction] Recovering phase timeout! Timer: {_phaseTimer:F2}s / {_recoveryPhaseTimeout:F2}s, " +
                               $"Health: {Controller.Stats.currentHealth:F1}/{Controller.Stats.maxHealth:F1}, " +
                               $"RevivalRate: {Controller.Stats.revivalRate:F1}");
                
                // Force restoration
                Controller.Stats.FullRestore();
                TransitionToCompleting("Timeout - forced restoration");
                return TaskStatus.Running;
            }
            
            return TaskStatus.Running;
        }
        
        private void TransitionToCompleting(string reason)
        {
            _currentPhase = RevivePhase.Completing;
            _phaseTimer = 0f;
            
            // Trigger complete animation
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                Animator.SetTrigger("ReviveComplete");
            }
            
            Debug.Log($"[ReviveAction] Phase: Recovering → Completing ({reason})");
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
                Debug.LogWarning($"[ReviveAction] Completing phase timeout! Timer: {_phaseTimer:F2}s / {_completePhaseTimeout:F2}s");
                TransitionToDone("Timeout");
                return TaskStatus.Success;
            }
            
            return TaskStatus.Running;
        }
        
        private void TransitionToDone(string reason)
        {
            _currentPhase = RevivePhase.Done;
            
            // Clean up animation state
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                Animator.SetBool("IsReviving", false);
            }
            
            // Notify controller to complete revival
            Controller.CompleteRevival();
            
            Debug.Log($"[ReviveAction] Phase: Completing → Done ({reason})");
            Debug.Log($"[ReviveAction] Revival completed successfully! Total time: {_globalTimer:F2}s");
        }
        
        /// <summary>
        /// Force complete all steps (used on global timeout)
        /// </summary>
        private void ForceComplete()
        {
            // Force restore full health
            Controller.Stats.FullRestore();
            
            // Clean up animation
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                Animator.SetBool("IsReviving", false);
            }
            
            // Notify controller
            Controller.CompleteRevival();
            
            _currentPhase = RevivePhase.Done;
            Debug.LogError($"[ReviveAction] Force completed due to global timeout! Total time: {_globalTimer:F2}s");
        }

        public override void OnEnd()
        {
            // Clean up animation state
            if (Animator != null && Animator.isActiveAndEnabled)
            {
                Animator.SetBool("IsReviving", false);
            }
            
            _phaseTimer = 0f;
            _globalTimer = 0f;
            
            Debug.Log($"[ReviveAction] OnEnd - Phase: {_currentPhase}, cleaned up");
        }
    }
}
