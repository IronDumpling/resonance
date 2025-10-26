using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;
using Resonance.Items;
using Resonance.Enemies.Triggers;
using Resonance.Interfaces;
using Resonance.Interfaces.Services;
using Resonance.Utilities;
using Resonance.Utilities.Types;

namespace Resonance.Player.Shooting
{
    /// <summary>
    /// HitScan shooting system
    /// Two-stage shooting:
    /// 1. Mouse raycast → Target point (Ground/Enemy/Objects)
    /// 2. Player position → Target point → Actual hit point
    /// Integrate accuracy and recoil system
    /// </summary>
    public class ShootingSystem
    {
        // Shooting configuration
        private LayerMask _targetLayerMask = -1; // Shooting target detection layer
        private LayerMask _mouseRaycastLayerMask = -1; // Mouse raycast detection layer (Ground, Enemy, Objects)
        
        // Camera reference
        private Camera _mainCamera;
        private Transform _playerTransform;
        
        // Audio service reference
        private IAudioService _audioService;
        
        // Camera impulse reference
        private CinemachineImpulseSource _impulseSource;
        
        // Weapon systems
        private WeaponAccuracySystem _accuracySystem;
        private WeaponRecoilSystem _recoilSystem;
        private WeaponDataAsset _currentWeapon;
        
        // Shooting line visual effect
        private LineRenderer _shootingLineRenderer;  // Flashing line during shooting
        private LineRenderer _aimingLineRenderer;    // Continuous line during aiming
        private float _lineDisplayDuration = 0.1f;
        private bool _showShootingLine = true;
        private bool _showAimingLine = true;
        
        // Shooting statistics
        private int _totalShots = 0;
        private int _hits = 0;
        
        // Events
        public System.Action<Vector3, float> OnShoot; // Shooting position, damage
        public System.Action<Vector3, GameObject, float> OnHit; // Hit position, target, damage
        public System.Action<Vector3> OnMiss; // Missed position

        public ShootingSystem(GameObject playerObject)
        {
            _playerTransform = playerObject.transform;
            SetupCamera();
            SetupLineRenderers(playerObject);
            SetupAudioService();
            SetupCameraImpulse(playerObject);
            
            // Set default layer masks
            SetDefaultLayerMasks();
        }
        
        /// <summary>
        /// Set audio service reference
        /// </summary>
        private void SetupAudioService()
        {
            _audioService = ServiceRegistry.Get<IAudioService>();
            if (_audioService == null)
            {
                Debug.LogWarning("ShootingSystem: AudioService not found. Audio effects will be disabled.");
            }
            else
            {
                Debug.Log("ShootingSystem: AudioService connected successfully");
            }
        }

        /// <summary>
        /// Set camera impulse source reference
        /// </summary>
        private void SetupCameraImpulse(GameObject playerObject)
        {
            _impulseSource = playerObject.GetComponent<CinemachineImpulseSource>();
            if (_impulseSource == null)
            {
                Debug.LogWarning("ShootingSystem: CinemachineImpulseSource not found on player object. Camera shake will be disabled.");
            }
            else
            {
                Debug.Log("ShootingSystem: CinemachineImpulseSource connected successfully");
            }
        }

        /// <summary>
        /// Set default layer masks
        /// </summary>
        private void SetDefaultLayerMasks()
        {
            // Mouse raycast detection layer
            _mouseRaycastLayerMask = (1 << 6) | (1 << 8); // Environment, Enemy
            
            // Shooting target detection layer
            _targetLayerMask = (1 << 6) | (1 << 8); // Environment, Enemy
            
            Debug.Log($"ShootingSystem: Set default layer masks - Mouse: {_mouseRaycastLayerMask}, Target: {_targetLayerMask}");
        }

        #region Weapon System Management

