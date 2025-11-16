using UnityEngine;
using TMPro;
using BehaviorDesigner.Runtime;
using System.Collections;
using Resonance.Core;
using Resonance.Gameplay.Enemies.Core;
using Resonance.Gameplay.Enemies.Data;
using Resonance.Gameplay.Enemies.Movement;
using Resonance.Gameplay.Enemies.Triggers;
using Resonance.Shared.Interfaces;
using Resonance.Shared.Interfaces.Services;
using Resonance.Utilities;
using Resonance.Shared.Types;
using Resonance.Systems.Waves;

namespace Resonance.Gameplay.Enemies
{
    /// <summary>
    /// MonoBehaviour component that handles Unity-specific enemy functionality.
    /// Acts as a bridge between Unity's GameObject system and the enemy logic.
    /// Implements IDamageable interface for damage handling.
    /// </summary>
    public class EnemyMonoBehaviour : MonoBehaviour, IDamageable, IHasBalance, IHasCoreHealth
    {
        [Header("Enemy Configuration")]
        [SerializeField] private EnemyBaseStats _baseStats;

        [Header("Visual")]
        [SerializeField] private Transform _visualTransform;
        [SerializeField] private Renderer _bodyRenderer;

        [Header("UI")]
        [SerializeField] private GameObject _waveUI;
        [SerializeField] private TextMeshProUGUI _waveUIText;

        [Header("Detection System")]
        [SerializeField] private SphereCollider _attackTrigger;
        
        [Header("Patrol System")]
        [SerializeField] private Transform _patrolPointA;
        [SerializeField] private Transform _patrolPointB;

        [SerializeField] private float _waitAtWaypointDuration = 1f;
        [Tooltip("How long to wait at each waypoint before moving to the next.")]
        
        [SerializeField] private bool _showPatrolPath = true;
        [Tooltip("Show patrol path in Scene view when enemy is selected.")]
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;

        // Core Components
        private EnemyController _enemyController;
        private EnemyMovement _movementSystem;
        private EnemyAnimator _enemyAnimator;
        private IAudioService _audioService;
        private Animator _animator;
        private EnemyHitboxManager _hitboxManager;

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
        public EnemyHitboxManager HitboxManager => _hitboxManager;
        public EnemyCrystalCoreHitbox CrystalCoreHitbox => _hitboxManager?.GetCrystalCoreHitbox();

        #region Unity Lifecycle

        void Awake()
        {
            if (_baseStats == null)
            {
                Debug.LogError("EnemyMonoBehaviour: BaseStats not assigned!");
                return;
            }

            // Validate base stats
            if (!_baseStats.ValidateConfig())
            {
                Debug.LogError("EnemyMonoBehaviour: BaseStats validation failed!");
                return;
            }

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
            
            // Verify and fix detection system (in case components were missing)
            VerifyDetectionSystem();

            // Setup Wave UI
            SetupWaveUI();

            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} started successfully");
        }

