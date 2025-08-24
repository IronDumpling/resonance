using UnityEngine;
using Resonance.Enemies.Core;
using Resonance.Enemies.Data;
using Resonance.Interfaces;
using Resonance.Interfaces.Services;
using Resonance.Utilities;
using System.Collections;

namespace Resonance.Enemies
{
    /// <summary>
    /// MonoBehaviour component that handles Unity-specific enemy functionality.
    /// Acts as a bridge between Unity's GameObject system and the enemy logic.
    /// Implements IDamageable interface for damage handling.
    /// </summary>
    public class EnemyMonoBehaviour : MonoBehaviour, IDamageable
    {
        [Header("Enemy Configuration")]
        [SerializeField] private EnemyBaseStats _baseStats;

        [Header("Visual Feedback")]
        [SerializeField] private Transform _visualTransform;
        [SerializeField] private Renderer _bodyRenderer;

        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;

        // Core Components
        private EnemyController _enemyController;
        private IAudioService _audioService;

        // Visual Materials
        private Material _normalMaterial;
        private Material _damageMaterial;
        private Material _revivalMaterial;

        // State
        private bool _isInitialized = false;

        // Events
        public System.Action<EnemyController> OnEnemyInitialized;
        public System.Action OnEnemyDestroyed;

        // Properties
        public EnemyController Controller => _enemyController;
        public bool IsInitialized => _isInitialized && _enemyController != null;

        #region Unity Lifecycle

        void Awake()
        {
            if (_baseStats == null)
            {
                Debug.LogError("EnemyMonoBehaviour: BaseStats not assigned!");
                return;
            }

            // Validate base stats
            if (!_baseStats.ValidateData())
            {
                Debug.LogError("EnemyMonoBehaviour: BaseStats validation failed!");
                return;
            }

            // Setup visual components
            SetupVisualComponents();

            // Initialize enemy
            InitializeEnemy();
        }

        void Start()
        {
            // Setup services
            SetupServices();

            // Load materials
            LoadMaterials();

            // Set initial material
            SetMaterial(_normalMaterial);

            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} started successfully");
        }

        void Update()
        {
            if (!IsInitialized) return;

            _enemyController.Update(Time.deltaTime);

            // Check for destruction
            if (_enemyController.StateMachine.IsReadyForDestruction())
            {
                DestroyEnemy();
            }

            // Update debug info
            if (_showDebugInfo && Time.frameCount % 30 == 0) // Every 0.5 seconds at 60fps
            {
                DrawDebugInfo();
            }
        }

        void OnDestroy()
        {
            OnEnemyDestroyed?.Invoke();
            
            if (_isInitialized)
            {
                _enemyController?.Shutdown();
            }
        }

        #endregion

        #region Initialization

        private void SetupVisualComponents()
        {
            // Find Visual child if not assigned
            if (_visualTransform == null)
            {
                _visualTransform = transform.Find("Visual");
                if (_visualTransform == null)
                {
                    Debug.LogWarning($"EnemyMonoBehaviour: No Visual child found in {gameObject.name}");
                    _visualTransform = transform; // Use root as fallback
                }
            }

            // Find Body renderer if not assigned
            if (_bodyRenderer == null)
            {
                // Try Visual/Body path first
                Transform bodyTransform = _visualTransform.Find("Body");
                if (bodyTransform != null)
                {
                    _bodyRenderer = bodyTransform.GetComponent<Renderer>();
                }

                // Fallback to visual transform
                if (_bodyRenderer == null)
                {
                    _bodyRenderer = _visualTransform.GetComponent<Renderer>();
                }

                // Last resort: search in children
                if (_bodyRenderer == null)
                {
                    _bodyRenderer = GetComponentInChildren<Renderer>();
                }

                if (_bodyRenderer == null)
                {
                    Debug.LogError($"EnemyMonoBehaviour: No Renderer found in {gameObject.name}!");
                }
            }
        }