        /// <summary>
        /// Initialize weapon systems with gun data
        /// Called when equipping a weapon or entering aiming state
        /// </summary>
        public void InitializeWeapon(WeaponDataAsset weaponData)
        {
            if (weaponData == null)
            {
                Debug.LogError("ShootingSystem: Cannot initialize with null weapon data");
                return;
            }
            
            _currentWeapon = weaponData;
            
            // Initialize accuracy system
            if (weaponData.accuracyConfig != null)
            {
                _accuracySystem = new WeaponAccuracySystem();
                _accuracySystem.Initialize(weaponData.accuracyConfig);
                Debug.Log($"ShootingSystem: Initialized accuracy system for {weaponData.weaponName}");
            }
            else
            {
                Debug.LogError($"ShootingSystem: Weapon {weaponData.weaponName} missing accuracyConfig!");
            }
            
            // Initialize recoil system
            if (weaponData.recoilConfig != null)
            {
                _recoilSystem = new WeaponRecoilSystem();
                _recoilSystem.Initialize(weaponData.recoilConfig);
                Debug.Log($"ShootingSystem: Initialized recoil system for {weaponData.weaponName}");
            }
            else
            {
                Debug.LogError($"ShootingSystem: Weapon {weaponData.weaponName} missing recoilConfig!");
            }
        }
        
        /// <summary>
        /// Update weapon systems each frame (called from aiming state)
        /// </summary>
        public void UpdateWeaponSystems(float deltaTime, bool isAiming, bool isMoving)
        {
            // Update accuracy system
            if (_accuracySystem != null && _accuracySystem.IsInitialized() && isAiming)
            {
                Vector2 currentMousePosition = UnityEngine.InputSystem.Mouse.current?.position.ReadValue() ?? Vector2.zero;
                _accuracySystem.UpdateAccuracy(deltaTime, isAiming, isMoving, currentMousePosition);
            }
            
            // Update recoil system
            if (_recoilSystem != null && _recoilSystem.IsInitialized())
            {
                _recoilSystem.UpdateRecoil(deltaTime);
            }
        }
        
        /// <summary>
        /// Cleanup weapon systems
        /// Called when unequipping weapon or exiting aiming state
        /// </summary>
        public void CleanupWeapon()
        {
            _accuracySystem = null;
            _recoilSystem = null;
            _currentWeapon = null;
            Debug.Log("ShootingSystem: Weapon systems cleaned up");
        }
        
        /// <summary>
        /// Get current crosshair radius (for UI display)
        /// </summary>
        public float GetCurrentCrosshairRadius()
        {
            return _accuracySystem?.GetCurrentRadius() ?? 0f;
        }
        
        /// <summary>
        /// Get accuracy percentage (0-1, 1 = perfect)
        /// </summary>
        public float GetAccuracyPercentage()
        {
            return _accuracySystem?.GetAccuracyPercentage() ?? 0f;
        }

        #endregion
        
        #region Public Methods

