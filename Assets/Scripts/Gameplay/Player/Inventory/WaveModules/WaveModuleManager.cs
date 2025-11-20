using UnityEngine;
using System.Collections.Generic;
using Resonance.Systems.GridSystem;
using Resonance.Systems.Waves;
using Resonance.Systems.Waves.Editors;
using Resonance.Shared.Types;

namespace Resonance.Gameplay.Player.Inventory
{
    /// <summary>
    /// WaveModuleManager - Manages wave module items and graphs in inventory
    /// Responsibilities:
    /// - Track module items in inventory
    /// - Manage active module graph
    /// - Execute graph to generate waves
    /// - Provide modules to UI for editing
    /// </summary>
    public class WaveModuleManager
    {
        private PlayerInventory _inventory;
        
        // Active module graph (currently being used/edited)
        private WaveModuleGraph _activeGraph;
        private int _activeGraphItemID = -1;
        
        // Events
        public System.Action<WaveModuleGraph> OnGraphActivated;
        public System.Action OnGraphDeactivated;
        public System.Action<Wave> OnGraphExecuted;
        
        // Properties
        public bool HasActiveGraph => _activeGraph != null;
        public WaveModuleGraph ActiveGraph => _activeGraph;
        public int ActiveGraphItemID => _activeGraphItemID;
        
        public WaveModuleManager(PlayerInventory inventory)
        {
            _inventory = inventory;
            
            // Listen to inventory events
            _inventory.OnItemAddedToGrid += OnItemAdded;
            _inventory.OnItemRemovedFromGrid += OnItemRemoved;
            
            Debug.Log("WaveModuleManager: Initialized");
        }

        #region Module Management

        /// <summary>
        /// Get all module items in inventory
        /// </summary>
        public List<GridItem> GetAllModules()
        {
            return _inventory.GetAllItems();
        }
        
        /// <summary>
        /// Create a new module graph item
        /// </summary>
        public GridItem CreateModuleGraphItem(string graphName)
        {
            var graph = new WaveModuleGraph();
            graph.GraphData.graphName = graphName;
            graph.GraphData.graphID = System.Guid.NewGuid().ToString();
            
            GridItem moduleItem = new GridItem
            {
                ItemID = Random.Range(100000, 999999),
                ItemName = graphName,
                ItemType = ItemType.Module,
                GridWidth = 2,
                GridHeight = 2,
                Quantity = 1,
                MaxStackQuantity = 1
            };
            
            // Save graph to item
            WaveModuleGraphHelper.SaveGraphToGridItem(graph, moduleItem);
            
            return moduleItem;
        }
        
        /// <summary>
        /// Activate a module graph for use/editing
        /// </summary>
        public bool ActivateGraph(int moduleItemID)
        {
            var moduleItem = _inventory.GetItemByID(moduleItemID);
            if (moduleItem == null || moduleItem.ItemType != ItemType.Module)
            {
                Debug.LogWarning($"WaveModuleManager: Module item {moduleItemID} not found");
                return false;
            }
            
            // Load graph from item
            _activeGraph = WaveModuleGraphHelper.LoadGraphFromGridItem(moduleItem);
            if (_activeGraph == null)
            {
                Debug.LogError($"WaveModuleManager: Failed to load graph from item {moduleItem.ItemName}");
                return false;
            }
            
            _activeGraphItemID = moduleItemID;
            
            OnGraphActivated?.Invoke(_activeGraph);
            
            Debug.Log($"WaveModuleManager: Activated graph '{_activeGraph.GraphData.graphName}'");
            return true;
        }
        
        /// <summary>
        /// Deactivate current graph
        /// </summary>
        public void DeactivateGraph()
        {
            if (_activeGraph == null) return;
            
            // Save graph back to item before deactivating
            SaveActiveGraph();
            
            _activeGraph = null;
            _activeGraphItemID = -1;
            
            OnGraphDeactivated?.Invoke();
            
            Debug.Log("WaveModuleManager: Graph deactivated");
        }
        
        /// <summary>
        /// Save active graph back to inventory item
        /// </summary>
        public bool SaveActiveGraph()
        {
            if (_activeGraph == null || _activeGraphItemID == -1)
            {
                Debug.LogWarning("WaveModuleManager: No active graph to save");
                return false;
            }
            
            var moduleItem = _inventory.GetItemByID(_activeGraphItemID);
            if (moduleItem == null)
            {
                Debug.LogError($"WaveModuleManager: Module item {_activeGraphItemID} not found");
                return false;
            }
            
            bool success = WaveModuleGraphHelper.SaveGraphToGridItem(_activeGraph, moduleItem);
            if (success)
            {
                Debug.Log($"WaveModuleManager: Saved graph '{_activeGraph.GraphData.graphName}'");
            }
            
            return success;
        }
        
        #endregion
        
        #region Graph Execution
        
        /// <summary>
        /// Execute active graph and generate wave
        /// </summary>
        public Wave ExecuteGraph()
        {
            if (_activeGraph == null)
            {
                Debug.LogWarning("WaveModuleManager: No active graph to execute");
                return null;
            }
            
            Wave generatedWave = _activeGraph.Execute();
            
            if (generatedWave != null)
            {
                OnGraphExecuted?.Invoke(generatedWave);
                Debug.Log($"WaveModuleManager: Graph executed - Wave energy: {generatedWave.EnergyStrength}");
            }
            else
            {
                Debug.LogWarning("WaveModuleManager: Graph execution returned null");
            }
            
            return generatedWave;
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnItemAdded(GridItem item, Vector2Int position)
        {
            if (item.ItemType == ItemType.Module)
            {
                Debug.Log($"WaveModuleManager: Module '{item.ItemName}' added to inventory");
            }
        }
        
        private void OnItemRemoved(GridItem item, Vector2Int position)
        {
            if (item.ItemType == ItemType.Module)
            {
                // If this was the active graph, deactivate it
                if (item.ItemID == _activeGraphItemID)
                {
                    Debug.LogWarning($"WaveModuleManager: Active graph '{item.ItemName}' removed");
                    DeactivateGraph();
                }
            }
        }
        
        #endregion
        
        #region Save/Load
        
        public WaveModuleManagerSaveData GetSaveData()
        {
            // Save active graph before getting save data
            if (_activeGraph != null)
            {
                SaveActiveGraph();
            }
            
            return new WaveModuleManagerSaveData
            {
                activeGraphItemID = _activeGraphItemID
            };
        }
        
        public void LoadFromSaveData(WaveModuleManagerSaveData saveData)
        {
            if (saveData == null || saveData.activeGraphItemID == -1)
            {
                _activeGraph = null;
                _activeGraphItemID = -1;
                return;
            }
            
            // Try to activate the saved graph
            ActivateGraph(saveData.activeGraphItemID);
        }
        
        #endregion
        
        #region Cleanup
        
        public void Cleanup()
        {
            if (_inventory != null)
            {
                _inventory.OnItemAddedToGrid -= OnItemAdded;
                _inventory.OnItemRemovedFromGrid -= OnItemRemoved;
            }
            
            // Save before cleanup
            SaveActiveGraph();
            
            OnGraphActivated = null;
            OnGraphDeactivated = null;
            OnGraphExecuted = null;
        }
        
        #endregion
    }
    
    [System.Serializable]
    public class WaveModuleManagerSaveData
    {
        public int activeGraphItemID;
    }
}