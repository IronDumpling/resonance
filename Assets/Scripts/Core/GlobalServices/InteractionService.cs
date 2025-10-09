using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Resonance.Interfaces.Services;
using Resonance.Interfaces.Objects;
using Resonance.Items;
using Resonance.Utilities;
using Resonance.Utilities.GridSystem;
using Resonance.Player.Core;
using Resonance.Core.StateMachine.States;

namespace Resonance.Core.GlobalServices
{
    /// <summary>
    /// Global service 
    /// for managing interactions between the player and interactable objects
    /// </summary>
    public class InteractionService : IInteractionService
    {
        private GameObject _currentInteractable;
        private string _currentInteractionText = "";
        private HashSet<GameObject> _registeredInteractables = new HashSet<GameObject>();
        
        // Track interactable objects in range
        private Dictionary<GameObject, IInteractable> _interactablesInRange = new Dictionary<GameObject, IInteractable>();
        
        // Dependencies
        private IPlayerService _playerService;
        private IUIService _uiService;
        
        // Internal strategies
        private PickupStrategy _pickupStrategy;
        private ReadableStrategy _readableStrategy;

        // IGameService Properties
        public int Priority => 30; // After PlayerService (20) since we need the player to be available
        public SystemState State { get; private set; } = SystemState.Uninitialized;

        // Properties
        public GameObject CurrentInteractable => _currentInteractable;
        public bool HasInteractable => _currentInteractable != null;
        public string InteractionText => _currentInteractionText;

        // Events
        public event System.Action<GameObject, string> OnInteractableChanged;
        public static event System.Action OnInventoryFullPickupAttempt;

        #region IGameService Implementation

        public void Initialize()
        {
            if (State != SystemState.Uninitialized)
            {
                Debug.LogWarning("InteractionService already initialized");
                return;
            }

            State = SystemState.Initializing;
            Debug.Log("InteractionService: Initializing");
            
            // Get dependencies
            _playerService = ServiceRegistry.Get<IPlayerService>();
            _uiService = ServiceRegistry.Get<IUIService>();
            
            // Initialize internal strategies
            _pickupStrategy = new PickupStrategy(_playerService);
            _readableStrategy = new ReadableStrategy(_uiService);

            State = SystemState.Running;
            Debug.Log("InteractionService: Initialized successfully with pickup and readable strategies");
        }

        public void Shutdown()
        {
            if (State == SystemState.Shutdown) return;

            Debug.Log("InteractionService: Shutting down");

            _registeredInteractables.Clear();
            _interactablesInRange.Clear();
            _currentInteractable = null;
            _currentInteractionText = "";
            
            OnInteractableChanged = null;

            State = SystemState.Shutdown;
        }

        #endregion

        #region IInteractionService Implementation

        public void RegisterInteractable(GameObject interactable)
        {
            if (interactable == null) return;
            
            if (_registeredInteractables.Add(interactable))
            {
                Debug.Log($"InteractionService: Registered interactable {interactable.name}");
            }
        }

        public void UnregisterInteractable(GameObject interactable)
        {
            if (interactable == null) return;
            
            if (_registeredInteractables.Remove(interactable))
            {
                // If the current interaction object is removed, clear it
                if (_currentInteractable == interactable)
                {
                    ClearCurrentInteractable();
                }
                
                Debug.Log($"InteractionService: Unregistered interactable {interactable.name}");
            }
        }

        public void SetCurrentInteractable(GameObject interactable, string interactionText = "")
        {
            if (_currentInteractable == interactable) return;
            
            _currentInteractable = interactable;
            _currentInteractionText = interactionText;
            
            OnInteractableChanged?.Invoke(_currentInteractable, _currentInteractionText);
            
            if (interactable != null)
            {
                Debug.Log($"InteractionService: Current interactable set to {interactable.name}: {interactionText}");
            }
        }

        public void ClearCurrentInteractable()
        {
            if (_currentInteractable == null) return;
            
            Debug.Log($"InteractionService: Cleared current interactable {_currentInteractable.name}");
            
            _currentInteractable = null;
            _currentInteractionText = "";
            
            OnInteractableChanged?.Invoke(null, "");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Get the nearest interactable object
        /// </summary>
        /// <returns>The nearest interactable object, or null if none</returns>
        public IInteractable GetNearestInteractable()
        {
            if (_interactablesInRange.Count == 0) return null;

            // Get player position
            var playerService = ServiceRegistry.Get<IPlayerService>();
            if (playerService?.CurrentPlayer == null) return null;

            Vector3 playerPosition = playerService.CurrentPlayer.transform.position;
            
            // Find the nearest interactable object
            IInteractable nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var kvp in _interactablesInRange)
            {
                if (kvp.Value != null && kvp.Value.CanInteract())
                {
                    float distance = Vector3.Distance(playerPosition, kvp.Value.GetPosition());
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = kvp.Value;
                    }
                }
            }

            return nearest;
        }

        /// <summary>
        /// Handle interactable object entering range
        /// </summary>
        /// <param name="gameObject">Game object</param>
        /// <param name="interactable">Interactable object</param>
        public void OnInteractableEnteredRange(GameObject gameObject, IInteractable interactable)
        {
            if (gameObject == null || interactable == null) return;

            // Add to range objects list
            _interactablesInRange[gameObject] = interactable;
        }

        /// <summary>
        /// Handle interactable object leaving range
        /// </summary>
        /// <param name="gameObject">Game object</param>
        /// <param name="interactable">Interactable object</param>
        public void OnInteractableExitedRange(GameObject gameObject, IInteractable interactable)
        {
            if (gameObject == null) return;

            // Remove from range objects list
            _interactablesInRange.Remove(gameObject);
        }