        /// <summary>
        /// Perform shoot
        /// Step 1: Mouse raycast → Get target point
        /// Step 2: Player → Target point → Actual hit point
        /// Integrate accuracy and recoil system
        /// </summary>
        /// <param name="shootOrigin">Shoot origin</param>
        /// <param name="gunData">Weapon data</param>
        /// <param name="isAiming">Is aiming</param>
        /// <returns>Shooting result</returns>
        public ShootingResult PerformShoot(Vector3 shootOrigin, WeaponDataAsset gunData, bool isAiming = true)
        {
            if (gunData == null)
            {
                Debug.LogError("ShootingSystem: WeaponData is null");
                return new ShootingResult { success = false };
            }

            if (_mainCamera == null)
            {
                Debug.LogError("ShootingSystem: No camera found for mouse raycast");
                return new ShootingResult { success = false };
            }

            _totalShots++;

            // Step 1: Get the base target point from mouse raycast (with recoil applied)
            Vector3 baseTargetPoint = GetMouseTargetPoint();
            
            // Step 2: Calculate shooting direction from player to final target point
            Vector3 shootDirection = (baseTargetPoint - shootOrigin).normalized;
            
            // Step 3: Perform raycast detection
            RaycastHit hitInfo;
            bool hasHit = Physics.Raycast(shootOrigin, shootDirection, out hitInfo, gunData.range, _targetLayerMask);
            Vector3 endPoint = hasHit ? hitInfo.point : baseTargetPoint;
            
            // Step 4: Get damage multiplier from accuracy system
            float damageMultiplier = 1.0f;
            if (_accuracySystem != null && _accuracySystem.IsInitialized())
            {
                damageMultiplier = _accuracySystem.GetDamageMultiplier();
            }
            
            // Create base damage dictionary (weapon's raw damage without multipliers)
            Damages baseDamages = gunData.damages;
            
            // Calculate total base damage for logging
            float baseTotalDamage = gunData.GetTotalDamage();
            float finalTotalDamage = baseTotalDamage * damageMultiplier;
            
            // Debug information
            Debug.Log($"ShootingSystem: Shooting from {shootOrigin} to {baseTargetPoint} " +
                     $"(total damage: {finalTotalDamage:F1} (base: {baseTotalDamage}, multiplier: {damageMultiplier:F2}))");
            
            // Show shooting line
            if (_showShootingLine)
            {
                ShowShootingLine(shootOrigin, endPoint);
            }
            
            // Apply recoil with current accuracy
            if (_recoilSystem != null && _recoilSystem.IsInitialized())
            {
                float currentAccuracy = _accuracySystem?.GetAccuracyPercentage() ?? 1.0f;
                _recoilSystem.ApplyRecoil(currentAccuracy);
            }
            
            // Notify accuracy system of shot
            if (_accuracySystem != null && _accuracySystem.IsInitialized())
            {
                _accuracySystem.OnShoot();
            }
            
            // Play shooting audio
            PlayShootingAudio(shootOrigin, gunData);
            
            // Trigger camera impulse
            TriggerCameraImpulse(gunData, finalTotalDamage);
            
            // Trigger shooting event with total final damage
            OnShoot?.Invoke(shootOrigin, finalTotalDamage);
            
            // Create shooting result
            ShootingResult result = new ShootingResult
            {
                success = true,
                hasHit = hasHit,
                startPosition = shootOrigin,
                endPosition = endPoint,
                direction = shootDirection,
                range = gunData.range,
                baseDamages = baseDamages, // Copy base damages
                actualDamages = new Damages(), // Will be populated in ProcessHit
                mouseTargetPoint = baseTargetPoint
            };

            if (hasHit)
            {
                result.hitObject = hitInfo.collider.gameObject;
                result.hitPoint = hitInfo.point;
                result.hitNormal = hitInfo.normal;
                result.hitDistance = hitInfo.distance;
                
                // Process damage and get actual damage dealt (pass damage multiplier to gunData.CreateDamageInfo)
                result.actualDamages = ProcessHit(hitInfo, shootOrigin, gunData, damageMultiplier);
                _hits++;
                
                float totalActualDamage = result.GetTotalActualDamage();
                Debug.Log($"ShootingSystem: Hit {hitInfo.collider.name} at distance {hitInfo.distance:F2}m " +
                         $"for {totalActualDamage:F1} total actual damage (base total: {baseTotalDamage:F1}) - {result.GetDamageBreakdown()}");
            }
            else
            {
                OnMiss?.Invoke(endPoint);
                Debug.Log($"ShootingSystem: Shot missed, aimed at {baseTargetPoint}");
            }

            return result;
        }

        /// <summary>
        /// Set target detection layer
        /// </summary>
        /// <param name="layerMask">Layer mask</param>
        public void SetTargetLayerMask(LayerMask layerMask)
        {
            _targetLayerMask = layerMask;
        }

        /// <summary>
        /// Set mouse raycast layer mask
        /// </summary>
        /// <param name="layerMask">Layer mask</param>
        public void SetMouseRaycastLayerMask(LayerMask layerMask)
        {
            _mouseRaycastLayerMask = layerMask;
        }

