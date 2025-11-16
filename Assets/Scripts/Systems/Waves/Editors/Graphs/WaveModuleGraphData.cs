using System;
using System.Collections.Generic;
using UnityEngine;

namespace Resonance.Systems.Waves.Editors
{
    /// <summary>
    /// Serializable data structure for saving and loading wave module graphs
    /// Stored in GridItem.CustomData for ItemType.Module items
    /// </summary>
    [Serializable]
    public class WaveModuleGraphData
    {
        /// <summary>
        /// Unique identifier for this graph
        /// </summary>
        public string graphID;
        
        /// <summary>
        /// Display name of the graph
        /// </summary>
        public string graphName;
        
        /// <summary>
        /// List of module data (serialized modules)
        /// </summary>
        public List<WaveModuleData> modules;
        
        /// <summary>
        /// List of patch cable connections
        /// </summary>
        public List<WavePatchCable> cables;
        
        /// <summary>
        /// Output module ID (the module that generates the final wave)
        /// </summary>
        public string outputModuleID;
        
        /// <summary>
        /// Output port ID (the port that outputs the final wave)
        /// </summary>
        public string outputPortID;
        
        /// <summary>
        /// Timestamp of last modification
        /// </summary>
        public long lastModifiedTimestamp;
        
        // Properties for backward compatibility
        public string GraphID { get => graphID; set => graphID = value; }
        public string GraphName { get => graphName; set => graphName = value; }
        public List<WaveModuleData> Modules { get => modules; set => modules = value; }
        public List<WavePatchCable> Cables { get => cables; set => cables = value; }
        public string OutputModuleID { get => outputModuleID; set => outputModuleID = value; }
        public string OutputPortID { get => outputPortID; set => outputPortID = value; }
        public long LastModifiedTimestamp { get => lastModifiedTimestamp; set => lastModifiedTimestamp = value; }
        
        public WaveModuleGraphData()
        {
            graphID = Guid.NewGuid().ToString();
            graphName = "New Wave Graph";
            modules = new List<WaveModuleData>();
            cables = new List<WavePatchCable>();
            outputModuleID = string.Empty;
            outputPortID = string.Empty;
            lastModifiedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        
        /// <summary>
        /// Validate graph data integrity
        /// </summary>
        public bool Validate()
        {
            if (modules == null || modules.Count == 0)
            {
                Debug.LogWarning("WaveModuleGraphData: No modules in graph");
                return false;
            }
            
            // Check if output module exists
            if (!string.IsNullOrEmpty(outputModuleID))
            {
                bool outputModuleExists = modules.Exists(m => m.moduleID == outputModuleID);
                if (!outputModuleExists)
                {
                    Debug.LogWarning($"WaveModuleGraphData: Output module {outputModuleID} not found");
                    return false;
                }
            }
            
            // Validate cables reference existing modules
            if (cables != null)
            {
                foreach (var cable in cables)
                {
                    bool sourceExists = modules.Exists(m => m.moduleID == cable.sourceModuleID);
                    bool targetExists = modules.Exists(m => m.moduleID == cable.targetModuleID);
                    
                    if (!sourceExists || !targetExists)
                    {
                        Debug.LogWarning($"WaveModuleGraphData: Cable references non-existent module");
                        return false;
                    }
                }
            }
            
            return true;
        }
    }
    
    /// <summary>
    /// Serializable data for a single module
    /// </summary>
    [Serializable]
    public class WaveModuleData
    {
        /// <summary>
        /// Module identifier
        /// </summary>
        public string moduleID;
        
        /// <summary>
        /// Module type name (e.g., "SineOscillator", "VCF", etc.)
        /// </summary>
        public string moduleTypeName;
        
        /// <summary>
        /// Module type (Source, Processor, Output)
        /// </summary>
        public WaveModuleType moduleType;
        
        /// <summary>
        /// Module parameters as JSON string (serialized Dictionary)
        /// </summary>
        public string parametersJson;
        
        /// <summary>
        /// Input ports configuration
        /// </summary>
        public List<WaveModulePort> inputPorts;
        
        /// <summary>
        /// Output ports configuration
        /// </summary>
        public List<WaveModulePort> outputPorts;
        
        /// <summary>
        /// Position in editor (for UI)
        /// </summary>
        public Vector2 editorPosition;
        
        // Properties for backward compatibility
        public string ModuleID { get => moduleID; set => moduleID = value; }
        public string ModuleTypeName { get => moduleTypeName; set => moduleTypeName = value; }
        public WaveModuleType ModuleType { get => moduleType; set => moduleType = value; }
        public List<WaveModulePort> InputPorts { get => inputPorts; set => inputPorts = value; }
        public List<WaveModulePort> OutputPorts { get => outputPorts; set => outputPorts = value; }
        public Vector2 EditorPosition { get => editorPosition; set => editorPosition = value; }
        
        public WaveModuleData()
        {
            moduleID = Guid.NewGuid().ToString();
            moduleTypeName = string.Empty;
            moduleType = WaveModuleType.Source;
            parametersJson = "{}";
            inputPorts = new List<WaveModulePort>();
            outputPorts = new List<WaveModulePort>();
        }
        
        /// <summary>
        /// Get parameters as dictionary
        /// </summary>
        public Dictionary<string, object> GetParameters()
        {
            if (string.IsNullOrEmpty(parametersJson))
            {
                return new Dictionary<string, object>();
            }
            
            try
            {
                // Simple JSON parsing for key-value pairs
                // Format: {"key1":"value1","key2":"value2"}
                Dictionary<string, object> result = new Dictionary<string, object>();
                
                // Remove braces and split by comma
                string cleaned = parametersJson.Trim().TrimStart('{').TrimEnd('}');
                if (string.IsNullOrEmpty(cleaned))
                {
                    return result;
                }
                
                string[] pairs = cleaned.Split(',');
                foreach (string pair in pairs)
                {
                    string[] kv = pair.Split(':');
                    if (kv.Length == 2)
                    {
                        string key = kv[0].Trim().Trim('"');
                        string value = kv[1].Trim().Trim('"');
                        
                        // Try to parse as float, otherwise keep as string
                        if (float.TryParse(value, out float floatValue))
                        {
                            result[key] = floatValue;
                        }
                        else
                        {
                            result[key] = value;
                        }
                    }
                }
                
                return result;
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }
        
        /// <summary>
        /// Set parameters from dictionary
        /// </summary>
        public void SetParameters(Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                parametersJson = "{}";
                return;
            }
            
            // Simple JSON serialization
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (var kvp in parameters)
            {
                if (!first) sb.Append(",");
                sb.Append($"\"{kvp.Key}\":\"{kvp.Value}\"");
                first = false;
            }
            sb.Append("}");
            parametersJson = sb.ToString();
        }
    }
}

