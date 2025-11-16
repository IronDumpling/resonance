using Resonance.Systems.GridSystem;
using Resonance.Shared.Types;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Resonance.Systems.Waves.Editors
{
    /// <summary>
    /// Helper class for integrating WaveModuleGraph with GridSystem
    /// Manages saving/loading module graphs from GridItem.CustomData
    /// </summary>
    public static class WaveModuleGraphHelper
    {
        private const string GRAPH_DATA_KEY = "WaveModuleGraphData";
        
        /// <summary>
        /// Save a module graph to a GridItem's CustomData
        /// </summary>
        public static bool SaveGraphToGridItem(WaveModuleGraph graph, GridItem gridItem)
        {
            if (graph == null || gridItem == null)
            {
                Debug.LogError("WaveModuleGraphHelper: Cannot save graph - graph or gridItem is null");
                return false;
            }
            
            if (gridItem.ItemType != ItemType.Module)
            {
                Debug.LogWarning($"WaveModuleGraphHelper: GridItem {gridItem.ItemName} is not a Module type");
                return false;
            }
            
            // Save graph to data
            graph.SaveToData();
            
            // Serialize graph data to JSON
            string jsonData = JsonUtility.ToJson(graph.GraphData, true);
            
            // Store in CustomData
            if (gridItem.CustomData == null)
            {
                gridItem.CustomData = new Dictionary<string, object>();
            }
            
            gridItem.CustomData[GRAPH_DATA_KEY] = jsonData;
            
            Debug.Log($"WaveModuleGraphHelper: Saved graph to GridItem {gridItem.ItemName}");
            return true;
        }
        
        /// <summary>
        /// Load a module graph from a GridItem's CustomData
        /// Note: This creates a new graph but does not instantiate modules
        /// Modules must be instantiated by a factory based on ModuleTypeName
        /// </summary>
        public static WaveModuleGraph LoadGraphFromGridItem(GridItem gridItem)
        {
            if (gridItem == null)
            {
                Debug.LogError("WaveModuleGraphHelper: Cannot load graph - gridItem is null");
                return null;
            }
            
            if (gridItem.ItemType != ItemType.Module)
            {
                Debug.LogWarning($"WaveModuleGraphHelper: GridItem {gridItem.ItemName} is not a Module type");
                return null;
            }
            
            if (gridItem.CustomData == null || !gridItem.CustomData.ContainsKey(GRAPH_DATA_KEY))
            {
                Debug.LogWarning($"WaveModuleGraphHelper: No graph data found in GridItem {gridItem.ItemName}");
                return null;
            }
            
            // Get JSON data
            object jsonDataObj = gridItem.CustomData[GRAPH_DATA_KEY];
            if (jsonDataObj == null)
            {
                Debug.LogWarning($"WaveModuleGraphHelper: Graph data is null in GridItem {gridItem.ItemName}");
                return null;
            }
            
            string jsonData = jsonDataObj.ToString();
            
            // Deserialize graph data
            try
            {
                WaveModuleGraphData graphData = JsonUtility.FromJson<WaveModuleGraphData>(jsonData);
                
                if (graphData == null)
                {
                    Debug.LogError("WaveModuleGraphHelper: Failed to deserialize graph data");
                    return null;
                }
                
                // Validate graph data
                if (!graphData.Validate())
                {
                    Debug.LogError("WaveModuleGraphHelper: Graph data validation failed");
                    return null;
                }
                
                // Create graph from data
                // Note: Modules will need to be instantiated by a factory
                WaveModuleGraph graph = new WaveModuleGraph(graphData);
                
                Debug.Log($"WaveModuleGraphHelper: Loaded graph from GridItem {gridItem.ItemName}");
                return graph;
            }
            catch (Exception e)
            {
                Debug.LogError($"WaveModuleGraphHelper: Exception while deserializing graph data: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Check if a GridItem contains a module graph
        /// </summary>
        public static bool HasGraph(GridItem gridItem)
        {
            if (gridItem == null || gridItem.ItemType != ItemType.Module)
            {
                return false;
            }
            
            return gridItem.CustomData != null && gridItem.CustomData.ContainsKey(GRAPH_DATA_KEY);
        }
        
        /// <summary>
        /// Create a new empty graph and save it to a GridItem
        /// </summary>
        public static WaveModuleGraph CreateNewGraphForGridItem(GridItem gridItem, string graphName = "New Wave Graph")
        {
            if (gridItem == null)
            {
                Debug.LogError("WaveModuleGraphHelper: Cannot create graph - gridItem is null");
                return null;
            }
            
            if (gridItem.ItemType != ItemType.Module)
            {
                Debug.LogWarning($"WaveModuleGraphHelper: GridItem {gridItem.ItemName} is not a Module type");
                return null;
            }
            
            // Create new graph
            WaveModuleGraph graph = new WaveModuleGraph();
            graph.GraphData.GraphName = graphName;
            
            // Save to grid item
            if (SaveGraphToGridItem(graph, gridItem))
            {
                return graph;
            }
            
            return null;
        }
        
        /// <summary>
        /// Get or create a graph for a GridItem
        /// </summary>
        public static WaveModuleGraph GetOrCreateGraphForGridItem(GridItem gridItem, string graphName = "New Wave Graph")
        {
            if (HasGraph(gridItem))
            {
                return LoadGraphFromGridItem(gridItem);
            }
            else
            {
                return CreateNewGraphForGridItem(gridItem, graphName);
            }
        }
    }
}