        #endregion

        #region Debug Methods

        /// <summary>
        /// Get the number of registered interactable objects (for debugging)
        /// </summary>
        /// <returns>Number of interactable objects</returns>
        public int GetRegisteredCount()
        {
            return _registeredInteractables.Count;
        }

        /// <summary>
        /// Get all registered interactable objects (for debugging)
        /// </summary>
        /// <returns>Interactable objects list</returns>
        public GameObject[] GetRegisteredInteractables()
        {
            GameObject[] result = new GameObject[_registeredInteractables.Count];
            _registeredInteractables.CopyTo(result);
            return result;
        }

        #endregion
        
        #region Interaction Flow Management
        
        /// <summary>
        /// Complete interaction with the specified interactable object
        /// This is the main entry point for executing interactions
        /// </summary>
        /// <param name="interactable">The interactable object to interact with</param>
        public void CompleteInteraction(IInteractable interactable)
        {
            if (interactable == null)
            {
                Debug.LogWarning("InteractionService: Cannot complete interaction with null interactable");
                return;
            }
            
            Debug.Log($"InteractionService: Completing interaction with {interactable.GetInteractableName()}");
            
            // Dispatch to the corresponding strategy based on type
            if (interactable is IPickupable pickupable)
            {
                _pickupStrategy.Execute(pickupable);
            }
            else if (interactable is IReadable readable)
            {
                _readableStrategy.Execute(readable);
            }
            else
            {
                Debug.LogWarning($"InteractionService: Unknown interactable type: {interactable.GetType().Name}");
            }
        }
        
        #endregion
        
        #region Internal Strategies
        
        /// <summary>
        /// Pickup strategy - handle pickup logic for IPickupable items
        /// </summary>
        private class PickupStrategy
        {
            private IPlayerService _playerService;
            
            public PickupStrategy(IPlayerService playerService)
            {
                _playerService = playerService;
            }
            
            public void Execute(IPickupable pickupable)
            {
                var playerController = _playerService.CurrentPlayer?.Controller;
                if (playerController == null)
                {
                    Debug.LogError("PickupStrategy: PlayerController is null");
                    return;
                }
                
                // Try to pickup
                bool canAdd = pickupable.TryAddToInventory(out GridItem gridItem, out string failureReason);
                
                if (canAdd && gridItem != null)
                {
                    bool added = false;
                    
                    // Special handling for Consumable (Ammo) - use ConsumableManager for stacking
                    if (gridItem.ItemType == Utilities.ItemType.Consumable)
                    {
                        var consumableManager = playerController.ConsumableManager;
                        if (consumableManager != null)
                        {
                            added = consumableManager.AddAmmo(gridItem.CustomData["ammoType"].ToString(), gridItem);
                            Debug.Log($"PickupStrategy: Added ammo via ConsumableManager - {gridItem.ItemName} x{gridItem.Quantity}");
                        }
                    }
                    else
                    {
                        // For other items (Weapons, Tools, etc.) - add directly to grid
                        var inventory = playerController.Inventory;
                        Vector2Int emptyPos = inventory.FindEmptySpace(gridItem.GridWidth, gridItem.GridHeight);
                        
                        if (emptyPos.x >= 0 && emptyPos.y >= 0)
                        {
                            added = inventory.AddItemToGrid(gridItem, emptyPos);
                            
                            if (added)
                            {
                                // Auto-equip weapon
                                AutoEquipIfWeapon(gridItem, playerController);
                            }
                        }
                    }
                    
                    if (added)
                    {
                        pickupable.DestroyPickupItem();
                        Debug.Log($"PickupStrategy: Successfully picked up {gridItem.ItemName}");
                        
                        // Show pickup information (for all item types)
                        var itemDataAsset = gridItem.CustomData.ContainsKey("originalAsset") 
                            ? gridItem.CustomData["originalAsset"] as IInfoable 
                            : null;
                        if (itemDataAsset != null)
                        {
                            InfoDisplayService.ShowInfo(itemDataAsset);
                        }
                        return;
                    }
                }
                
                // Failed: Inventory full
                Debug.LogWarning($"PickupStrategy: Pickup failed - {failureReason}");
                pickupable.OnInventoryFull();
                OnInventoryFullPickupAttempt?.Invoke();
            }
            
            private void AutoEquipIfWeapon(GridItem item, PlayerController controller)
            {
                if (item.ItemType == Utilities.ItemType.Weapon)
                {
                    controller.WeaponManager?.EquipWeapon(item.ItemID);
                    Debug.Log($"PickupStrategy: Auto-equipped weapon {item.ItemName}");
                }
            }
        }
        
        /// <summary>
        /// Read strategy - handle information display logic for IReadable items
        /// </summary>
        private class ReadableStrategy
        {
            private IUIService _uiService;
            
            public ReadableStrategy(IUIService uiService)
            {
                _uiService = uiService;
            }
            
            public void Execute(IReadable readable)
            {
                var infoable = readable.GetInfoable();
                if (infoable == null)
                {
                    Debug.LogError("ReadableStrategy: GetInfoable() returned null");
                    return;
                }
                
                Debug.Log($"ReadableStrategy: Displaying info for {infoable.GetInfoData().name}");
                
                // Use the unified InfoDisplayService to display information
                InfoDisplayService.ShowInfo(infoable);
            }
        }
        
        #endregion
    }
}
