using UnityEngine;
using DG.Tweening;

namespace Resonance.Enemies.Data
{
    /// <summary>
    /// Enemy base stats data ScriptableObject
    /// Used to create and edit Enemy configurations in Unity Editor
    /// </summary>
    [CreateAssetMenu(fileName = "New Enemy Stats", menuName = "Wave/Enemies/Enemy Stats", order = 1)]
    public class EnemyBaseStats : ScriptableObject
    {
        [Header("Basic Info")]
        public string enemyName = "Basic Enemy";
        [TextArea(2, 4)]
        public string enemyDescription = "A basic enemy";
        
        [Header("Health System")]
        [Tooltip("Maximum physical health")]
        public float maxPhysicalHealth = 100f;
        [Tooltip("Maximum core health")]
        public float maxCoreHealth = 50f;
        
        [Header("Health Regeneration")]
        [Tooltip("Physical health regeneration rate per second (only when physically alive)")]
        public float physicalHealthRegenRate = 0f;
        [Tooltip("Core health regeneration rate per second (only in normal state)")]
        public float coreHealthRegenRate = 0f;
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
        [Tooltip("Core health threshold for critical state (0-1)")]
        public float coreCriticalThreshold = 0.4f;
        [Tooltip("Movement speed multiplier when wounded (physical health low)")]
        public float woundedSpeedMultiplier = 0.7f;
        [Tooltip("Physical damage multiplier when core health is critical")]
        public float criticalPhysicalDamageMultiplier = 1.5f;
        [Tooltip("Physical damage multiplier when core health is dead")]
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
        [Tooltip("DoTween ease curve type for QTE value animation in WavePanel")]
        public DG.Tweening.Ease qteEaseType = DG.Tweening.Ease.InOutSine;
        [Tooltip("QTE cycle duration in seconds")]
        public float qteCycleDuration = 3f;
        [Tooltip("QTE target window size (smaller = harder)")]
        [Range(0.05f, 0.5f)]
        public float qteTargetWindow = 0.2f;
        
        [Header("Loot System")]
        [Tooltip("Prefab to spawn when enemy dies (true death)")]
        public GameObject deathLootPrefab;
        [Tooltip("Number of loot items to spawn")]
        [Range(1, 5)]
        public int lootCount = 1;
        [Tooltip("Spawn radius for loot items")]
        public float lootSpawnRadius = 1.5f;
        [Tooltip("Chance to drop loot (0-1)")]
        [Range(0f, 1f)]
        public float lootDropChance = 1f;

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
                
                // Core Health
                maxCoreHealth = this.maxCoreHealth,
                currentCoreHealth = this.maxCoreHealth,
                
                // Regeneration
                physicalHealthRegenRate = this.physicalHealthRegenRate,
                coreHealthRegenRate = this.coreHealthRegenRate,
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
                coreCriticalThreshold = this.coreCriticalThreshold,
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
                
                // Loot System
                deathLootPrefab = this.deathLootPrefab,
                lootCount = this.lootCount,
                lootSpawnRadius = this.lootSpawnRadius,
                lootDropChance = this.lootDropChance,
                
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

            if (maxCoreHealth <= 0)
            {
                Debug.LogError($"EnemyBaseStats: {enemyName} has invalid max core health: {maxCoreHealth}");
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
            maxCoreHealth = Mathf.Max(1f, maxCoreHealth);
            physicalHealthRegenRate = Mathf.Max(0f, physicalHealthRegenRate);
            coreHealthRegenRate = Mathf.Max(0f, coreHealthRegenRate);
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
            coreCriticalThreshold = Mathf.Clamp01(coreCriticalThreshold);
            woundedSpeedMultiplier = Mathf.Max(0.1f, woundedSpeedMultiplier);
            criticalPhysicalDamageMultiplier = Mathf.Max(1f, criticalPhysicalDamageMultiplier);
            deadPhysicalDamageMultiplier = Mathf.Max(1f, deadPhysicalDamageMultiplier);
            
            revivalDelay = Mathf.Max(0f, revivalDelay);
            revivalDuration = Mathf.Max(0.1f, revivalDuration);
            damageFlashDuration = Mathf.Max(0.1f, damageFlashDuration);
            
            // Validate QTE configuration
            qteCycleDuration = Mathf.Max(0.5f, qteCycleDuration);
            qteTargetWindow = Mathf.Clamp(qteTargetWindow, 0.05f, 0.5f);
            
            // Validate Loot System configuration
            lootCount = Mathf.Clamp(lootCount, 1, 5);
            lootSpawnRadius = Mathf.Max(0.5f, lootSpawnRadius);
            lootDropChance = Mathf.Clamp01(lootDropChance);
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
        
        [Header("Core Health")]
        public float maxCoreHealth;
        public float currentCoreHealth;
        
        [Header("Regeneration")]
        public float physicalHealthRegenRate;
        public float coreHealthRegenRate;
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
        public float coreCriticalThreshold;
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
        
        [Header("Loot System")]
        public GameObject deathLootPrefab;
        public int lootCount;
        public float lootSpawnRadius;
        public float lootDropChance;
        
        [Header("Debug")]
        public bool showHealthBar;
        public bool showDetectionRange;
        public bool showAttackRange;

        [Header("Health Tiers")]
        public EnemyPhysicalHealthTier physicalTier;
        public EnemyCoreHealthTier coreTier;
        
        // Health Properties
        public bool IsAlive => currentPhysicalHealth > 0f;
        public bool IsCoreAlive => currentCoreHealth > 0f;
        public bool IsInPhysicalDeathState => currentPhysicalHealth <= 0f && currentCoreHealth > 0f;
        
        // Health Percentages
        public float PhysicalHealthPercentage => maxPhysicalHealth > 0 ? currentPhysicalHealth / maxPhysicalHealth : 0f;
        public float CoreHealthPercentage => maxCoreHealth > 0 ? currentCoreHealth / maxCoreHealth : 0f;

        /// <summary>
        /// Restore all health to full
        /// </summary>
        public void RestoreToFullHealth()
        {
            currentPhysicalHealth = maxPhysicalHealth;
            currentCoreHealth = maxCoreHealth;
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
        /// Restore core health to full
        /// </summary>
        public void RestoreCoreHealth()
        {
            currentCoreHealth = maxCoreHealth;
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
                
            // Core Tier calculation  
            if (currentCoreHealth <= 0f)
                coreTier = EnemyCoreHealthTier.Dead;
            else if (CoreHealthPercentage <= coreCriticalThreshold)
                coreTier = EnemyCoreHealthTier.Critical;
            else
                coreTier = EnemyCoreHealthTier.Healthy;
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
        /// Get physical damage multiplier based on core health tier
        /// </summary>
        public float GetPhysicalDamageMultiplier()
        {
            switch (coreTier)
            {
                case EnemyCoreHealthTier.Dead:
                    return deadPhysicalDamageMultiplier;
                case EnemyCoreHealthTier.Critical:
                    return criticalPhysicalDamageMultiplier;
                default:
                    return 1f;
            }
        }
    }
}
