using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Resonance.Gameplay.Items;
using Resonance.Utilities;
using Resonance.Shared.Types;
using Resonance.Systems.GridSystem;
using Resonance.Gameplay.Player.Core;

namespace Resonance.Gameplay.Player.Inventory
{
    /// <summary>
    /// ConsumableManager - Manage consumable items (EnergyBottle, Healant, etc.)
    /// Responsibilities: manage consumable usage, stack same type consumables
    /// </summary>
    public class ConsumableManager
    {
        private PlayerInventory _inventory;
        private PlayerController _playerController;
        
        // Events
        public System.Action<ConsumableType> OnConsumableUsed; // consumableType
        public System.Action<ConsumableType, int> OnConsumableCountChanged; // consumableType, newCount
        
        public ConsumableManager(PlayerInventory inventory)
        {
            _inventory = inventory;
            Debug.Log("ConsumableManager: Initialized");
        }
        
        /// <summary>
        /// Set PlayerController reference (needed for restoration effects)
        /// </summary>
        public void SetPlayerController(PlayerController playerController)
        {
            _playerController = playerController;
        }
        
        #region Consumable Usage
        
        /// <summary>
        /// Use Energy Bottle - Restores Crystal Core Energy
        /// Automatically finds and uses the first available EnergyBottle
        /// </summary>
        public bool UseEnergyBottle()
        {
            if (_playerController == null)
            {
                Debug.LogWarning("ConsumableManager: PlayerController not set");
                return false;
            }
            
            // Find first energy bottle in inventory
            var energyBottle = GetConsumableByType(ConsumableType.EnergyBottle);
            if (energyBottle == null)
            {
                Debug.LogWarning("ConsumableManager: No Energy Bottle in inventory");
                return false;
            }
            
            return UseEnergyBottle(energyBottle);
        }
        
        /// <summary>
        /// Use specific Energy Bottle - Restores Crystal Core Energy
        /// Consumes one full bottle and restores energy by configured amount
        /// Overflow energy is ignored (capped at MaxEnergy)
        /// </summary>
        public bool UseEnergyBottle(GridItem energyBottle)
        {
            if (_playerController == null)
            {
                Debug.LogWarning("ConsumableManager: PlayerController not set");
                return false;
            }
            
            if (energyBottle == null || energyBottle.ConsumableType != ConsumableType.EnergyBottle)
            {
                Debug.LogWarning("ConsumableManager: Invalid EnergyBottle item");
                return false;
            }
            
            // Load the data asset to get restoration amount
            var dataAsset = LoadEnergyBottleAsset(energyBottle.AssetPath);
            if (dataAsset == null)
            {
                Debug.LogError($"ConsumableManager: Failed to load EnergyBottleDataAsset from {energyBottle.AssetPath}");
                return false;
            }
            
            var crystalCore = _playerController.Stats.crystalCore;
            float energyBefore = crystalCore.CurrentEnergy;
            
            // Restore full configured amount (RestoreEnergy will handle capping at MaxEnergy)
            crystalCore.AddEnergy(dataAsset.energyRestoreAmount);
            
            float energyAfter = crystalCore.CurrentEnergy;
            float actualRestored = energyAfter - energyBefore;
            
            // Consume one unit
            ConsumeOne(energyBottle.ItemID);
            
            OnConsumableUsed?.Invoke(ConsumableType.EnergyBottle);
            int remaining = GetConsumableCount(ConsumableType.EnergyBottle);
            OnConsumableCountChanged?.Invoke(ConsumableType.EnergyBottle, remaining);
            
            Debug.Log($"ConsumableManager: Used Energy Bottle. Configured: {dataAsset.energyRestoreAmount:F1}, Actual: {actualRestored:F1} energy. Energy: {energyAfter:F1}/{crystalCore.MaxEnergy:F1}, Remaining bottles: {remaining}");
            return true;
        }
        
        /// <summary>
        /// Use Healant - Restores Crystal Core Health
        /// Automatically finds and uses the first available Healant
        /// </summary>
        public bool UseHealant()
        {
            if (_playerController == null)
            {
                Debug.LogWarning("ConsumableManager: PlayerController not set");
                return false;
            }
            
            // Find first healant in inventory
            var healant = GetConsumableByType(ConsumableType.Healant);
            if (healant == null)
            {
                Debug.LogWarning("ConsumableManager: No Healant in inventory");
                return false;
            }
            
            return UseHealant(healant);
        }
        
        /// <summary>
        /// Use specific Healant - Restores Crystal Core Health
        /// Consumes one full healant and restores core health by configured amount
        /// Overflow health is ignored (capped at MaxCoreHealth)
        /// </summary>
        public bool UseHealant(GridItem healant)
        {
            if (_playerController == null)
            {
                Debug.LogWarning("ConsumableManager: PlayerController not set");
                return false;
            }
            
            if (healant == null || healant.ConsumableType != ConsumableType.Healant)
            {
                Debug.LogWarning("ConsumableManager: Invalid Healant item");
                return false;
            }
            
            // Load the data asset to get restoration amount
            var dataAsset = LoadHealantAsset(healant.AssetPath);
            if (dataAsset == null)
            {
                Debug.LogError($"ConsumableManager: Failed to load HealantDataAsset from {healant.AssetPath}");
                return false;
            }
            
            var crystalCore = _playerController.Stats.crystalCore;
            float healthBefore = crystalCore.CurrentCoreHealth;
            
            // Restore full configured amount (RestoreCoreHealth will handle capping at MaxCoreHealth)
            crystalCore.RestoreCoreHealth(dataAsset.coreHealthRestoreAmount);
            
            float healthAfter = crystalCore.CurrentCoreHealth;
            float actualRestored = healthAfter - healthBefore;
            
            // Consume one unit
            ConsumeOne(healant.ItemID);
            
            OnConsumableUsed?.Invoke(ConsumableType.Healant);
            int remaining = GetConsumableCount(ConsumableType.Healant);
            OnConsumableCountChanged?.Invoke(ConsumableType.Healant, remaining);
            
            Debug.Log($"ConsumableManager: Used Healant. Configured: {dataAsset.coreHealthRestoreAmount:F1}, Actual: {actualRestored:F1} core health. Health: {healthAfter:F1}/{crystalCore.MaxCoreHealth:F1}, Remaining healants: {remaining}");
            return true;
        }
        