        /// <summary>
        /// Set whether to show shooting line
        /// </summary>
        /// <param name="show">Show</param>
        public void SetShowShootingLine(bool show)
        {
            _showShootingLine = show;
        }

        /// <summary>
        /// Set whether to show aiming line
        /// </summary>
        /// <param name="show">Show</param>
        public void SetShowAimingLine(bool show)
        {
            _showAimingLine = show;
            if (!show && _aimingLineRenderer != null)
            {
                _aimingLineRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Update aiming line display (called in aiming state)
        /// </summary>
        /// <param name="shootOrigin">Shoot origin</param>
        public void UpdateAimingLine(Vector3 shootOrigin)
        {
            if (!_showAimingLine || _aimingLineRenderer == null) return;

            // Get mouse target point
            Vector3 targetPoint = GetMouseTargetPoint();
            
            // Show aiming line
            ShowAimingLine(shootOrigin, targetPoint);
        }

        /// <summary>
        /// Hide aiming line
        /// </summary>
        public void HideAimingLine()
        {
            if (_aimingLineRenderer != null)
            {
                _aimingLineRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Get current mouse target point (public method)
        /// Use the same logic as the shooting system, ensure the player's orientation and shooting direction are consistent
        /// </summary>
        /// <returns>Mouse target point</returns>
        public Vector3 GetCurrentMouseTargetPoint()
        {
            return GetMouseTargetPoint();
        }

        /// <summary>
        /// Preview the actual shooting end point without performing the shot
        /// This calculates the real end point by performing raycast from player to target
        /// Used by UI systems to show where the shot will actually land
        /// </summary>
        /// <param name="shootOrigin">Shoot origin position</param>
        /// <param name="gunData">Weapon data for range</param>
        /// <returns>The actual end point (hit point or base target point)</returns>
        public Vector3 PreviewShootingEndPoint(Vector3 shootOrigin, WeaponDataAsset gunData)
        {
            if (gunData == null)
            {
                Debug.LogWarning("ShootingSystem: PreviewShootingEndPoint called with null gunData");
                return shootOrigin + Vector3.forward * 10f;
            }

            // Step 1: Get the base target point from mouse raycast (with recoil applied)
            Vector3 baseTargetPoint = GetMouseTargetPoint();
            
            // Step 2: Calculate shooting direction from player to target point
            Vector3 shootDirection = (baseTargetPoint - shootOrigin).normalized;
            
            // Step 3: Perform raycast detection to get actual end point
            RaycastHit hitInfo;
            bool hasHit = Physics.Raycast(shootOrigin, shootDirection, out hitInfo, gunData.range, _targetLayerMask);
            
            // Return hit point if we hit something, otherwise return base target point
            return hasHit ? hitInfo.point : baseTargetPoint;
        }

        /// <summary>
        /// Set shooting line display duration
        /// </summary>
        /// <param name="duration">Display duration (seconds)</param>
        public void SetLineDisplayDuration(float duration)
        {
            _lineDisplayDuration = Mathf.Max(0.01f, duration);
        }

        /// <summary>
        /// Get shooting statistics
        /// </summary>
        /// <returns>Accuracy</returns>
        public float GetAccuracy()
        {
            if (_totalShots == 0) return 0f;
            return (float)_hits / _totalShots;
        }

        /// <summary>
        /// Reset shooting statistics
        /// </summary>
        public void ResetStats()
        {
            _totalShots = 0;
            _hits = 0;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Set camera reference
        /// </summary>
        private void SetupCamera()
        {
            // First try to find CameraManager's main camera
            var cameraManager = Object.FindAnyObjectByType<Resonance.Cameras.CameraManager>();
            if (cameraManager != null && cameraManager.Brain != null)
            {
                _mainCamera = cameraManager.Brain.OutputCamera;
                Debug.Log("ShootingSystem: Found camera from CameraManager");
            }
            
            // Fallback: find Main Camera
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera != null)
                {
                    Debug.Log("ShootingSystem: Using Camera.main");
                }
            }
            
            // Last fallback: find any camera
            if (_mainCamera == null)
            {
                _mainCamera = Object.FindAnyObjectByType<Camera>();
                if (_mainCamera != null)
                {
                    Debug.Log("ShootingSystem: Using first found camera");
                }
            }
            
            if (_mainCamera == null)
            {
                Debug.LogError("ShootingSystem: No camera found! Mouse-based shooting will not work.");
            }
        }

        /// <summary>
        /// Get mouse target point (apply recoil offset)
        /// Step 1: Mouse raycast → Ground/Enemy/Objects
        /// If not hit, use plane intersection as backup
        /// Finally apply recoil offset
        /// </summary>
        /// <returns>Target point world coordinates (with recoil)</returns>
        private Vector3 GetMouseTargetPoint()
        {
            if (_mainCamera == null)
            {
                return _playerTransform.position + _playerTransform.forward * 10f;
            }

            // Get mouse position
            Vector2 mousePosition = UnityEngine.InputSystem.Mouse.current?.position.ReadValue() ?? Vector2.zero;
            
            if (mousePosition == Vector2.zero)
            {
                Debug.LogWarning("ShootingSystem: Mouse position is zero");
                return _playerTransform.position + _playerTransform.forward * 10f;
            }

            // Create a ray from the camera through the mouse
            Ray mouseRay = _mainCamera.ScreenPointToRay(mousePosition);
            
            // Step 1: Try raycast detection Environment/Enemy/Objects
            RaycastHit hitInfo;
            Vector3 baseTargetPoint;
            if (Physics.Raycast(mouseRay, out hitInfo, Mathf.Infinity, _mouseRaycastLayerMask))
            {
                baseTargetPoint = hitInfo.point;
            }
            else
            {
                // Backup: Use plane intersection
                baseTargetPoint = IntersectPlane(mouseRay, _playerTransform.position.y);
            }
            
            // Apply recoil offset
            if (_recoilSystem != null && _recoilSystem.IsInitialized())
            {
                Vector3 recoilOffset = _recoilSystem.GetRecoilOffset();
                baseTargetPoint += recoilOffset;
            }
            
            return baseTargetPoint;
        }

        /// <summary>
        /// Calculate the intersection of the ray with the specified height plane
        /// </summary>
        /// <param name="ray">Ray</param>
        /// <param name="y">Plane height</param>
        /// <returns>Intersection world coordinates</returns>
        private Vector3 IntersectPlane(Ray ray, float y)
        {
            Plane plane = new Plane(Vector3.up, new Vector3(0f, y, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }
            
            // If not hit, keep the direction
            return _playerTransform.position + _playerTransform.forward * 10f;
        }

        /// <summary>
        /// Set shooting line renderer
        /// </summary>
        /// <param name="playerObject">Player object</param>
        private void SetupLineRenderers(GameObject playerObject)
        {
            // Create shooting line (red, flashing)
            GameObject shootingLineObject = new GameObject("ShootingLine");
            shootingLineObject.transform.SetParent(playerObject.transform);
            
            _shootingLineRenderer = shootingLineObject.AddComponent<LineRenderer>();
            _shootingLineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            Gradient shootingGradient = new Gradient();
            shootingGradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.red, 0.0f), new GradientColorKey(Color.red, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            );
            _shootingLineRenderer.colorGradient = shootingGradient;
            _shootingLineRenderer.startWidth = 0.03f;
            _shootingLineRenderer.endWidth = 0.02f;
            _shootingLineRenderer.positionCount = 2;
            _shootingLineRenderer.enabled = false;
            
            // Create aiming line (green, continuous)
            GameObject aimingLineObject = new GameObject("AimingLine");
            aimingLineObject.transform.SetParent(playerObject.transform);
            
            _aimingLineRenderer = aimingLineObject.AddComponent<LineRenderer>();
            _aimingLineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            Gradient aimingGradient = new Gradient();
            aimingGradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.yellow, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.7f, 0.0f), new GradientAlphaKey(0.5f, 1.0f) }
            );
            _aimingLineRenderer.colorGradient = aimingGradient;
            _aimingLineRenderer.startWidth = 0.015f;
            _aimingLineRenderer.endWidth = 0.01f;
            _aimingLineRenderer.positionCount = 2;
            _aimingLineRenderer.enabled = false;
            
            Debug.Log("ShootingSystem: Shooting and aiming line renderers setup complete");
        }

