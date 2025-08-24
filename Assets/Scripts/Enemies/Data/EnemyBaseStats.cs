using UnityEngine;

namespace Resonance.Enemies.Data
{
    /// <summary>
    /// Enemy基础属性数据的ScriptableObject
    /// 用于在Unity Editor中创建和编辑Enemy配置
    /// </summary>
    [CreateAssetMenu(fileName = "New Enemy Stats", menuName = "Resonance/Enemies/Enemy Stats", order = 1)]
    public class EnemyBaseStats : ScriptableObject
    {
        [Header("Basic Info")]
        public string enemyName = "Basic Enemy";
        [TextArea(2, 4)]
        public string enemyDescription = "A basic enemy";
        
        [Header("Health System")]
        [Tooltip("Maximum physical health")]
        public float maxPhysicalHealth = 100f;
        [Tooltip("Maximum mental health")]
        public float maxMentalHealth = 50f;
        
        [Header("Health Regeneration")]
        [Tooltip("Physical health regeneration rate per second (only when physically alive)")]
        public float physicalHealthRegenRate = 0f;
        [Tooltip("Mental health regeneration rate per second (only in normal state)")]
        public float mentalHealthRegenRate = 0f;
        [Tooltip("Physical health revival rate per second (during revival state)")]
        public float revivalRate = 10f;
        
        [Header("Combat Stats")]
        [Tooltip("Damage dealt to player")]
        public float attackDamage = 20f;
        [Tooltip("Attack cooldown in seconds")]
        public float attackCooldown = 2f;
        [Tooltip("Attack range")]
        public float attackRange = 3f;
        [Tooltip("Detection range for player")]
        public float detectionRange = 8f;
        
        [Header("Movement")]
        [Tooltip("Normal movement speed")]
        public float moveSpeed = 3f;
        [Tooltip("Alert movement speed")]
        public float alertMoveSpeed = 5f;
        [Tooltip("Patrol radius")]
        public float patrolRadius = 5f;
        
        [Header("Revival System")]
        [Tooltip("Time to wait before starting revival")]
        public float revivalDelay = 2f;
        [Tooltip("Time to complete revival")]
        public float revivalDuration = 5f;
        
        [Header("Visual")]
        public string normalMaterialPath = "Art/Materials/Enemy_Body";
        public string damageMaterialPath = "Art/Materials/Damage_Body";
        public string revivalMaterialPath = "Art/Materials/Revival_Body";
        public float damageFlashDuration = 0.2f;
        
        [Header("Audio")]
        public bool enableAudio = true;
        
        [Header("Debug")]
        public bool showHealthBar = true;
        public bool showDetectionRange = false;
        public bool showAttackRange = false;

        /// <summary>
        /// 创建运行时属性实例
        /// </summary>
        /// <returns>运行时属性</returns>
        public EnemyRuntimeStats CreateRuntimeStats()
        {
            return new EnemyRuntimeStats
            {
                // Physical Health
                maxPhysicalHealth = this.maxPhysicalHealth,
                currentPhysicalHealth = this.maxPhysicalHealth,
                
                // Mental Health
                maxMentalHealth = this.maxMentalHealth,
                currentMentalHealth = this.maxMentalHealth,
                
                // Regeneration
                physicalHealthRegenRate = this.physicalHealthRegenRate,
                mentalHealthRegenRate = this.mentalHealthRegenRate,
                revivalRate = this.revivalRate,
                
                // Combat
                attackDamage = this.attackDamage,
                attackCooldown = this.attackCooldown,
                attackRange = this.attackRange,
                detectionRange = this.detectionRange,
                
                // Movement
                moveSpeed = this.moveSpeed,
                alertMoveSpeed = this.alertMoveSpeed,
                patrolRadius = this.patrolRadius,
                
                // Revival
                revivalDelay = this.revivalDelay,
                revivalDuration = this.revivalDuration,
                
                // Visual
                normalMaterialPath = this.normalMaterialPath,
                damageMaterialPath = this.damageMaterialPath,
                revivalMaterialPath = this.revivalMaterialPath,
                damageFlashDuration = this.damageFlashDuration,
                
                // Audio
                enableAudio = this.enableAudio,
                
                // Debug
                showHealthBar = this.showHealthBar,
                showDetectionRange = this.showDetectionRange,
                showAttackRange = this.showAttackRange
            };
        }

