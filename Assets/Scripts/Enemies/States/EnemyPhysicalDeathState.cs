using UnityEngine;
using Resonance.Core;
using Resonance.Enemies.Core;

namespace Resonance.Enemies.States
{
    /// <summary>
    /// Enemy物理死亡状态，物理血量归零时进入
    /// 停止所有AI行为，暴露核心，等待复活
    /// </summary>
    public class EnemyPhysicalDeathState : IState
    {
        private EnemyController _enemyController;
        private float _deathTimer = 0f;
        private bool _revivalStarted = false;
        
        public string Name => "PhysicalDeath";

        public EnemyPhysicalDeathState(EnemyController enemyController)
        {
            _enemyController = enemyController;
        }

        public void Enter()
        {
            Debug.Log("EnemyState: Entered Physical Death state - core exposed");
            
            _deathTimer = 0f;
            _revivalStarted = false;
            
            // Stop all movement and AI behaviors
            _enemyController.StopPatrol();
            _enemyController.LosePlayer();
            
            // TODO: Visual effects for core exposure
            // TODO: Play death audio
            
            Debug.Log("EnemyState: Core exposed - vulnerable to mental attacks");
        }

        public void Update()
        {
            _deathTimer += Time.deltaTime;
            
            // Start revival after delay
            if (!_revivalStarted && _deathTimer >= _enemyController.Stats.revivalDelay)
            {
                StartRevival();
            }
        }

        public void Exit()
        {
            Debug.Log("EnemyState: Exited Physical Death state");
        }

        public bool CanTransitionTo(IState newState)
        {
            // Can transition to:
            // - Reviving (automatic after delay)
            // - TrueDeath (when mental health reaches 0)
            return newState.Name == "Reviving" || newState.Name == "TrueDeath";
        }

        /// <summary>
        /// Start the revival process
        /// </summary>
        private void StartRevival()
        {
            _revivalStarted = true;
            _enemyController.StartRevival();
            Debug.Log("EnemyPhysicalDeathState: Starting revival process");
        }

        /// <summary>
        /// Get time remaining until revival starts
        /// </summary>
        public float GetRevivalDelayRemaining()
        {
            return Mathf.Max(0f, _enemyController.Stats.revivalDelay - _deathTimer);
        }

        /// <summary>
        /// Check if revival has started
        /// </summary>
        public bool HasRevivalStarted()
        {
            return _revivalStarted;
        }
    }
}