        /// <summary>
        /// Show aiming line
        /// </summary>
        /// <param name="start">Start position</param>
        /// <param name="end">End position</param>
        private void ShowAimingLine(Vector3 start, Vector3 end)
        {
            if (_aimingLineRenderer == null) return;

            _aimingLineRenderer.SetPosition(0, start);
            _aimingLineRenderer.SetPosition(1, end);
            _aimingLineRenderer.enabled = true;
        }

        /// <summary>
        /// Show shooting line
        /// </summary>
        /// <param name="start">Start position</param>
        /// <param name="end">End position</param>
        private void ShowShootingLine(Vector3 start, Vector3 end)
        {
            if (_shootingLineRenderer == null) return;

            _shootingLineRenderer.SetPosition(0, start);
            _shootingLineRenderer.SetPosition(1, end);
            _shootingLineRenderer.enabled = true;
            
            // Create a simple script to handle the coroutine
            LineDisplayController controller = _shootingLineRenderer.gameObject.GetComponent<LineDisplayController>();
            if (controller == null)
            {
                controller = _shootingLineRenderer.gameObject.AddComponent<LineDisplayController>();
            }
            controller.ShowLineTemporarily(_shootingLineRenderer, _lineDisplayDuration);
        }

        /// <summary>
        /// Process hit target
        /// </summary>
        /// <param name="hitInfo">Raycast hit info</param>
        /// <param name="damageSource">Damage source position</param>
        /// <param name="gunData">Weapon data asset (required)</param>
        /// <param name="damageMultiplier">Damage multiplier from accuracy</param>
        /// <returns>Dictionary of actual damage dealt by type</returns>
        private Damages ProcessHit(RaycastHit hitInfo, Vector3 damageSource, WeaponDataAsset gunData, float damageMultiplier = 1f)
        {
            GameObject hitObject = hitInfo.collider.gameObject;
            Damages actualDamages = new Damages();
            
            Debug.Log($"ShootingSystem: ProcessHit called for {hitObject.name} (Layer: {hitObject.layer})");
            
            // First check if it hit a weakpoint
            EnemyHitbox weakpointHitbox = hitObject.GetComponent<EnemyHitbox>();
            if (weakpointHitbox != null && weakpointHitbox.IsInitialized)
            {
                Debug.Log($"ShootingSystem: Hit weakpoint {hitObject.name}, delegating to EnemyHitbox");
                
                // Create damage info with all damage types and multiplier
                DamageInfo damageInfo = gunData.CreateDamageInfo(damageSource, _playerTransform.gameObject, damageMultiplier);
                
                // Let the weakpoint handle damage modification and application
                DamageInfo modifiedDamageInfo = weakpointHitbox.ProcessDamageHit(damageInfo);
                
                // Extract actual damages from modified damage info
                if (modifiedDamageInfo.damages != null)
                {
                    actualDamages = modifiedDamageInfo.damages;
                }
                
                float totalActualDamage = modifiedDamageInfo.GetTotalDamage();
                
                // Play audio and trigger event
                PlayHitAudio(hitInfo.point, hitObject);
                OnHit?.Invoke(hitInfo.point, hitObject, totalActualDamage);
                return actualDamages;
            }
            
            // If it's not a weakpoint, process IDamageable and IDestructible
            IDamageable damageable = hitObject.GetComponent<IDamageable>();
            IDestructible destructible = hitObject.GetComponent<IDestructible>();
            
            // Determine the GameObject reference to use
            GameObject damageableObject = hitObject;
            GameObject destructibleObject = hitObject;
            
            // If a component is found on the hit object, update the GameObject reference
            if (damageable != null)
            {
                damageableObject = (damageable as MonoBehaviour)?.gameObject ?? hitObject;
            }
            if (destructible != null)
            {
                destructibleObject = (destructible as MonoBehaviour)?.gameObject ?? hitObject;
            }
            
            if (damageable == null)
            {
                damageable = hitObject.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    damageableObject = (damageable as MonoBehaviour)?.gameObject ?? hitObject;
                    Debug.Log($"ShootingSystem: Found IDamageable on parent {damageableObject.name} of {hitObject.name}");
                }
            }
            