        private void InitializeEnemy()
        {
            _enemyController = new EnemyController(_baseStats, transform.position);

            // Subscribe to enemy events
            _enemyController.OnPhysicalHealthChanged += HandlePhysicalHealthChanged;
            _enemyController.OnMentalHealthChanged += HandleMentalHealthChanged;
            _enemyController.OnPhysicalDeath += HandlePhysicalDeath;
            _enemyController.OnTrueDeath += HandleTrueDeath;
            _enemyController.OnRevivalStarted += HandleRevivalStarted;
            _enemyController.OnRevivalCompleted += HandleRevivalCompleted;
            _enemyController.OnAttackLaunched += HandleAttackLaunched;
            _enemyController.OnStateChanged += HandleStateChanged;

            _isInitialized = true;
            OnEnemyInitialized?.Invoke(_enemyController);

            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} initialized successfully");
        }

        private void SetupServices()
        {
            _audioService = ServiceRegistry.Get<IAudioService>();
            if (_audioService == null)
            {
                Debug.LogWarning("EnemyMonoBehaviour: AudioService not found. Audio effects will be disabled.");
            }
        }

        private void LoadMaterials()
        {
            if (_baseStats == null) return;

            // Load normal material
            _normalMaterial = Resources.Load<Material>(_baseStats.normalMaterialPath);
            if (_normalMaterial == null)
            {
                Debug.LogError($"EnemyMonoBehaviour: Failed to load normal material from {_baseStats.normalMaterialPath}");
            }

            // Load damage material
            _damageMaterial = Resources.Load<Material>(_baseStats.damageMaterialPath);
            if (_damageMaterial == null)
            {
                Debug.LogError($"EnemyMonoBehaviour: Failed to load damage material from {_baseStats.damageMaterialPath}");
            }

            // Load revival material
            _revivalMaterial = Resources.Load<Material>(_baseStats.revivalMaterialPath);
            if (_revivalMaterial == null)
            {
                Debug.LogWarning($"EnemyMonoBehaviour: Failed to load revival material from {_baseStats.revivalMaterialPath}");
                _revivalMaterial = _damageMaterial; // Use damage material as fallback
            }
        }

        #endregion

        #region IDamageable Implementation

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!IsInitialized) return;

            switch (damageInfo.type)
            {
                case DamageType.Physical:
                    _enemyController.TakePhysicalDamage(damageInfo.amount);
                    break;
                    
                case DamageType.Mental:
                    _enemyController.TakeMentalDamage(damageInfo.amount);
                    break;
                    
                case DamageType.Mixed:
                    float physicalDamage = damageInfo.amount * damageInfo.physicalRatio;
                    float mentalDamage = damageInfo.amount * (1f - damageInfo.physicalRatio);
                    _enemyController.TakePhysicalDamage(physicalDamage);
                    _enemyController.TakeMentalDamage(mentalDamage);
                    break;
                    
                case DamageType.True:
                    // True damage affects both health types equally
                    _enemyController.TakePhysicalDamage(damageInfo.amount * 0.5f);
                    _enemyController.TakeMentalDamage(damageInfo.amount * 0.5f);
                    break;
            }

            // Visual and audio feedback
            ShowDamageEffect(damageInfo);
            PlayHitAudio(damageInfo);

            Debug.Log($"EnemyMonoBehaviour: Took {damageInfo.amount} {damageInfo.type} damage");
        }

        public void TakePhysicalDamage(float damage, Vector3 damageSource)
        {
            if (IsInitialized)
            {
                _enemyController.TakePhysicalDamage(damage);
                ShowDamageEffect(new DamageInfo(damage, DamageType.Physical, damageSource));
                PlayHitAudio(new DamageInfo(damage, DamageType.Physical, damageSource));
            }
        }

        public void TakeMentalDamage(float damage, Vector3 damageSource)
        {
            if (IsInitialized)
            {
                _enemyController.TakeMentalDamage(damage);
                ShowDamageEffect(new DamageInfo(damage, DamageType.Mental, damageSource));
                PlayHitAudio(new DamageInfo(damage, DamageType.Mental, damageSource));
            }
        }

        #endregion

        #region Health Properties

        public bool IsPhysicallyAlive => IsInitialized && _enemyController.IsPhysicallyAlive;
        public bool IsMentallyAlive => IsInitialized && _enemyController.IsMentallyAlive;
        public bool IsInPhysicalDeathState => IsInitialized && _enemyController.IsInPhysicalDeathState;
        public float CurrentPhysicalHealth => IsInitialized ? _enemyController.Stats.currentPhysicalHealth : 0f;
        public float MaxPhysicalHealth => IsInitialized ? _enemyController.Stats.maxPhysicalHealth : 0f;
        public float CurrentMentalHealth => IsInitialized ? _enemyController.Stats.currentMentalHealth : 0f;
        public float MaxMentalHealth => IsInitialized ? _enemyController.Stats.maxMentalHealth : 0f;

        #endregion

        #region Event Handlers

        private void HandlePhysicalHealthChanged(float current, float max)
        {
            // Health UI updates would go here
        }

        private void HandleMentalHealthChanged(float current, float max)
        {
            // Health UI updates would go here
        }

        private void HandlePhysicalDeath()
        {
            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} entered physical death state");
            SetMaterial(_damageMaterial);
            PlayDeathAudio();
        }

        private void HandleTrueDeath()
        {
            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} entered true death state");
            SetMaterial(_damageMaterial);
            PlayDeathAudio();
            
            // Start destruction countdown
            Destroy(gameObject, 3f);
        }

        private void HandleRevivalStarted()
        {
            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} started revival");
            SetMaterial(_revivalMaterial);
        }

        private void HandleRevivalCompleted()
        {
            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} completed revival");
            SetMaterial(_normalMaterial);
        }

        private void HandleAttackLaunched(float damage)
        {
            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} launched attack for {damage} damage");
            // Attack effects would go here
        }

        private void HandleStateChanged(string stateName)
        {
            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} changed to state {stateName}");
        }

        #endregion

        #region Visual Effects

        private void ShowDamageEffect(DamageInfo damageInfo)
        {
            if (_bodyRenderer != null && _damageMaterial != null)
            {
                StartCoroutine(DamageFlashCoroutine());
            }
        }

        private IEnumerator DamageFlashCoroutine()
        {
            // Switch to damage material
            if (_bodyRenderer != null && _damageMaterial != null)
            {
                Material originalMaterial = _bodyRenderer.material;
                _bodyRenderer.material = _damageMaterial;
                
                yield return new WaitForSeconds(_baseStats.damageFlashDuration);
                
                // Restore appropriate material based on state
                RestoreStateMaterial();
            }
        }

        private void SetMaterial(Material material)
        {
            if (_bodyRenderer != null && material != null)
            {
                _bodyRenderer.material = material;
            }
        }

        private void RestoreStateMaterial()
        {
            if (!IsInitialized) return;

            if (_enemyController.StateMachine.IsReviving())
            {
                SetMaterial(_revivalMaterial);
            }
            else if (_enemyController.StateMachine.IsPhysicallyDead() || _enemyController.StateMachine.IsTrulyDead())
            {
                SetMaterial(_damageMaterial);
            }
            else
            {
                SetMaterial(_normalMaterial);
            }
        }

        #endregion

        #region Audio Effects

        private void PlayHitAudio(DamageInfo damageInfo)
        {
            if (_audioService == null || !_baseStats.enableAudio) return;

            AudioClipType hitClipType = GetHitAudioClipType(damageInfo);
            _audioService.PlaySFX3D(hitClipType, transform.position, 0.7f, 1f);
        }

        private void PlayDeathAudio()
        {
            if (_audioService == null || !_baseStats.enableAudio) return;

            AudioClipType deathClipType = GetDeathAudioClipType();
            _audioService.PlaySFX3D(deathClipType, transform.position, 0.9f, 1f);
        }

        private AudioClipType GetHitAudioClipType(DamageInfo damageInfo)
        {
            string enemyName = gameObject.name.ToLower();
            
            if (enemyName.Contains("metal") || enemyName.Contains("robot"))
            {
                return AudioClipType.EnemyHitMetal;
            }
            else if (enemyName.Contains("flesh") || enemyName.Contains("organic"))
            {
                return AudioClipType.EnemyHitFlesh;
            }
            else
            {
                return AudioClipType.EnemyHit;
            }
        }

        private AudioClipType GetDeathAudioClipType()
        {
            string enemyName = gameObject.name.ToLower();
            
            if (enemyName.Contains("boss") || enemyName.Contains("explosion"))
            {
                return AudioClipType.EnemyDeathExplosion;
            }
            else
            {
                return AudioClipType.EnemyDeath;
            }
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Reset enemy to full health (for testing)
        /// </summary>
        public void ResetEnemy()
        {
            if (!IsInitialized) return;

            _enemyController.Stats.RestoreToFullHealth();
            _enemyController.StateMachine.ChangeState("Normal");
            SetMaterial(_normalMaterial);
            
            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} reset to full health");
        }

        /// <summary>
        /// Get enemy statistics
        /// </summary>
        public string GetStats()
        {
            if (!IsInitialized) return "Not initialized";
            return _enemyController.GetStats();
        }

        /// <summary>
        /// Force enemy to enter specific state (for testing)
        /// </summary>
        public void ForceState(string stateName)
        {
            if (!IsInitialized) return;
            _enemyController.StateMachine.ChangeState(stateName);
        }

        #endregion

        #region Destruction

        private void DestroyEnemy()
        {
            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} ready for destruction");
            Destroy(gameObject);
        }

        #endregion

        #region Debug

        private void DrawDebugInfo()
        {
            if (!IsInitialized) return;

            var stats = _enemyController.Stats;
            string stateInfo = $"State: {_enemyController.CurrentState}";
            if (_enemyController.StateMachine.IsInState("Normal"))
            {
                stateInfo += $" ({_enemyController.StateMachine.GetNormalSubState()})";
            }
            
            Debug.Log($"Enemy {gameObject.name}: Physical: {stats.currentPhysicalHealth:F1}/{stats.maxPhysicalHealth}, " +
                     $"Mental: {stats.currentMentalHealth:F1}/{stats.maxMentalHealth}, {stateInfo}");
        }

        void OnDrawGizmos()
        {
            if (!IsInitialized || !_baseStats.showHealthBar) return;

            // Draw health bar
            Vector3 barPosition = transform.position + Vector3.up * 2f;
            float barWidth = 2f;
            float barHeight = 0.2f;
            
            // Physical health (bottom bar)
            Gizmos.color = Color.red;
            Gizmos.DrawCube(barPosition, new Vector3(barWidth, barHeight * 0.5f, 0.1f));
            
            float physicalPercentage = _enemyController.Stats.PhysicalHealthPercentage;
            Gizmos.color = Color.green;
            Vector3 physicalBarSize = new Vector3(barWidth * physicalPercentage, barHeight * 0.5f, 0.1f);
            Vector3 physicalBarPosition = barPosition + Vector3.left * (barWidth * (1f - physicalPercentage) * 0.5f);
            Gizmos.DrawCube(physicalBarPosition, physicalBarSize);
            
            // Mental health (top bar)
            Vector3 mentalBarCenter = barPosition + Vector3.up * barHeight * 0.6f;
            Gizmos.color = Color.blue;
            Gizmos.DrawCube(mentalBarCenter, new Vector3(barWidth, barHeight * 0.5f, 0.1f));
            
            float mentalPercentage = _enemyController.Stats.MentalHealthPercentage;
            Gizmos.color = Color.cyan;
            Vector3 mentalBarSize = new Vector3(barWidth * mentalPercentage, barHeight * 0.5f, 0.1f);
            Vector3 mentalBarPosition = mentalBarCenter + Vector3.left * (barWidth * (1f - mentalPercentage) * 0.5f);
            Gizmos.DrawCube(mentalBarPosition, mentalBarSize);
            
            // Border
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(barPosition, new Vector3(barWidth, barHeight * 0.5f, 0.1f));
            Gizmos.DrawWireCube(mentalBarCenter, new Vector3(barWidth, barHeight * 0.5f, 0.1f));
        }

        void OnDrawGizmosSelected()
        {
            if (!IsInitialized) return;

            // Draw detection range
            if (_baseStats.showDetectionRange)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, _baseStats.detectionRange);
            }

            // Draw attack range
            if (_baseStats.showAttackRange)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, _baseStats.attackRange);
            }
        }

        #endregion
    }
}