        #endregion
        
        #region Query Methods
        
        /// <summary>
        /// Get first consumable of specified type
        /// </summary>
        private GridItem GetConsumableByType(ConsumableType consumableType)
        {
            return _inventory.GetItemsByType(ItemType.Consumable)
                .FirstOrDefault(item => item.ConsumableType == consumableType);
        }
        
        /// <summary>
        /// Get total count of specific consumable type
        /// </summary>
        public int GetConsumableCount(ConsumableType consumableType)
        {
            return _inventory.GetItemsByType(ItemType.Consumable)
                .Where(item => item.ConsumableType == consumableType)
                .Sum(item => item.Quantity);
        }
        
        /// <summary>
        /// Check if player has specific consumable
        /// </summary>
        public bool HasConsumable(ConsumableType consumableType, int amount = 1)
        {
            return GetConsumableCount(consumableType) >= amount;
        }
        
        #endregion
        
        #region Stacking Operations
        
        /// <summary>
        /// Try to stack two items
        /// </summary>
        public bool TryStackItems(int sourceItemID, int targetItemID)
        {
            var sourceItem = _inventory.GetItemByID(sourceItemID);
            var targetItem = _inventory.GetItemByID(targetItemID);
            
            if (sourceItem == null || targetItem == null)
            {
                Debug.LogWarning("ConsumableManager: Source or target item not found");
                return false;
            }
            
            // Check if they can be stacked
            if (!CanStackItems(sourceItem, targetItem))
            {
                Debug.LogWarning($"ConsumableManager: Cannot stack {sourceItem.ItemName} with {targetItem.ItemName}");
                return false;
            }
            
            // Calculate the total quantity after stacking
            int totalQuantity = sourceItem.Quantity + targetItem.Quantity;
            int targetMaxStack = targetItem.MaxStackQuantity;
            
            if (totalQuantity <= targetMaxStack)
            {
                // Fully stack to target
                _inventory.UpdateItemQuantity(targetItemID, totalQuantity);
                _inventory.RemoveItemFromGrid(sourceItemID);
                
                Debug.Log($"ConsumableManager: Fully stacked {sourceItem.ItemName}. New quantity: {totalQuantity}");
                return true;
            }
            else
            {
                // Partially stack
                int amountToTransfer = targetMaxStack - targetItem.Quantity;
                _inventory.UpdateItemQuantity(targetItemID, targetMaxStack);
                _inventory.UpdateItemQuantity(sourceItemID, sourceItem.Quantity - amountToTransfer);
                
                Debug.Log($"ConsumableManager: Partially stacked {amountToTransfer}. Target: {targetMaxStack}, Source: {sourceItem.Quantity - amountToTransfer}");
                return true;
            }
        }
        
        /// <summary>
        /// Check if two items can be stacked
        /// </summary>
        public bool CanStackItems(GridItem sourceItem, GridItem targetItem)
        {
            if (sourceItem == null || targetItem == null)
                return false;
            
            // Must be the same type
            if (sourceItem.ItemType != targetItem.ItemType)
                return false;
            
            // Must be a consumable
            if (sourceItem.ItemType != ItemType.Consumable)
                return false;
            
            // Must be the same consumable type
            return sourceItem.ConsumableType == targetItem.ConsumableType;
        }
        
        #endregion
        
        #region Internal Helper Methods
        
        /// <summary>
        /// Consume one unit of a consumable
        /// </summary>
        private void ConsumeOne(int itemID)
        {
            var item = _inventory.GetItemByID(itemID);
            if (item == null) return;
            
            int newQuantity = item.Quantity - 1;
            _inventory.UpdateItemQuantity(itemID, newQuantity);
            
            Debug.Log($"ConsumableManager: Consumed one {item.ItemName}. Remaining: {newQuantity}");
        }
        
        /// <summary>
        /// Load EnergyBottleDataAsset from asset path
        /// </summary>
        private EnergyBottleDataAsset LoadEnergyBottleAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;
                
            #if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<EnergyBottleDataAsset>(assetPath);
            #else
            return Resources.Load<EnergyBottleDataAsset>(assetPath);
            #endif
        }
        
        /// <summary>
        /// Load HealantDataAsset from asset path
        /// </summary>
        private HealantDataAsset LoadHealantAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;
                
            #if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<HealantDataAsset>(assetPath);
            #else
            return Resources.Load<HealantDataAsset>(assetPath);
            #endif
        }
        
        #endregion
        
        #region Cleanup
        
        public void Cleanup()
        {
            OnConsumableUsed = null;
            OnConsumableCountChanged = null;
            _playerController = null;
        }
        
        #endregion
    }
}
