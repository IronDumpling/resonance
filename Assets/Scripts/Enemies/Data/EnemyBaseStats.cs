using UnityEngine;
using DG.Tweening;

namespace Resonance.Enemies.Data
{
    /// <summary>
    /// Enemy base stats data ScriptableObject
    /// Used to create and edit Enemy configurations in Unity Editor
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
        public float moveSpeed = 1f;
        [Tooltip("Chase movement speed")]
        public float chaseMoveSpeed = 2f;
        [Tooltip("Patrol radius")]
        public float patrolRadius = 5f;
        [Tooltip("Distance threshold for considering 'arrived' at target (prevents collision issues)")]
        public float arrivalThreshold = 1.2f;
        
        [Header("Health Tiers")]
        [Tooltip("Physical health threshold for wounded state (0-1)")]
        public float physicalWoundedThreshold = 0.4f;
        [Tooltip("Mental health threshold for critical state (0-1)")]
        public float mentalCriticalThreshold = 0.4f;
        [Tooltip("Movement speed multiplier when wounded (physical health low)")]
        public float woundedSpeedMultiplier = 0.7f;
        [Tooltip("Physical damage multiplier when mental health is critical")]
        public float criticalPhysicalDamageMultiplier = 1.5f;
        [Tooltip("Physical damage multiplier when mental health is dead")]
        public float deadPhysicalDamageMultiplier = 2.0f;
        
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
        
        [Header("QTE Configuration")]
        [Tooltip("DoTween ease curve type for QTE value animation in ResonancePanel")]
        public DG.Tweening.Ease qteEaseType = DG.Tweening.Ease.InOutSine;
        [Tooltip("QTE cycle duration in seconds")]
        public float qteCycleDuration = 3f;
        [Tooltip("QTE target window size (smaller = harder)")]
        [Range(0.05f, 0.5f)]
        public float qteTargetWindow = 0.2f;
        
        [Header("Debug")]
        public bool showHealthBar = true;
        public bool showDetectionRange = false;
        public bool showAttackRange = false;

        /// <summary>
        /// Create runtime stats instance
        /// </summary>
        /// <returns>Runtime stats</returns>
        public EnemyRuntimeStats CreateRuntimeStats()
        {
            var stats = new EnemyRuntimeStats
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
                chaseMoveSpeed = this.chaseMoveSpeed,
                patrolRadius = this.patrolRadius,
                arrivalThreshold = this.arrivalThreshold,
                
                // Health Tiers
                physicalWoundedThreshold = this.physicalWoundedThreshold,
                mentalCriticalThreshold = this.mentalCriticalThreshold,
                woundedSpeedMultiplier = this.woundedSpeedMultiplier,
                criticalPhysicalDamageMultiplier = this.criticalPhysicalDamageMultiplier,
                deadPhysicalDamageMultiplier = this.deadPhysicalDamageMultiplier,
                
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
                
                // QTE Configuration
                qteEaseType = this.qteEaseType,
                qteCycleDuration = this.qteCycleDuration,
                qteTargetWindow = this.qteTargetWindow,
                
                // Debug
                showHealthBar = this.showHealthBar,
                showDetectionRange = this.showDetectionRange,
                showAttackRange = this.showAttackRange
            };
            