            if (destructible == null)
            {
                destructible = hitObject.GetComponentInParent<IDestructible>();
                if (destructible != null)
                {
                    destructibleObject = (destructible as MonoBehaviour)?.gameObject ?? hitObject;
                    Debug.Log($"ShootingSystem: Found IDestructible on parent {destructibleObject.name} of {hitObject.name}");
                }
            }
            
            // Process damageable objects
            if (damageable != null)
            {
                Debug.Log($"ShootingSystem: Found IDamageable on {damageableObject.name}");
                
                // Create damage info with all damage types and multiplier
                DamageInfo damageInfo = gunData.CreateDamageInfo(damageSource, _playerTransform.gameObject, damageMultiplier);
                damageable.TakeDamage(damageInfo);
                
                // Extract actual damages - assume all damage was dealt (no reduction tracking for now)
                if (damageInfo.damages != null)
                {
                    actualDamages = damageInfo.damages;
                }
                
                float totalActualDamage = damageInfo.GetTotalDamage();
                
                Debug.Log($"ShootingSystem: Dealt {totalActualDamage:F1} total damage to {damageableObject.name} " +
                         $"({gunData.GetDamageTypeDescription()})");
                
                PlayHitAudio(hitInfo.point, damageableObject);
                OnHit?.Invoke(hitInfo.point, damageableObject, totalActualDamage);
                return actualDamages;
            }

