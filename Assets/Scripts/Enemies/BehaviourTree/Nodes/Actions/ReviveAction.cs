using UnityEngine;
using Resonance.Enemies.Core;
using Resonance.Enemies.BehaviourTree.Base;

namespace Resonance.Enemies.BehaviourTree.Nodes.Actions
{
    /// <summary>
    /// Revive action node - handles the revival process when physical health reaches 0
    /// Restores physical health over time
    /// </summary>
    public class ReviveAction : ActionNode
    {
        private float _reviveTimer = 0f;
        private float _maxReviveTime;
        private bool _isActive = false;

        public override BTNodeStatus Execute()
        {
            // Check if can start revival
            if (!_isActive)
            {
                if (!CanStartRevival())
                {
                    return BTNodeStatus.Failure;
                }
                StartRevival();
            }

            _reviveTimer += Time.deltaTime;

            // Check if core health dropped to 0 during revival
            if (!Controller.IsCoreAlive)
            {
                Debug.Log("ReviveAction: Revival interrupted - core health reached 0");
                FinishRevival();
                return BTNodeStatus.Failure;
            }

            // Check if revival is complete (physical health restored)
            if (Controller.IsAlive)
            {
                Debug.Log("ReviveAction: Revival completed - physical health restored");
                FinishRevival();
                return BTNodeStatus.Success;
            }

            // Safety timeout
            if (_reviveTimer > _maxReviveTime)
            {
                Debug.LogWarning("ReviveAction: Revival timeout - forcing completion");
                Controller.Stats.FullRestore();
                FinishRevival();
                return BTNodeStatus.Success;
            }

            return BTNodeStatus.Running;
        }

        private bool CanStartRevival()
        {
            return !Controller.IsAlive &&
                   Controller.IsCoreAlive &&
                   Controller.CurrentState == EnemyState.Reviving;
        }

        private void StartRevival()
        {
            _isActive = true;
            _reviveTimer = 0f;
            _maxReviveTime = 3f * Controller.Stats.maxHealth / Controller.Stats.revivalRate;

            Debug.Log("ReviveAction: Started revival process");

            // Stop all movement and behaviors
            Controller.StopPatrol();
            Controller.LosePlayer();
        }

        private void FinishRevival()
        {
            _isActive = false;
        }
    }
}
