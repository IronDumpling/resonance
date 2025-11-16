using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Interfaces.Operations;
using Resonance.Interfaces.Services;

namespace Resonance.Player.Inventory.Operations
{
    /// <summary>
    /// Base class for all item operation handlers
    /// Provides common dependencies and utility methods
    /// </summary>
    public abstract class BaseItemOperationHandler
    {
        // Dependencies - injected via constructor
        protected PlayerInventory Inventory { get; private set; }
        protected WeaponManager WeaponManager { get; private set; }
        protected ConsumableManager ConsumableManager { get; private set; }
        
        // Services - fetched from ServiceRegistry
        protected IAudioService AudioService { get; private set; }
        
        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public BaseItemOperationHandler(
            PlayerInventory inventory,
            WeaponManager weaponManager,
            ConsumableManager consumableManager)
        {
            Inventory = inventory;
            WeaponManager = weaponManager;
            ConsumableManager = consumableManager;
            
            // Get services
            AudioService = ServiceRegistry.Get<IAudioService>();
        }
        
        /// <summary>
        /// Log debug message with handler name prefix
        /// </summary>
        protected void Log(string message)
        {
            Debug.Log($"{GetType().Name}: {message}");
        }
        
        /// <summary>
        /// Log warning message with handler name prefix
        /// </summary>
        protected void LogWarning(string message)
        {
            Debug.LogWarning($"{GetType().Name}: {message}");
        }
        
        /// <summary>
        /// Log error message with handler name prefix
        /// </summary>
        protected void LogError(string message)
        {
            Debug.LogError($"{GetType().Name}: {message}");
        }
    }
}