            // Process destructible objects
            if (destructible != null)
            {
                Debug.Log($"ShootingSystem: Found IDestructible on {destructibleObject.name}");
                
                // For destructible objects, use total damage as physical damage
                float totalDamage = gunData.GetTotalDamage() * damageMultiplier;
                destructible.TakeDamage(totalDamage, damageSource);
                
                // Record as physical health damage (destructible objects only take physical damage)
                actualDamages.SetDamage(DamageType.PhysicalHealth, totalDamage);
                
                PlayHitAudio(hitInfo.point, destructibleObject);
                OnHit?.Invoke(hitInfo.point, destructibleObject, totalDamage);
                Debug.Log($"ShootingSystem: Dealt {totalDamage:F1} total damage to destructible {destructibleObject.name}");
                return actualDamages;
            }

            // If it's not a damageable or destructible object, still trigger the hit event (for audio, particle effects, etc.)
            PlayHitAudio(hitInfo.point, hitObject);
            OnHit?.Invoke(hitInfo.point, hitObject, 0f);
            Debug.Log($"ShootingSystem: Hit non-damageable object {hitObject.name} - no damage dealt");
            return actualDamages; // Return empty dictionary
        }

        #endregion

        #region Audio Effects
        
        /// <summary>
        /// Play shooting audio
        /// </summary>
        /// <param name="shootOrigin">Shoot origin</param>
        /// <param name="gunData">Weapon data</param>
        private void PlayShootingAudio(Vector3 shootOrigin, WeaponDataAsset gunData)
        {
            if (_audioService == null) return;
            
            // According to the weapon type to select audio
            AudioClipType shootingClipType = GetShootingAudioClipType(gunData);
            
            // Play 3D shooting audio
            _audioService.PlaySFX3D(shootingClipType, shootOrigin, 0.8f, 1f);
            
            Debug.Log($"ShootingSystem: Played shooting audio {shootingClipType} at {shootOrigin}");
        }
        
