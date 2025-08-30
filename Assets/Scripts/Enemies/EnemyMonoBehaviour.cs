using UnityEngine;
using Resonance.Enemies.Core;
using Resonance.Enemies.Data;
using Resonance.Interfaces;
using Resonance.Interfaces.Services;
using Resonance.Utilities;
using System.Collections;

namespace Resonance.Enemies
{
    // Patrol mode enumeration
    public enum PatrolMode
    {
        Infinite,
        Limited    
    }

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

        [Header("Detection System")]
        [SerializeField] private SphereCollider _detectionCollider;
        [SerializeField] private SphereCollider _attackCollider;
        
        [Header("Patrol System - Waypoints")]
        [SerializeField] private Transform _patrolPointA;
        [SerializeField] private Transform _patrolPointB;
        [SerializeField] private bool _useTransformPoints = false;
        [Tooltip("If true, use Transform references. If false, use Vector3 waypoints relative to enemy position.")]
        
        [SerializeField] private Vector3 _patrolWaypointA = Vector3.zero;
        [SerializeField] private Vector3 _patrolWaypointB = Vector3.forward * 5f;
        
        [Header("Patrol System - Behavior")]
        [SerializeField] private PatrolMode _patrolMode = PatrolMode.Infinite;
        [Tooltip("Infinite: Never stops patrolling. Limited: Stops after specified cycles.")]
        
        [SerializeField] private int _maxPatrolCycles = 3;
        [Tooltip("How many complete A→B→A cycles before stopping (only used in Limited mode).")]
        
        [SerializeField] private float _patrolSpeed = 2f;
        [Tooltip("Movement speed while patrolling (units per second).")]
        
        [Header("Patrol System - Timing")]
        [SerializeField] private float _singleCycleDuration = 10f;
        [Tooltip("How long one complete patrol cycle (A→B→A) should take in seconds.")]
        
        [SerializeField] private float _waitAtWaypointDuration = 1f;
        [Tooltip("How long to wait at each waypoint before moving to the next.")]
        
        [SerializeField] private float _arrivalThreshold = 0.5f;
        [Tooltip("How close to waypoint before considering 'arrived' (meters).")]
        
        [Header("Patrol System - Visual")]
        [SerializeField] private bool _showPatrolPath = true;
        [Tooltip("Show patrol path in Scene view when enemy is selected.")]

        [Header("Chase System")]
        [SerializeField] private float _targetUpdateInterval = 0.5f;
        [Tooltip("How often to update the chase target position (seconds).")]
        
        [SerializeField] private float _chaseArrivalThreshold = 1f;
        [Tooltip("How close to get to target before considering 'arrived' (meters).")]

        [Header("Attack System")]
        [SerializeField] private float _attackDuration = 0.5f;
        [Tooltip("How long the attack action stays active after performing the attack (seconds).")]
        
        [SerializeField] private float _attackDamage = 20f;
        [Tooltip("Base damage amount for enemy attacks.")]
        
        [SerializeField] private float _attackCooldown = 3f;
        [Tooltip("Cooldown time between attacks (seconds).")]

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

            // Setup detection system
            SetupDetectionSystem();

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

            // Update collider radii in case stats changed
            UpdateColliderRadii();
            
            // Verify and fix detection system (in case components were missing)
            VerifyDetectionSystem();
            
            // Setup patrol waypoints
            SetupPatrolWaypoints();

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
        
        #region Patrol System Properties
        
        /// <summary>
        /// Get patrol waypoint A in world coordinates
        /// </summary>
        public Vector3 PatrolWaypointA
        {
            get
            {
                if (_useTransformPoints && _patrolPointA != null)
                    return _patrolPointA.position;
                else
                    return transform.position + _patrolWaypointA;
            }
        }
        
        /// <summary>
        /// Get patrol waypoint B in world coordinates
        /// </summary>
        public Vector3 PatrolWaypointB
        {
            get
            {
                if (_useTransformPoints && _patrolPointB != null)
                    return _patrolPointB.position;
                else
                    return transform.position + _patrolWaypointB;
            }
        }
        
        /// <summary>
        /// Check if patrol waypoints are properly configured
        /// </summary>
        public bool HasValidPatrolWaypoints
        {
            get
            {
                if (_useTransformPoints)
                {
                    return _patrolPointA != null && _patrolPointB != null;
                }
                else
                {
                    return Vector3.Distance(_patrolWaypointA, _patrolWaypointB) > 0.1f;
                }
            }
        }
        