        void Update()
        {
            if (!IsInitialized || _enemyController.IsPaused) return;

            // Update core controller (health, combat, etc.)
            _enemyController.Update(Time.deltaTime);

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
                if (_patrolPointA != null)
                    return _patrolPointA.position;
                else
                    return Vector3.zero;
            }
        }
        
        /// <summary>
        /// Get patrol waypoint B in world coordinates
        /// </summary>
        public Vector3 PatrolWaypointB
        {
            get
            {
                if (_patrolPointB != null)
                    return _patrolPointB.position;
                else
                    return Vector3.zero;
            }
        }
        
        /// <summary>
        /// Check if patrol waypoints are properly configured
        /// </summary>
        public bool HasValidPatrolWaypoints
        {
            get
            {
                return _patrolPointA != null && _patrolPointB != null;
            }
        }
        
        /// <summary>
        /// Patrol configuration properties
        /// </summary>
        public float WaitAtWaypointDuration => _waitAtWaypointDuration;
        
        #endregion

        #region Initialization

        private void InitializeEnemy()
        {
            // Setup visual components
            SetupVisualComponents();

            // Initialize core controller
            _enemyController = new EnemyController(_baseStats, transform.position, transform);

            // Get movement system from controller
            _movementSystem = _enemyController.Movement;

            // Subscribe to enemy events
            _enemyController.OnBalanceChanged += HandleBalanceChanged;
            _enemyController.OnCoreEnergyChanged += HandleCoreHealthChanged;
            _enemyController.OnUnbalanced += HandleUnbalanced;
            _enemyController.OnDeath += HandleDeath;
            _enemyController.OnUnbalancedStarted += HandleUnbalancedStarted;
            _enemyController.OnUnbalancedCompleted += HandleUnbalancedCompleted;
            _enemyController.OnCoreExposureStarted += HandleCoreExposureStarted;
            _enemyController.OnCoreExposureCompleted += HandleCoreExposureCompleted;
            _enemyController.OnAttackLaunched += HandleAttackLaunched;
            _enemyController.OnStateChanged += HandleStateChanged;

            _isInitialized = true;

            // Setup EnemyAnimator
            SetupEnemyAnimator();

            // Setup patrol waypoints after controller is initialized
            SetupPatrolWaypoints();
            
            // Reset attack cooldown so enemy can attack immediately when needed
            _enemyController.ResetAttackCooldown();

            // Setup detection system
            SetupDetectionSystem();
            
            OnEnemyInitialized?.Invoke(_enemyController);

            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} initialized successfully");
        }

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

        private void SetupEnemyAnimator()
        {
            // get Animator component (should be on root game object)
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                Debug.LogError($"EnemyMonoBehaviour: No Animator found on {gameObject.name}!");
                _animator = gameObject.AddComponent<Animator>();
            }

            // get EnemyAnimator component
            _enemyAnimator = _animator.gameObject.GetComponent<EnemyAnimator>();
            if (_enemyAnimator == null)
            {
                Debug.LogError($"EnemyMonoBehaviour: No EnemyAnimator found on {gameObject.name}!");
                _enemyAnimator = _animator.gameObject.AddComponent<EnemyAnimator>();
            }
            
            // initialize EnemyAnimator
            if(_enemyAnimator != null)
            {
                EnemyDamageHitbox damageHitbox = GetComponentInChildren<EnemyDamageHitbox>();
                _enemyAnimator.Initialize(this, damageHitbox);
            }
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
            // Setup vision system
            SetupVisionSystem();
            
            // Setup attack collider
            SetupAttackTrigger();
            
            // Setup damage hitbox
            SetupDamageHitbox();
            
            // Setup hitbox system
            SetupHitboxSystem();
        }
        
        private void SetupVisionSystem()
        {
            // EnemyVision component will be automatically added and initialized by EnemyController
            // We just need to ensure the component exists on this GameObject
            EnemyVision vision = GetComponent<EnemyVision>();
            if (vision == null)
            {
                vision = gameObject.AddComponent<EnemyVision>();
                Debug.Log($"EnemyMonoBehaviour: Added EnemyVision component to {gameObject.name}");
            }
            else
            {
                Debug.Log($"EnemyMonoBehaviour: EnemyVision component already exists on {gameObject.name}");
            }
        }
        
        private void SetupAttackTrigger()
        {
            // Try to find existing attack collider
            Transform attackChild = transform.Find("AttackRange");
            
            if (attackChild != null)
            {
                _attackTrigger = attackChild.GetComponent<SphereCollider>();
                
                // Ensure it has a SphereCollider
                if (_attackTrigger == null)
                {
                    _attackTrigger = attackChild.gameObject.AddComponent<SphereCollider>();
                    _attackTrigger.isTrigger = true;
                }
            }
            else
            {
                // Create new AttackRange GameObject
                GameObject attackGO = new GameObject("AttackRange");
                attackGO.transform.SetParent(transform);
                attackGO.transform.localPosition = Vector3.zero;
                attackGO.layer = gameObject.layer;
                
                _attackTrigger = attackGO.AddComponent<SphereCollider>();
                _attackTrigger.isTrigger = true;
                
                attackChild = attackGO.transform;
            }
            
            // Ensure EnemyAttackTrigger component exists
            EnemyAttackTrigger attackTrigger = attackChild.GetComponent<EnemyAttackTrigger>();
            if (attackTrigger == null)
            {
                attackTrigger = attackChild.gameObject.AddComponent<EnemyAttackTrigger>();
            }

            if (_baseStats != null)
            {
                _attackTrigger.radius = _baseStats.normalAttackStats.range;
            }

            attackTrigger.Initialize(this);
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
                BoxCollider hitboxCollider = damageHitboxGO.AddComponent<BoxCollider>();
                hitboxCollider.isTrigger = true;
                hitboxCollider.size = new Vector3(1.5f, 1.5f, 1.5f); // Default attack hitbox radius
                
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
        
        private void SetupHitboxSystem()
        {
            // Try to find existing weakpoints system
            Transform visualChild = _visualTransform ?? transform.Find("Visual");
            if (visualChild == null)
            {
                Debug.LogWarning($"EnemyMonoBehaviour: No Visual child found for hitbox system setup on {gameObject.name}");
                return;
            }
            
            SetupEnemyHitboxManagerComponent(visualChild.gameObject);
        }
        
        private void SetupEnemyHitboxManagerComponent(GameObject hitboxManagerObject)
        {
            // Check if EnemyHitboxManager already exists
            EnemyHitboxManager existingActivator = hitboxManagerObject.GetComponent<EnemyHitboxManager>();
            
            if (existingActivator != null)
            {
                existingActivator.Initialize(this);
                _hitboxManager = existingActivator;
                Debug.Log($"EnemyMonoBehaviour: Initialized existing EnemyHitboxManager on {hitboxManagerObject.name}");
            }
            else
            {
                EnemyHitboxManager newActivator = hitboxManagerObject.AddComponent<EnemyHitboxManager>();
                newActivator.Initialize(this);
                _hitboxManager = newActivator;
                Debug.Log($"EnemyMonoBehaviour: Added and initialized new EnemyHitboxManager on {hitboxManagerObject.name}");
            }
        }
        
        private void SetupWaveUI()
        {
            if(_waveUI == null)
            {
                Transform waveUIChild = transform.Find("WaveUI");
                if(waveUIChild != null)
                {
                    _waveUI = waveUIChild.gameObject;
                    Debug.Log($"EnemyMonoBehaviour: Found WaveUI child object: {waveUIChild.name}");
                }
            }

            if(_waveUI == null)
            {
                Debug.LogWarning($"EnemyMonoBehaviour: No WaveUI found on {gameObject.name}. UI wave will be disabled.");
                return;
            }

            if(_waveUIText == null)
            {
                Transform textChild = _waveUI.transform.Find("Text");
                if(textChild != null)
                {
                    _waveUIText = textChild.GetComponent<TextMeshProUGUI>();
                }
            }

            if (_waveUIText == null)
            {
                Debug.LogWarning($"EnemyMonoBehaviour: No TextMeshProUGUI component found in WaveUI on {gameObject.name}");
            }
            else
            {
                Debug.Log($"WeaponMonoBehaviour: Found TextMeshProUGUI component for interaction UI");
                _waveUIText.text = "F";
            }

            if(_waveUI != null)
            {
                _waveUI.SetActive(false);
            }

            Debug.Log($"EnemyMonoBehaviour: Wave UI setup complete");
        }
        
        private void SetupPatrolWaypoints()
        {
            // Validate patrol waypoints
            if (!HasValidPatrolWaypoints)
            {
                Debug.LogWarning($"EnemyMonoBehaviour: {gameObject.name} has invalid patrol waypoints. Using default points.");
                
                // Set default waypoints if none are configured
                if(_patrolPointA == null)
                {
                    _patrolPointA = new GameObject("PatrolPointA").transform;
                    _patrolPointA.SetParent(transform);
                    _patrolPointA.localPosition = Vector3.left * 3f;
                }
                if(_patrolPointB == null)
                {
                    _patrolPointB = new GameObject("PatrolPointB").transform;
                    _patrolPointB.SetParent(transform);
                    _patrolPointB.localPosition = Vector3.right * 3f;
                }
            }
            
            // Pass waypoints and configuration to controller if it's initialized
            if (_isInitialized && _enemyController != null)
            {
                _enemyController.SetPatrolWaypoints(PatrolWaypointA, PatrolWaypointB);
                _enemyController.SetPatrolConfiguration(
                    _waitAtWaypointDuration
                );
            }
            
            Debug.Log($"EnemyMonoBehaviour: Patrol waypoints set - A: {PatrolWaypointA}, B: {PatrolWaypointB}");
        }
        
        private void VerifyDetectionSystem()
        {            
            // Check attack collider and trigger component (detection now handled by Vision system)
            if (_attackTrigger != null)
            {
                EnemyAttackTrigger attackTrigger = _attackTrigger.GetComponent<EnemyAttackTrigger>();
                if (attackTrigger == null)
                {
                    attackTrigger = _attackTrigger.gameObject.AddComponent<EnemyAttackTrigger>();
                    attackTrigger.Initialize(this);
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

        /// <summary>
        /// Take damage using the new damage system
        /// Supports multiple damage types: Physical Health, Core Health
        /// All damage types from the same attack are processed together
        /// </summary>
        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!IsInitialized || damageInfo.damages == null) return;
            
            // Delegate to controller's unified damage handling
            // Controller processes all damage types from the DamageInfo together
            _enemyController.TakeDamage(damageInfo);

            // Visual and audio feedback
            ShowDamageEffect(damageInfo);
            PlayHitAudio(damageInfo);
        }

        #endregion

        #region IDamageable, IHasBalance, IHasCoreHealth Properties

        // IHasBalance implementation
        /// <summary>
        /// Current balance value (stance/posture system)
        /// </summary>
        public float CurrentBalance => IsInitialized ? _enemyController.Stats.currentBalance : 0f;

        /// <summary>
        /// Maximum balance value
        /// </summary>
        public float MaxBalance => IsInitialized ? _enemyController.Stats.maxBalance : 0f;

        // IHasCoreHealth implementation
        /// <summary>
        /// Core health state
        /// </summary>
        public CoreHealthState CoreState => IsInitialized && _enemyController.Stats.crystalCore != null 
            ? _enemyController.Stats.crystalCore.CoreHealthState 
            : CoreHealthState.Destroyed;

        /// <summary>
        /// Current core health value
        /// </summary>
        public float CurrentCoreHealth => IsInitialized && _enemyController.Stats.crystalCore != null 
            ? _enemyController.Stats.crystalCore.CurrentCoreHealth 
            : 0f;

        /// <summary>
        /// Maximum core health value
        /// </summary>
        public float MaxCoreHealth => IsInitialized && _enemyController.Stats.crystalCore != null 
            ? _enemyController.Stats.crystalCore.MaxCoreHealth 
            : 0f;

        #endregion

        #region Event Handlers

        private void HandleBalanceChanged(float current, float max)
        {
            // Balance UI updates would go here
        }

        private void HandleCoreHealthChanged(float current, float max)
        {
            // Core health UI updates would go here
        }

        private void HandleUnbalanced()
        {
            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} became unbalanced - visual effects");
            SetMaterial(_damageMaterial);
        }

        private void HandleDeath()
        {
            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} entered death state - visual effects only");
            SetMaterial(_damageMaterial);
        }

        /// <summary>
        /// Handle unbalanced state started
        /// </summary>
        private void HandleUnbalancedStarted()
        {
            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} started unbalanced state");
            SetMaterial(_damageMaterial);
        }

        /// <summary>
        /// Handle unbalanced state completed
        /// </summary>
        private void HandleUnbalancedCompleted()
        {
            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} completed unbalanced state");
            SetMaterial(_normalMaterial);
        }

        /// <summary>
        /// Handle core exposure started - set material to revival material
        /// </summary>
        private void HandleCoreExposureStarted()
        {
            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} core exposure started");
            SetMaterial(_revivalMaterial);
        }

        /// <summary>
        /// Handle core exposure completed - set material to normal material
        /// </summary>
        private void HandleCoreExposureCompleted()
        {
            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} core exposure completed");
            SetMaterial(_normalMaterial);
        }

        /// <summary>
        /// Handle attack launched - log attack details
        /// </summary>
        private void HandleAttackLaunched(float damage)
        {
            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} launched attack for {damage} damage");
        }

        /// <summary>
        /// Handle state changed - log state change
        /// </summary>
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
                SetMaterial(_damageMaterial);
                yield return new WaitForSeconds(_baseStats.damageFlashDuration);
                SetMaterial(originalMaterial);
            }
        }

        private void SetMaterial(Material material)
        {
            if (_bodyRenderer != null && material != null)
            {
                _bodyRenderer.material = material;
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

        public void PlayDeathAudio()
        {
            if (_audioService == null || !_baseStats.enableAudio) return;

            AudioClipType deathClipType = GetDeathAudioClipType();
            _audioService.PlaySFX3D(deathClipType, transform.position, 0.9f, 1f);
        }

        private AudioClipType GetHitAudioClipType(DamageInfo damageInfo)
        {
            string enemyName = gameObject.name.ToLower();
            return AudioClipType.EnemyHit;
        }

        private AudioClipType GetDeathAudioClipType()
        {
            string enemyName = gameObject.name.ToLower();
            return AudioClipType.EnemyDeath;
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Reset enemy to full balance and health
        /// </summary>
        public void ResetEnemy()
        {
            if (!IsInitialized) return;

            _enemyController.Stats.FullRestore();
            _enemyController.Stats.crystalCore.FullRestoreCoreHealth();
            SetMaterial(_normalMaterial);
            
            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} reset to full balance and health");
        }

        /// <summary>
        /// Get enemy statistics
        /// </summary>
        public string GetStats()
        {
            if (!IsInitialized) return "Not initialized";
            return _enemyController.GetStats();
        }

        #endregion

        #region Trigger System

        /// <summary>
        /// 处理触发器进入事件
        /// </summary>
        public void HandleTriggerEnter(Collider other)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning($"EnemyMonoBehaviour: HandleTriggerEnter called but not initialized on {gameObject.name}");
                return;
            }

            // 只检测玩家
            if (!other.CompareTag("Player"))
            {
                Debug.Log($"EnemyMonoBehaviour: Trigger enter from non-Player object: {other.name} with tag: {other.tag}");
                return;
            }

            Debug.Log($"EnemyMonoBehaviour: {gameObject.name} detected Player in attack range trigger");

            _enemyController.SetPlayerInAttackRange(true);
        }

        /// <summary>
        /// 处理触发器退出事件
        /// </summary>
        public void HandleTriggerExit(Collider other)
        {
            if (!IsInitialized) return;
            
            // 只检测玩家
            if (!other.CompareTag("Player")) return;
            
            _enemyController.SetPlayerInAttackRange(false);
        }

        /// <summary>
        /// 处理触发器停留事件
        /// </summary>
        public void HandleTriggerStay(Collider other)
        {
            if (!IsInitialized) return;

            // 玩家进入攻击范围
            _enemyController.SetPlayerInAttackRange(true);
        }

        #endregion

        #region Wave UI Control

        /// <summary>
        /// Show wave UI (called by EnemyHitboxManager when Core hitbox is enabled)
        /// </summary>
        public void ShowWaveUI()
        {
            if (_waveUI != null)
            {
                _waveUI.SetActive(true);
                // Default to white color
                SetWaveUIColor(Color.white);
                
                if (_showDebugInfo)
                {
                    Debug.Log($"EnemyMonoBehaviour: {gameObject.name} showing wave UI");
                }
            }
        }

        /// <summary>
        /// Hide wave UI (called by EnemyHitboxManager when Core hitbox is disabled)
        /// </summary>
        public void HideWaveUI()
        {
            if (_waveUI != null)
            {
                _waveUI.SetActive(false);
                
                if (_showDebugInfo)
                {
                    Debug.Log($"EnemyMonoBehaviour: {gameObject.name} hiding wave UI");
                }
            }
        }

        /// <summary>
        /// Set wave UI text color (called by WaveAttackTrigger for closest target indication)
        /// </summary>
        /// <param name="color">Color to set (red for closest target, white for others)</param>
        public void SetWaveUIColor(Color color)
        {
            if (_waveUIText != null)
            {
                _waveUIText.color = color;
                
                if (_showDebugInfo)
                {
                    string colorName = color == Color.red ? "red" : "white";
                    Debug.Log($"EnemyMonoBehaviour: {gameObject.name} set wave UI color to {colorName}");
                }
            }
        }

        #endregion

        #region Debug

        private void DrawDebugInfo()
        {
            if (!IsInitialized) return;

            var stats = _enemyController.Stats;
            
            Debug.Log($"Enemy {gameObject.name}: Balance: {stats.currentBalance:F1}/{stats.maxBalance}, " +
                     $"Core Energy: {stats.crystalCore.CurrentEnergy:F1}/{stats.crystalCore.MaxEnergy}, " +
                     $"Core Health: {stats.crystalCore.CurrentCoreHealth:F1}/{stats.crystalCore.MaxCoreHealth}, " +
                     $"State: {_enemyController.CurrentState}");
        }

        void OnDrawGizmos()
        {
            if (!IsInitialized || !_baseStats.showBalanceBar) return;

            // Draw balance and health bars
            Vector3 barPosition = transform.position + Vector3.up * 2f;
            float barWidth = 2f;
            float barHeight = 0.2f;

            // balance bar (top bar) - yellow/orange color
            Vector3 balanceBarCenter = barPosition + Vector3.up * barHeight * 0.6f;
            Gizmos.color = new Color(0.8f, 0.4f, 0.1f); // Dark orange background
            Gizmos.DrawCube(balanceBarCenter, new Vector3(barWidth, barHeight * 0.5f, 0.1f));
            
            float balancePercentage = _enemyController.Stats.BalancePercentage;
            Gizmos.color = new Color(1f, 0.8f, 0f); // Bright yellow for balance
            Vector3 balanceBarSize = new Vector3(barWidth * balancePercentage, barHeight * 0.5f, 0.1f);
            Vector3 balanceBarPosition = balanceBarCenter + Vector3.left * (barWidth * (1f - balancePercentage) * 0.5f);
            Gizmos.DrawCube(balanceBarPosition, balanceBarSize);
            
            // core health (middle bar)
            Gizmos.color = Color.blue;
            Gizmos.DrawCube(barPosition, new Vector3(barWidth, barHeight * 0.5f, 0.1f));
            
            float corePercentage = _enemyController.Stats.crystalCore.CoreHealthPercentage;
            Gizmos.color = Color.cyan;
            Vector3 coreBarSize = new Vector3(barWidth * corePercentage, barHeight * 0.5f, 0.1f);
            Vector3 coreBarPosition = barPosition + Vector3.left * (barWidth * (1f - corePercentage) * 0.5f);
            Gizmos.DrawCube(coreBarPosition, coreBarSize);
            
            // energy bar (bottom bar) - replaces chaos bar
            Vector3 energyBarPosition = barPosition + Vector3.down * barHeight * 0.6f;
            Gizmos.color = Color.magenta;
            Gizmos.DrawCube(energyBarPosition, new Vector3(barWidth, barHeight * 0.5f, 0.1f));
            
            float energyPercentage = _enemyController.Stats.crystalCore.EnergyPercentage;
            Gizmos.color = Color.magenta;
            Vector3 energyBarSize = new Vector3(barWidth * energyPercentage, barHeight * 0.5f, 0.1f);
            Vector3 energyBarFillPosition = energyBarPosition + Vector3.left * (barWidth * (1f - energyPercentage) * 0.5f);
            Gizmos.DrawCube(energyBarFillPosition, energyBarSize);
            
            // Border
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(barPosition, new Vector3(barWidth, barHeight * 0.5f, 0.1f));
            Gizmos.DrawWireCube(balanceBarCenter, new Vector3(barWidth, barHeight * 0.5f, 0.1f));
            Gizmos.DrawWireCube(energyBarPosition, new Vector3(barWidth, barHeight * 0.5f, 0.1f));
        }

        void OnDrawGizmosSelected()
        { 
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
    }
}