            // Initialize health tiers
            stats.UpdateHealthTiers();
            return stats;
        }

        /// <summary>
        /// Validate if Enemy data is valid
        /// </summary>
        /// <returns>Validation result</returns>
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
            // Ensure values are within reasonable ranges
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
            chaseMoveSpeed = Mathf.Max(0.1f, chaseMoveSpeed);
            patrolRadius = Mathf.Max(0f, patrolRadius);
            arrivalThreshold = Mathf.Max(0.1f, arrivalThreshold);
            
            // Validate health tier thresholds
            physicalWoundedThreshold = Mathf.Clamp01(physicalWoundedThreshold);
            mentalCriticalThreshold = Mathf.Clamp01(mentalCriticalThreshold);
            woundedSpeedMultiplier = Mathf.Max(0.1f, woundedSpeedMultiplier);
            criticalPhysicalDamageMultiplier = Mathf.Max(1f, criticalPhysicalDamageMultiplier);
            deadPhysicalDamageMultiplier = Mathf.Max(1f, deadPhysicalDamageMultiplier);
            
            revivalDelay = Mathf.Max(0f, revivalDelay);
            revivalDuration = Mathf.Max(0.1f, revivalDuration);
            damageFlashDuration = Mathf.Max(0.1f, damageFlashDuration);
            
            // Validate QTE configuration
            qteCycleDuration = Mathf.Max(0.5f, qteCycleDuration);
            qteTargetWindow = Mathf.Clamp(qteTargetWindow, 0.05f, 0.5f);
        }

        #endregion
    }

    /// <summary>
    /// Enemy runtime stats data
    /// Contains current state and variable data
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
        public float chaseMoveSpeed;
        public float patrolRadius;
        public float arrivalThreshold;
        
        [Header("Health Tiers")]
        public float physicalWoundedThreshold;
        public float mentalCriticalThreshold;
        public float woundedSpeedMultiplier;
        public float criticalPhysicalDamageMultiplier;
        public float deadPhysicalDamageMultiplier;
        
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
        
        [Header("QTE Configuration")]
        public DG.Tweening.Ease qteEaseType;
        public float qteCycleDuration;
        public float qteTargetWindow;
        
        [Header("Debug")]
        public bool showHealthBar;
        public bool showDetectionRange;
        public bool showAttackRange;

        [Header("Health Tiers")]
        public EnemyPhysicalHealthTier physicalTier;
        public EnemyMentalHealthTier mentalTier;
        
        // Health Properties
        public bool IsPhysicallyAlive => currentPhysicalHealth > 0f;
        public bool IsMentallyAlive => currentMentalHealth > 0f;
        public bool IsInPhysicalDeathState => currentPhysicalHealth <= 0f && currentMentalHealth > 0f;
        
        // Health Percentages
        public float PhysicalHealthPercentage => maxPhysicalHealth > 0 ? currentPhysicalHealth / maxPhysicalHealth : 0f;
        public float MentalHealthPercentage => maxMentalHealth > 0 ? currentMentalHealth / maxMentalHealth : 0f;

        /// <summary>
        /// Restore all health to full
        /// </summary>
        public void RestoreToFullHealth()
        {
            currentPhysicalHealth = maxPhysicalHealth;
            currentMentalHealth = maxMentalHealth;
            UpdateHealthTiers();
        }

        /// <summary>
        /// Restore physical health to full
        /// </summary>
        public void RestorePhysicalHealth()
        {
            currentPhysicalHealth = maxPhysicalHealth;
            UpdateHealthTiers();
        }

        /// <summary>
        /// Restore mental health to full
        /// </summary>
        public void RestoreMentalHealth()
        {
            currentMentalHealth = maxMentalHealth;
            UpdateHealthTiers();
        }
        
        /// <summary>
        /// Update health tiers based on current health values
        /// </summary>
        public void UpdateHealthTiers()
        {
            // Physical Tier calculation
            if (currentPhysicalHealth <= 0f)
                physicalTier = EnemyPhysicalHealthTier.Dead;
            else if (PhysicalHealthPercentage <= physicalWoundedThreshold)
                physicalTier = EnemyPhysicalHealthTier.Wounded;
            else
                physicalTier = EnemyPhysicalHealthTier.Healthy;
                
            // Mental Tier calculation  
            if (currentMentalHealth <= 0f)
                mentalTier = EnemyMentalHealthTier.Dead;
            else if (MentalHealthPercentage <= mentalCriticalThreshold)
                mentalTier = EnemyMentalHealthTier.Critical;
            else
                mentalTier = EnemyMentalHealthTier.Healthy;
        }
        
        /// <summary>
        /// Get current movement speed with health tier modifiers
        /// </summary>
        public float GetModifiedMoveSpeed()
        {
            if (physicalTier == EnemyPhysicalHealthTier.Dead)
                return 0f; // Cannot move when physically dead
            else if (physicalTier == EnemyPhysicalHealthTier.Wounded)
                return moveSpeed * woundedSpeedMultiplier;
            else
                return moveSpeed;
        }
        
        /// <summary>
        /// Get current chase move speed with health tier modifiers
        /// </summary>
        public float GetModifiedChaseMoveSpeed()
        {
            if (physicalTier == EnemyPhysicalHealthTier.Dead)
                return 0f; // Cannot move when physically dead
            else if (physicalTier == EnemyPhysicalHealthTier.Wounded)
                return chaseMoveSpeed * woundedSpeedMultiplier;
            else
                return chaseMoveSpeed;
        }
        
        /// <summary>
        /// Get physical damage multiplier based on mental health tier
        /// </summary>
        public float GetPhysicalDamageMultiplier()
        {
            switch (mentalTier)
            {
                case EnemyMentalHealthTier.Dead:
                    return deadPhysicalDamageMultiplier;
                case EnemyMentalHealthTier.Critical:
                    return criticalPhysicalDamageMultiplier;
                default:
                    return 1f;
            }
        }
    }
}