        /// <summary>
        /// Patrol configuration properties
        /// </summary>
        public PatrolMode EnemyPatrolMode => _patrolMode;
        public int MaxPatrolCycles => _maxPatrolCycles;
        public float PatrolSpeed => _patrolSpeed;
        public float SingleCycleDuration => _singleCycleDuration;
        public float WaitAtWaypointDuration => _waitAtWaypointDuration;
        public float ArrivalThreshold => _arrivalThreshold;
        
        /// <summary>
        /// Chase configuration properties
        /// </summary>
        public float TargetUpdateInterval => _targetUpdateInterval;
        public float ChaseArrivalThreshold => _chaseArrivalThreshold;
        
        /// <summary>
        /// Attack configuration properties
        /// </summary>
        public float AttackDuration => _attackDuration;
        public float AttackDamage => _attackDamage;
        public float AttackCooldown => _attackCooldown;
        
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
            _enemyController = new EnemyController(_baseStats, transform.position, transform);

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
            
            // Setup patrol waypoints after controller is initialized
            SetupPatrolWaypoints();
            
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
                _revivalMaterial = _damageMaterial;
            }
        }

        private void SetupDetectionSystem()
        {
            // Setup detection collider
            SetupDetectionCollider();
            
            // Setup attack collider
            SetupAttackCollider();
            
            // Setup damage hitbox
            SetupDamageHitbox();

            // Set initial radii
            UpdateColliderRadii();
        }
        
        private void SetupDetectionCollider()
        {
            // Try to find existing detection collider
            Transform detectionChild = transform.Find("DetectionRange");
            
            if (detectionChild != null)
            {
                _detectionCollider = detectionChild.GetComponent<SphereCollider>();
                
                // Ensure it has a SphereCollider
                if (_detectionCollider == null)
                {
                    _detectionCollider = detectionChild.gameObject.AddComponent<SphereCollider>();
                    _detectionCollider.isTrigger = true;
                }
                
                // Check and add EnemyDetectionTrigger if needed
                SetupDetectionTriggerComponent(detectionChild.gameObject, TriggerType.Detection);
            }
            else
            {
                GameObject detectionGO = new GameObject("DetectionRange");
                detectionGO.transform.SetParent(transform);
                detectionGO.transform.localPosition = Vector3.zero;
                detectionGO.layer = gameObject.layer;
                
                _detectionCollider = detectionGO.AddComponent<SphereCollider>();
                _detectionCollider.isTrigger = true;
                
                // Add trigger component
                SetupDetectionTriggerComponent(detectionGO, TriggerType.Detection);
            }
        }
        
        private void SetupAttackCollider()
        {
            // Try to find existing attack collider
            Transform attackChild = transform.Find("AttackRange");
            
            if (attackChild != null)
            {
                _attackCollider = attackChild.GetComponent<SphereCollider>();
                
                // Ensure it has a SphereCollider
                if (_attackCollider == null)
                {
                    _attackCollider = attackChild.gameObject.AddComponent<SphereCollider>();
                    _attackCollider.isTrigger = true;
                }
                
                // Check and add EnemyDetectionTrigger if needed
                SetupDetectionTriggerComponent(attackChild.gameObject, TriggerType.Attack);
            }
            else
            {
                GameObject attackGO = new GameObject("AttackRange");
                attackGO.transform.SetParent(transform);
                attackGO.transform.localPosition = Vector3.zero;
                attackGO.layer = gameObject.layer;
                
                _attackCollider = attackGO.AddComponent<SphereCollider>();
                _attackCollider.isTrigger = true;
                
                // Add trigger component
                SetupDetectionTriggerComponent(attackGO, TriggerType.Attack);
            }
        }
        
        private void SetupDamageHitbox()
        {
            // Try to find existing damage hitbox
            Transform damageHitboxChild = transform.Find("DamageHitbox");
            
            if (damageHitboxChild != null)
            {
                // Check and add EnemyDamageHitbox if needed
                SetupDamageHitboxComponent(damageHitboxChild.gameObject);
            }
            else
            {
                // Create DamageHitbox GameObject if it doesn't exist
                GameObject damageHitboxGO = new GameObject("DamageHitbox");
                damageHitboxGO.transform.SetParent(transform);
                damageHitboxGO.transform.localPosition = Vector3.zero;
                damageHitboxGO.layer = gameObject.layer;
                
                // Add a default collider (can be customized in inspector)
                SphereCollider hitboxCollider = damageHitboxGO.AddComponent<SphereCollider>();
                hitboxCollider.isTrigger = true;
                hitboxCollider.radius = 1.5f; // Default attack hitbox radius
                
                // Add damage hitbox component
                SetupDamageHitboxComponent(damageHitboxGO);
                
                // Start disabled - will be enabled by animation events
                damageHitboxGO.SetActive(false);
            }
        }
        
        private void SetupDamageHitboxComponent(GameObject hitboxObject)
        {
            // Check if EnemyDamageHitbox already exists
            EnemyDamageHitbox existingHitbox = hitboxObject.GetComponent<EnemyDamageHitbox>();
            
            if (existingHitbox != null)
            {
                existingHitbox.Initialize(this);
            }
            else
            {
                EnemyDamageHitbox newHitbox = hitboxObject.AddComponent<EnemyDamageHitbox>();
                newHitbox.Initialize(this);
            }
        }
        
        private void SetupDetectionTriggerComponent(GameObject triggerObject, TriggerType triggerType)
        {
            // Check if EnemyDetectionTrigger already exists
            EnemyDetectionTrigger existingTrigger = triggerObject.GetComponent<EnemyDetectionTrigger>();
            
            if (existingTrigger != null)
            {
                existingTrigger.Initialize(this, triggerType);
            }
            else
            {
                EnemyDetectionTrigger newTrigger = triggerObject.AddComponent<EnemyDetectionTrigger>();
                newTrigger.Initialize(this, triggerType);
            }
        }
        
        private void SetupPatrolWaypoints()
        {
            // Validate patrol waypoints
            if (!HasValidPatrolWaypoints)
            {
                Debug.LogWarning($"EnemyMonoBehaviour: {gameObject.name} has invalid patrol waypoints. Using default points.");
                
                // Set default waypoints if none are configured
                if (!_useTransformPoints)
                {
                    _patrolWaypointA = Vector3.left * 3f;
                    _patrolWaypointB = Vector3.right * 3f;
                }
            }
            
            // Pass waypoints and configuration to controller if it's initialized
            if (_isInitialized && _enemyController != null)
            {
                _enemyController.SetPatrolWaypoints(PatrolWaypointA, PatrolWaypointB);
                _enemyController.SetPatrolConfiguration(
                    _patrolMode,
                    _maxPatrolCycles,
                    _patrolSpeed,
                    _singleCycleDuration,
                    _waitAtWaypointDuration,
                    _arrivalThreshold
                );
                
                // Set chase and attack configuration
                _enemyController.SetChaseConfiguration(
                    _targetUpdateInterval,
                    _chaseArrivalThreshold
                );
                
                _enemyController.SetAttackConfiguration(
                    _attackDuration,
                    _attackDamage,
                    _attackCooldown
                );
            }
            
            Debug.Log($"EnemyMonoBehaviour: Patrol waypoints set - A: {PatrolWaypointA}, B: {PatrolWaypointB}");
        }

        private void UpdateColliderRadii()
        {
            if (_baseStats == null) return;

            if (_detectionCollider != null)
            {
                _detectionCollider.radius = _baseStats.detectionRange;
            }

            if (_attackCollider != null)
            {
                _attackCollider.radius = _baseStats.attackRange;
            }
        }
        
        private void VerifyDetectionSystem()
        {            
            // Check detection collider and trigger component
            if (_detectionCollider != null)
            {
                EnemyDetectionTrigger detectionTrigger = _detectionCollider.GetComponent<EnemyDetectionTrigger>();
                if (detectionTrigger == null)
                {
                    SetupDetectionTriggerComponent(_detectionCollider.gameObject, TriggerType.Detection);
                }
            }
            
            // Check attack collider and trigger component
            if (_attackCollider != null)
            {
                EnemyDetectionTrigger attackTrigger = _attackCollider.GetComponent<EnemyDetectionTrigger>();
                if (attackTrigger == null)
                {
                    SetupDetectionTriggerComponent(_attackCollider.gameObject, TriggerType.Attack);
                }
            }
            
            // Check damage hitbox component
            Transform damageHitboxChild = transform.Find("DamageHitbox");
            if (damageHitboxChild != null)
            {
                EnemyDamageHitbox damageHitbox = damageHitboxChild.GetComponent<EnemyDamageHitbox>();
                if (damageHitbox == null)
                {
                    SetupDamageHitboxComponent(damageHitboxChild.gameObject);
                }
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
            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} physical death - checking mental health for state transition");
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

        #region Trigger System

        /// <summary>
        /// 处理触发器进入事件
        /// </summary>
        public void HandleTriggerEnter(TriggerType triggerType, Collider other)
        {
            if (!IsInitialized) return;

            // 只检测玩家
            if (!other.CompareTag("Player")) return;

            Transform playerTransform = other.transform;

            switch (triggerType)
            {
                case TriggerType.Detection:
                    _enemyController.SetPlayerTarget(playerTransform);
                    break;

                case TriggerType.Attack:
                    _enemyController.SetPlayerInAttackRange(true);
                    break;
            }
        }

        /// <summary>
        /// 处理触发器退出事件
        /// </summary>
        public void HandleTriggerExit(TriggerType triggerType, Collider other)
        {
            if (!IsInitialized) return;
            
            // 只检测玩家
            if (!other.CompareTag("Player")) return;
            
            switch (triggerType)
            {
                case TriggerType.Detection:
                    _enemyController.LosePlayer();
                    break;

                case TriggerType.Attack:
                    _enemyController.SetPlayerInAttackRange(false);
                    break;
            }
        }

        /// <summary>
        /// 处理触发器停留事件
        /// </summary>
        public void HandleTriggerStay(TriggerType triggerType, Collider other)
        {
            if (!IsInitialized) return;

            Transform playerTransform = other.transform;

            switch (triggerType)
            {
                case TriggerType.Detection:
                    // 玩家进入检测范围
                    _enemyController.SetPlayerTarget(playerTransform);
                    // Debug.Log($"EnemyMonoBehaviour: Player still in detection range");
                    break;

                case TriggerType.Attack:
                    // 玩家进入攻击范围
                    _enemyController.SetPlayerInAttackRange(true);
                    // Debug.Log($"EnemyMonoBehaviour: Player still in attack range");
                    break;
            }
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
            // Draw detection range
            if (_baseStats != null && _baseStats.showDetectionRange)
            {
                Gizmos.color = Color.yellow;
                float detectionRadius = _detectionCollider != null ? _detectionCollider.radius : _baseStats.detectionRange;
                Gizmos.DrawWireSphere(transform.position, detectionRadius);
            }

            // Draw attack range
            if (_baseStats != null && _baseStats.showAttackRange)
            {
                Gizmos.color = Color.red;
                float attackRadius = _attackCollider != null ? _attackCollider.radius : _baseStats.attackRange;
                Gizmos.DrawWireSphere(transform.position, attackRadius);
            }
            
            // Draw patrol path
            if (_showPatrolPath)
            {
                DrawPatrolPath();
            }
        }
        
        private void DrawPatrolPath()
        {
            Vector3 waypointA = PatrolWaypointA;
            Vector3 waypointB = PatrolWaypointB;
            
            // Draw waypoints
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(waypointA, 0.3f);
            Gizmos.DrawWireSphere(waypointB, 0.3f);
            
            // Draw path line
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(waypointA, waypointB);
            
            // Draw labels
            Gizmos.color = Color.white;
            Gizmos.DrawRay(waypointA, Vector3.up * 0.5f);
            Gizmos.DrawRay(waypointB, Vector3.up * 0.5f);
            
            // Draw current target if patrolling
            if (IsInitialized && _enemyController.IsPatrolling)
            {
                Vector3 currentTarget = _enemyController.CurrentPatrolTarget;
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(currentTarget, 0.2f);
                Gizmos.DrawLine(transform.position, currentTarget);
            }
        }

        #endregion
        
        #region Editor Validation
        
        void OnValidate()
        {
            // Validate patrol configuration
            if (_maxPatrolCycles < 1)
                _maxPatrolCycles = 1;
                
            if (_patrolSpeed < 0.1f)
                _patrolSpeed = 0.1f;
                
            if (_singleCycleDuration < 1f)
                _singleCycleDuration = 1f;
                
            if (_waitAtWaypointDuration < 0f)
                _waitAtWaypointDuration = 0f;
                
            if (_arrivalThreshold < 0.1f)
                _arrivalThreshold = 0.1f;
            
            // Validate chase configuration
            if (_targetUpdateInterval < 0.1f)
                _targetUpdateInterval = 0.1f;
                
            if (_chaseArrivalThreshold < 0.1f)
                _chaseArrivalThreshold = 0.1f;
            
            // Validate attack configuration
            if (_attackDuration < 0.1f)
                _attackDuration = 0.1f;
                
            if (_attackDamage < 0f)
                _attackDamage = 0f;
                
            if (_attackCooldown < 0f)
                _attackCooldown = 0f;
        }
        
        #endregion
    }
}
