using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Resonance.UI
{
    /// <summary>
    /// WorldUIManager handles World Space UI elements that are attached to game objects.
    /// It manages their lifecycle, visibility, and screen-space positioning automatically.
    /// </summary>
    public class WorldUIManager : MonoBehaviour
    {
        [Header("World UI Configuration")]
        [SerializeField] private Camera _uiCamera; // Camera used for world-to-screen calculations
        [SerializeField] private Canvas _worldUICanvas; // Canvas for world UI elements
        [SerializeField] private float _maxDisplayDistance = 50f; // Max distance to show UI
        [SerializeField] private float _updateInterval = 0.1f; // How often to update positions
        
        [Header("Performance Settings")]
        [SerializeField] private int _maxVisibleElements = 100; // Performance limit
        [SerializeField] private bool _enableCulling = true; // Distance-based culling
        [SerializeField] private bool _enableOcclusion = false; // Simple occlusion culling

        private Dictionary<Transform, WorldUIElement> _worldUIElements = new Dictionary<Transform, WorldUIElement>();
        private List<WorldUIElement> _visibleElements = new List<WorldUIElement>();
        private Coroutine _updateCoroutine;

        public Camera UICamera => _uiCamera;
        public Canvas WorldUICanvas => _worldUICanvas;

        void Start()
        {
            // Setup camera reference
            if (_uiCamera == null)
            {
                _uiCamera = Camera.main;
                if (_uiCamera == null)
                {
                    _uiCamera = FindAnyObjectByType<Camera>();
                }
            }

            // Setup canvas
            if (_worldUICanvas == null)
            {
                _worldUICanvas = GetComponent<Canvas>();
                if (_worldUICanvas == null)
                {
                    Debug.LogError("WorldUIManager: No Canvas found. Please assign a Canvas component.");
                }
            }

            // Start update coroutine
            _updateCoroutine = StartCoroutine(UpdateWorldUIElements());
            
            Debug.Log("WorldUIManager: Initialized successfully");
        }

        /// <summary>
        /// Register a world UI element with a target transform
        /// </summary>
        public void RegisterWorldUI(Transform target, GameObject uiPrefab, Vector3 offset = default)
        {
            if (target == null || uiPrefab == null) return;

            // Don't register duplicates
            if (_worldUIElements.ContainsKey(target))
            {
                Debug.LogWarning($"WorldUIManager: Target {target.name} already has a world UI element");
                return;
            }

            // Instantiate UI element
            GameObject uiInstance = Instantiate(uiPrefab, _worldUICanvas.transform);
            
            // Create world UI element data
            var worldUIElement = new WorldUIElement
            {
                target = target,
                uiElement = uiInstance,
                offset = offset,
                isVisible = false,
                lastDistance = float.MaxValue
            };

            // Register element
            _worldUIElements[target] = worldUIElement;
            
            // Initially hide the element
            uiInstance.SetActive(false);
            
            Debug.Log($"WorldUIManager: Registered world UI for {target.name}");
        }

        /// <summary>
        /// Unregister and destroy world UI element
        /// </summary>
        public void UnregisterWorldUI(Transform target)
        {
            if (target != null && _worldUIElements.TryGetValue(target, out WorldUIElement element))
            {
                if (element.uiElement != null)
                {
                    Destroy(element.uiElement);
                }
                
                _worldUIElements.Remove(target);
                _visibleElements.Remove(element);
                
                Debug.Log($"WorldUIManager: Unregistered world UI for {target.name}");
            }
        }

        /// <summary>
        /// Show/hide specific world UI element
        /// </summary>
        public void SetWorldUIVisible(Transform target, bool visible)
        {
            if (target != null && _worldUIElements.TryGetValue(target, out WorldUIElement element))
            {
                element.forceHidden = !visible;
                UpdateElementVisibility(element);
            }
        }

        /// <summary>
        /// Get world UI element for a target
        /// </summary>
        public GameObject GetWorldUI(Transform target)
        {
            if (target != null && _worldUIElements.TryGetValue(target, out WorldUIElement element))
            {
                return element.uiElement;
            }
            return null;
        }

        private IEnumerator UpdateWorldUIElements()
        {
            while (true)
            {
                yield return new WaitForSeconds(_updateInterval);
                
                if (_uiCamera == null) continue;

                _visibleElements.Clear();
                
                // Update all registered elements
                foreach (var kvp in _worldUIElements)
                {
                    var element = kvp.Value;
                    
                    // Skip if target is destroyed
                    if (element.target == null)
                    {
                        continue;
                    }

                    UpdateElementPosition(element);
                    UpdateElementVisibility(element);
                    
                    if (element.isVisible)
                    {
                        _visibleElements.Add(element);
                    }
                }

                // Apply performance limits
                if (_visibleElements.Count > _maxVisibleElements)
                {
                    LimitVisibleElements();
                }

                // Clean up destroyed targets
                CleanupDestroyedTargets();
            }
        }

        private void UpdateElementPosition(WorldUIElement element)
        {
            if (element.target == null || element.uiElement == null) return;

            // Calculate world position with offset
            Vector3 worldPosition = element.target.position + element.offset;
            
            // Convert to screen space
            Vector3 screenPosition = _uiCamera.WorldToScreenPoint(worldPosition);
            
            // Update UI element position
            element.uiElement.transform.position = screenPosition;
            
            // Calculate distance
            element.lastDistance = Vector3.Distance(_uiCamera.transform.position, worldPosition);
        }

        private void UpdateElementVisibility(WorldUIElement element)
        {
            if (element.target == null || element.uiElement == null) return;

            bool shouldBeVisible = true;

            // Check force hidden flag
            if (element.forceHidden)
            {
                shouldBeVisible = false;
            }
            // Check distance culling
            else if (_enableCulling && element.lastDistance > _maxDisplayDistance)
            {
                shouldBeVisible = false;
            }
            // Check if behind camera
            else if (element.lastDistance < 0)
            {
                shouldBeVisible = false;
            }
            // Check occlusion (simple version)
            else if (_enableOcclusion && IsOccluded(element))
            {
                shouldBeVisible = false;
            }

            // Apply visibility
            if (element.isVisible != shouldBeVisible)
            {
                element.isVisible = shouldBeVisible;
                element.uiElement.SetActive(shouldBeVisible);
            }
        }

        private bool IsOccluded(WorldUIElement element)
        {
            // Simple occlusion check using raycast
            Vector3 direction = element.target.position - _uiCamera.transform.position;
            
            if (Physics.Raycast(_uiCamera.transform.position, direction, out RaycastHit hit, element.lastDistance))
            {
                return hit.transform != element.target;
            }
            
            return false;
        }

        private void LimitVisibleElements()
        {
            // Sort by distance and hide farthest elements
            _visibleElements.Sort((a, b) => a.lastDistance.CompareTo(b.lastDistance));
            
            for (int i = _maxVisibleElements; i < _visibleElements.Count; i++)
            {
                _visibleElements[i].isVisible = false;
                _visibleElements[i].uiElement.SetActive(false);
            }
        }

        private void CleanupDestroyedTargets()
        {
            var keysToRemove = new List<Transform>();
            
            foreach (var kvp in _worldUIElements)
            {
                if (kvp.Key == null)
                {
                    keysToRemove.Add(kvp.Key);
                    if (kvp.Value.uiElement != null)
                    {
                        Destroy(kvp.Value.uiElement);
                    }
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _worldUIElements.Remove(key);
            }
        }

        void OnDestroy()
        {
            if (_updateCoroutine != null)
            {
                StopCoroutine(_updateCoroutine);
            }
            
            // Clean up all world UI elements
            foreach (var element in _worldUIElements.Values)
            {
                if (element.uiElement != null)
                {
                    Destroy(element.uiElement);
                }
            }
            
            _worldUIElements.Clear();
        }

        // Helper methods for common world UI types
        public void RegisterHealthBar(Transform target, GameObject healthBarPrefab)
        {
            Vector3 offset = Vector3.up * 2f; // Above the target
            RegisterWorldUI(target, healthBarPrefab, offset);
        }

        public void RegisterInteractionPrompt(Transform target, GameObject promptPrefab)
        {
            Vector3 offset = Vector3.up * 1.5f;
            RegisterWorldUI(target, promptPrefab, offset);
        }

        public void RegisterDamageNumber(Vector3 worldPosition, GameObject damageNumberPrefab, float duration = 2f)
        {
            GameObject instance = Instantiate(damageNumberPrefab, _worldUICanvas.transform);
            Vector3 screenPosition = _uiCamera.WorldToScreenPoint(worldPosition);
            instance.transform.position = screenPosition;
            
            // Auto-destroy after duration
            Destroy(instance, duration);
        }
    }

    [System.Serializable]
    public class WorldUIElement
    {
        public Transform target;
        public GameObject uiElement;
        public Vector3 offset;
        public bool isVisible;
        public bool forceHidden;
        public float lastDistance;
    }
}