        /// <summary>
        /// 验证Enemy数据是否有效
        /// </summary>
        /// <returns>验证结果</returns>
        public bool ValidateData()
        {
            if (string.IsNullOrEmpty(enemyName))
            {
                Debug.LogError($"EnemyBaseStats: {name} has empty enemy name");
                return false;
            }

            if (maxPhysicalHealth <= 0)
            {
                Debug.LogError($"EnemyBaseStats: {enemyName} has invalid max physical health: {maxPhysicalHealth}");
                return false;
            }

            if (maxMentalHealth <= 0)
            {
                Debug.LogError($"EnemyBaseStats: {enemyName} has invalid max mental health: {maxMentalHealth}");
                return false;
            }

            if (attackDamage <= 0)
            {
                Debug.LogError($"EnemyBaseStats: {enemyName} has invalid attack damage: {attackDamage}");
                return false;
            }

            if (moveSpeed <= 0)
            {
                Debug.LogError($"EnemyBaseStats: {enemyName} has invalid move speed: {moveSpeed}");
                return false;
            }

            return true;
        }

        #region Unity Editor

        void OnValidate()
        {
            // 确保数值在合理范围内
            maxPhysicalHealth = Mathf.Max(1f, maxPhysicalHealth);
            maxMentalHealth = Mathf.Max(1f, maxMentalHealth);
            physicalHealthRegenRate = Mathf.Max(0f, physicalHealthRegenRate);
            mentalHealthRegenRate = Mathf.Max(0f, mentalHealthRegenRate);
            revivalRate = Mathf.Max(0.1f, revivalRate);
            attackDamage = Mathf.Max(0.1f, attackDamage);
            attackCooldown = Mathf.Max(0.1f, attackCooldown);
            attackRange = Mathf.Max(0.1f, attackRange);
            detectionRange = Mathf.Max(0.1f, detectionRange);
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            alertMoveSpeed = Mathf.Max(0.1f, alertMoveSpeed);
            patrolRadius = Mathf.Max(0f, patrolRadius);
            revivalDelay = Mathf.Max(0f, revivalDelay);
            revivalDuration = Mathf.Max(0.1f, revivalDuration);
            damageFlashDuration = Mathf.Max(0.1f, damageFlashDuration);
        }

        #endregion
    }

    /// <summary>
    /// Enemy运行时属性数据
    /// 包含当前状态和可变数据
    /// </summary>
    [System.Serializable]
    public class EnemyRuntimeStats
    {
        [Header("Physical Health")]
        public float maxPhysicalHealth;
        public float currentPhysicalHealth;
        
        [Header("Mental Health")]
        public float maxMentalHealth;
        public float currentMentalHealth;
        
        [Header("Regeneration")]
        public float physicalHealthRegenRate;
        public float mentalHealthRegenRate;
        public float revivalRate;
        
        [Header("Combat")]
        public float attackDamage;
        public float attackCooldown;
        public float attackRange;
        public float detectionRange;
        
        [Header("Movement")]
        public float moveSpeed;
        public float alertMoveSpeed;
        public float patrolRadius;
        
        [Header("Revival")]
        public float revivalDelay;
        public float revivalDuration;
        
        [Header("Visual")]
        public string normalMaterialPath;
        public string damageMaterialPath;
        public string revivalMaterialPath;
        public float damageFlashDuration;
        
        [Header("Audio")]
        public bool enableAudio;
        
        [Header("Debug")]
        public bool showHealthBar;
        public bool showDetectionRange;
        public bool showAttackRange;

        // Health Properties
        public bool IsPhysicallyAlive => currentPhysicalHealth > 0f;
        public bool IsMentallyAlive => currentMentalHealth > 0f;
        public bool IsInPhysicalDeathState => currentPhysicalHealth <= 0f && currentMentalHealth > 0f;
        
        // Health Percentages
        public float PhysicalHealthPercentage => maxPhysicalHealth > 0 ? currentPhysicalHealth / maxPhysicalHealth : 0f;
        public float MentalHealthPercentage => maxMentalHealth > 0 ? currentMentalHealth / maxMentalHealth : 0f;

        /// <summary>
        /// 恢复所有血量到满血
        /// </summary>
        public void RestoreToFullHealth()
        {
            currentPhysicalHealth = maxPhysicalHealth;
            currentMentalHealth = maxMentalHealth;
        }

        /// <summary>
        /// 恢复物理血量到满血
        /// </summary>
        public void RestorePhysicalHealth()
        {
            currentPhysicalHealth = maxPhysicalHealth;
        }

        /// <summary>
        /// 恢复精神血量到满血
        /// </summary>
        public void RestoreMentalHealth()
        {
            currentMentalHealth = maxMentalHealth;
        }
    }
}
