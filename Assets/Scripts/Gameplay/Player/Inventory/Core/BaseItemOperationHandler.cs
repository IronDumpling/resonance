using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Shared.Interfaces.Operations;
using Resonance.Shared.Interfaces.Services;

namespace Resonance.Gameplay.Player.Inventory.Operations
{
    /// <summary>
    /// Base class for all item operation handlers
    /// Provides common dependencies and utility methods
    /// </summary>
    public abstract class BaseItemOperationHandler
    {
        // Dependencies - injected via constructor
        protected PlayerInventory Inventory { get; private set; }
        protected WaveOutputManager WaveOutputManager { get; private set; }
        protected ConsumableManager ConsumableManager { get; private set; }
        
        // Services - fetched from ServiceRegistry
        protected IAudioService AudioService { get; private set; }
        
        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public BaseItemOperationHandler(
            PlayerInventory inventory,
            WaveOutputManager waveOutputManager,
            ConsumableManager consumableManager)
        {
            Inventory = inventory;
            WaveOutputManager = waveOutputManager;
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