        /// <summary>
        /// According to the weapon data to get the corresponding shooting audio type
        /// </summary>
        /// <param name="gunData">Weapon data</param>
        /// <returns>Audio type</returns>
        private AudioClipType GetShootingAudioClipType(WeaponDataAsset gunData)
        {
            // According to the weapon name or type to select audio
            // Here can be extended according to the actual weapon system
            string weaponName = gunData.weaponName.ToLower();
            
            if (weaponName.Contains("rifle"))
            {
                return AudioClipType.WeaponFireRifle;
            }
            else
            {
                // Default use pistol audio
                return AudioClipType.WeaponFirePistol;
            }
        }
        
        /// <summary>
        /// Play hit audio
        /// </summary>
        /// <param name="hitPoint">Hit position</param>
        /// <param name="hitObject">Hit object</param>
        private void PlayHitAudio(Vector3 hitPoint, GameObject hitObject)
        {
            if (_audioService == null) return;
            
            // According to the hit object's tag or layer to select audio
            AudioClipType hitClipType = GetHitAudioClipType(hitObject);
            
            // Play 3D hit audio
            _audioService.PlaySFX3D(hitClipType, hitPoint, 0.6f, 1f);
            
            Debug.Log($"ShootingSystem: Played hit audio {hitClipType} at {hitPoint}");
        }
        
        /// <summary>
        /// According to the hit object to get the corresponding hit audio type
        /// </summary>
        /// <param name="hitObject">Hit object</param>
        /// <returns>Audio type</returns>
        private AudioClipType GetHitAudioClipType(GameObject hitObject)
        {
            // According to the object's tag or name to select the appropriate hit audio
            string tag = hitObject.tag.ToLower();
            string name = hitObject.name.ToLower();
            
            if (tag.Contains("player") || name.Contains("player"))
            {
                return AudioClipType.PlayerHit;
            }
            else if (tag.Contains("enemy") || name.Contains("enemy"))
            {
                return AudioClipType.EnemyHit;
            }

            return AudioClipType.PlayerHit;
        }

        #endregion

        #region Camera Impulse

        /// <summary>
        /// Trigger camera impulse based on weapon configuration
        /// </summary>
        /// <param name="weaponData">Weapon data containing recoil configuration</param>
        /// <param name="totalDamage">Total damage dealt (for scaling)</param>
        private void TriggerCameraImpulse(WeaponDataAsset weaponData, float totalDamage)
        {
            if (_impulseSource == null) return;
            if (weaponData == null) return;

            // Check if impulse is enabled for this weapon
            if (!weaponData.recoilConfig.enableCameraImpulse)
            {
                return;
            }

            // Calculate impulse force
            float impulseForce = weaponData.recoilConfig.impulseForce;

            // Scale to damage if enabled
            if (weaponData.recoilConfig.scaleToDamage)
            {
                float damageScale = totalDamage * weaponData.recoilConfig.damageScaleFactor;
                impulseForce *= damageScale;
            }

            // Generate impulse with calculated force
            _impulseSource.GenerateImpulse(impulseForce);

            Debug.Log($"ShootingSystem: Camera impulse triggered with force {impulseForce:F2} " +
                     $"(base: {weaponData.recoilConfig.impulseForce}, damage: {totalDamage:F1})");
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Cleanup()
        {
            if (_shootingLineRenderer != null)
            {
                Object.Destroy(_shootingLineRenderer.gameObject);
            }
            
            if (_aimingLineRenderer != null)
            {
                Object.Destroy(_aimingLineRenderer.gameObject);
            }
            
            OnShoot = null;
            OnHit = null;
            OnMiss = null;
        }

        #endregion
    }
}
