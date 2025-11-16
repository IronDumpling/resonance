using Resonance.Systems.Waves;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Resonance.Systems.Waves.Editors
{
    /// <summary>
    /// Core graph structure and execution engine for the modular wave editor
    /// Manages modules, connections, and executes the graph to generate final waves
    /// </summary>
    public class WaveModuleGraph
    {
        #region Private Fields
        
        private Dictionary<string, IWaveModule> _modules;
        private List<WavePatchCable> _cables;
        private string _outputModuleID;
        private string _outputPortID;
        private bool _isDirty;
        private List<string> _topologicalOrder;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Graph data (for serialization)
        /// </summary>
        public WaveModuleGraphData GraphData { get; private set; }
        
        /// <summary>
        /// Whether the graph structure has changed and needs recalculation
        /// </summary>
        public bool IsDirty => _isDirty;
        
        /// <summary>
        /// Number of modules in the graph
        /// </summary>
        public int ModuleCount => _modules.Count;
        
        /// <summary>
        /// Number of connections in the graph
        /// </summary>
        public int ConnectionCount => _cables.Count;
        
        #endregion
        
        #region Constructor
        
        public WaveModuleGraph()
        {
            _modules = new Dictionary<string, IWaveModule>();
            _cables = new List<WavePatchCable>();
            _outputModuleID = string.Empty;
            _outputPortID = string.Empty;
            _isDirty = true;
            _topologicalOrder = new List<string>();
            GraphData = new WaveModuleGraphData();
        }
        
        public WaveModuleGraph(WaveModuleGraphData graphData)
        {
            _modules = new Dictionary<string, IWaveModule>();
            _cables = new List<WavePatchCable>();
            _isDirty = true;
            _topologicalOrder = new List<string>();
            GraphData = graphData ?? new WaveModuleGraphData();
            
            // Load modules and cables from data
            LoadFromData(GraphData);
        }
        
        #endregion
        
        #region Module Management
        
        /// <summary>
        /// Add a module to the graph
        /// </summary>
        public bool AddModule(IWaveModule module)
        {
            if (module == null)
            {
                Debug.LogError("WaveModuleGraph: Cannot add null module");
                return false;
            }
            
            if (_modules.ContainsKey(module.ModuleID))
            {
                Debug.LogWarning($"WaveModuleGraph: Module {module.ModuleID} already exists");
                return false;
            }
            
            if (!module.Validate())
            {
                Debug.LogError($"WaveModuleGraph: Module {module.ModuleID} validation failed");
                return false;
            }
            
            _modules[module.ModuleID] = module;
            _isDirty = true;
            
            return true;
        }
        
        /// <summary>
        /// Remove a module from the graph
        /// </summary>
        public bool RemoveModule(string moduleID)
        {
            if (!_modules.ContainsKey(moduleID))
            {
                return false;
            }
            
            // Remove all cables connected to this module
            _cables.RemoveAll(c => c.IsConnectedToModule(moduleID));
            
            // Remove module
            _modules.Remove(moduleID);
            
            // Clear output if it was this module
            if (_outputModuleID == moduleID)
            {
                _outputModuleID = string.Empty;
                _outputPortID = string.Empty;
            }
            
            _isDirty = true;
            
            return true;
        }
        
        /// <summary>
        /// Get a module by ID
        /// </summary>
        public IWaveModule GetModule(string moduleID)
        {
            return _modules.TryGetValue(moduleID, out IWaveModule module) ? module : null;
        }
        
        /// <summary>
        /// Get all modules
        /// </summary>
        public IReadOnlyDictionary<string, IWaveModule> GetAllModules()
        {
            return _modules;
        }
        
        #endregion
        
        #region Connection Management
        
        /// <summary>
        /// Connect two modules with a patch cable
        /// </summary>
        public bool ConnectModules(string sourceModuleID, string sourcePortID, string targetModuleID, string targetPortID)
        {
            // Validate modules exist
            if (!_modules.TryGetValue(sourceModuleID, out IWaveModule sourceModule))
            {
                Debug.LogError($"WaveModuleGraph: Source module {sourceModuleID} not found");
                return false;
            }
            
            if (!_modules.TryGetValue(targetModuleID, out IWaveModule targetModule))
            {
                Debug.LogError($"WaveModuleGraph: Target module {targetModuleID} not found");
                return false;
            }
            
            // Validate ports exist
            var sourcePort = sourceModule.OutputPorts.FirstOrDefault(p => p.PortID == sourcePortID);
            if (sourcePort == null)
            {
                Debug.LogError($"WaveModuleGraph: Source port {sourcePortID} not found on module {sourceModuleID}");
                return false;
            }
            
            var targetPort = targetModule.InputPorts.FirstOrDefault(p => p.PortID == targetPortID);
            if (targetPort == null)
            {
                Debug.LogError($"WaveModuleGraph: Target port {targetPortID} not found on module {targetModuleID}");
                return false;
            }
            
            // Check if connection already exists
            bool connectionExists = _cables.Any(c => c.Connects(sourceModuleID, sourcePortID, targetModuleID, targetPortID));
            if (connectionExists)
            {
                Debug.LogWarning($"WaveModuleGraph: Connection already exists");
                return false;
            }
            
            // Check if ports can accept connection
            if (!sourcePort.CanConnect())
            {
                Debug.LogWarning($"WaveModuleGraph: Source port {sourcePortID} cannot accept more connections");
                return false;
            }
            
            if (!targetPort.CanConnect())
            {
                Debug.LogWarning($"WaveModuleGraph: Target port {targetPortID} cannot accept more connections");
                return false;
            }
            
            // Create and add cable
            var cable = new WavePatchCable(sourceModuleID, sourcePortID, targetModuleID, targetPortID);
            
            // Set stack index for output ports with multiple connections
            int stackIndex = sourcePort.ConnectionCount;
            cable.StackIndex = stackIndex;
            
            _cables.Add(cable);
            
            // Update port connection counts
            sourcePort.AddConnection();
            targetPort.AddConnection();
            
            _isDirty = true;
            
            return true;
        }
        
        /// <summary>
        /// Disconnect modules
        /// </summary>
        public bool DisconnectModules(string sourceModuleID, string sourcePortID, string targetModuleID, string targetPortID)
        {
            var cable = _cables.FirstOrDefault(c => c.Connects(sourceModuleID, sourcePortID, targetModuleID, targetPortID));
            if (cable == null)
            {
                return false;
            }
            
            // Update port connection counts
            if (_modules.TryGetValue(sourceModuleID, out IWaveModule sourceModule))
            {
                var sourcePort = sourceModule.OutputPorts.FirstOrDefault(p => p.PortID == sourcePortID);
                sourcePort?.RemoveConnection();
            }
            
            if (_modules.TryGetValue(targetModuleID, out IWaveModule targetModule))
            {
                var targetPort = targetModule.InputPorts.FirstOrDefault(p => p.PortID == targetPortID);
                targetPort?.RemoveConnection();
            }
            
            _cables.Remove(cable);
            _isDirty = true;
            
            return true;
        }
        
        /// <summary>
        /// Set the output module and port (where the final wave is generated)
        /// </summary>
        public bool SetOutput(string moduleID, string portID)
        {
            if (!_modules.TryGetValue(moduleID, out IWaveModule module))
            {
                Debug.LogError($"WaveModuleGraph: Module {moduleID} not found");
                return false;
            }
            
            var port = module.OutputPorts.FirstOrDefault(p => p.PortID == portID);
            if (port == null)
            {
                Debug.LogError($"WaveModuleGraph: Port {portID} not found on module {moduleID}");
                return false;
            }
            
            _outputModuleID = moduleID;
            _outputPortID = portID;
            _isDirty = true;
            
            return true;
        }
        
        #endregion
        
        #region Graph Execution
        
        /// <summary>
        /// Execute the graph and generate the final output wave
        /// Uses topological sorting to process modules in correct order
        /// </summary>
        public Wave Execute()
        {
            if (_modules.Count == 0)
            {
                Debug.LogWarning("WaveModuleGraph: No modules in graph");
                return null;
            }
            
            if (string.IsNullOrEmpty(_outputModuleID))
            {
                Debug.LogWarning("WaveModuleGraph: No output module set");
                return null;
            }
            
            // Recalculate topological order if graph is dirty
            if (_isDirty)
            {
                if (!CalculateTopologicalOrder())
                {
                    Debug.LogError("WaveModuleGraph: Failed to calculate topological order (circular dependency?)");
                    return null;
                }
                _isDirty = false;
            }
            
            // Execute modules in topological order
            Dictionary<string, Dictionary<string, Wave>> moduleOutputs = new Dictionary<string, Dictionary<string, Wave>>();
            
            foreach (string moduleID in _topologicalOrder)
            {
                if (!_modules.TryGetValue(moduleID, out IWaveModule module))
                {
                    continue;
                }
                
                // Collect input waves for this module
                Dictionary<string, Wave> inputWaves = new Dictionary<string, Wave>();
                
                foreach (var inputPort in module.InputPorts)
                {
                    // Find cable connected to this input port
                    var cable = _cables.FirstOrDefault(c => c.TargetModuleID == moduleID && c.TargetPortID == inputPort.PortID);
                    if (cable != null)
                    {
                        // Get wave from source module output
                        if (moduleOutputs.TryGetValue(cable.SourceModuleID, out Dictionary<string, Wave> sourceOutputs))
                        {
                            if (sourceOutputs.TryGetValue(cable.SourcePortID, out Wave inputWave))
                            {
                                inputWaves[inputPort.PortID] = inputWave;
                            }
                        }
                    }
                }
                
                // Process module
                Dictionary<string, Wave> outputs = module.Process(inputWaves);
                moduleOutputs[moduleID] = outputs;
            }
            
            // Get final output wave
            if (moduleOutputs.TryGetValue(_outputModuleID, out Dictionary<string, Wave> finalOutputs))
            {
                if (finalOutputs.TryGetValue(_outputPortID, out Wave finalWave))
                {
                    return finalWave;
                }
            }
            
            Debug.LogWarning("WaveModuleGraph: Failed to generate output wave");
            return null;
        }
        
        /// <summary>
        /// Calculate topological order using Kahn's algorithm
        /// Returns true if successful, false if circular dependency detected
        /// </summary>
        private bool CalculateTopologicalOrder()
        {
            _topologicalOrder.Clear();
            
            // Build adjacency list and in-degree count
            Dictionary<string, List<string>> adjacencyList = new Dictionary<string, List<string>>();
            Dictionary<string, int> inDegree = new Dictionary<string, int>();
            
            // Initialize
            foreach (string moduleID in _modules.Keys)
            {
                adjacencyList[moduleID] = new List<string>();
                inDegree[moduleID] = 0;
            }
            
            // Build graph from cables
            foreach (var cable in _cables)
            {
                if (!adjacencyList.ContainsKey(cable.SourceModuleID) || !adjacencyList.ContainsKey(cable.TargetModuleID))
                {
                    continue;
                }
                
                adjacencyList[cable.SourceModuleID].Add(cable.TargetModuleID);
                inDegree[cable.TargetModuleID]++;
            }
            
            // Kahn's algorithm
            Queue<string> queue = new Queue<string>();
            
            // Add all modules with no incoming edges (sources)
            foreach (var kvp in inDegree)
            {
                if (kvp.Value == 0)
                {
                    queue.Enqueue(kvp.Key);
                }
            }
            
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                _topologicalOrder.Add(current);
                
                foreach (string neighbor in adjacencyList[current])
                {
                    inDegree[neighbor]--;
                    if (inDegree[neighbor] == 0)
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
            
            // Check if all modules were processed (no circular dependencies)
            if (_topologicalOrder.Count != _modules.Count)
            {
                Debug.LogError($"WaveModuleGraph: Circular dependency detected. Processed {_topologicalOrder.Count}/{_modules.Count} modules");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Validate graph structure
        /// </summary>
        public bool Validate()
        {
            if (_modules.Count == 0)
            {
                Debug.LogWarning("WaveModuleGraph: Graph has no modules");
                return false;
            }
            
            // Validate all modules
            foreach (var module in _modules.Values)
            {
                if (!module.Validate())
                {
                    Debug.LogError($"WaveModuleGraph: Module {module.ModuleID} validation failed");
                    return false;
                }
            }
            
            // Validate cables
            foreach (var cable in _cables)
            {
                if (!_modules.ContainsKey(cable.SourceModuleID))
                {
                    Debug.LogError($"WaveModuleGraph: Cable references non-existent source module {cable.SourceModuleID}");
                    return false;
                }
                
                if (!_modules.ContainsKey(cable.TargetModuleID))
                {
                    Debug.LogError($"WaveModuleGraph: Cable references non-existent target module {cable.TargetModuleID}");
                    return false;
                }
            }
            
            // Validate output
            if (!string.IsNullOrEmpty(_outputModuleID))
            {
                if (!_modules.ContainsKey(_outputModuleID))
                {
                    Debug.LogError($"WaveModuleGraph: Output module {_outputModuleID} not found");
                    return false;
                }
            }
            
            return true;
        }
        
        #endregion
        
        #region Serialization
        
        /// <summary>
        /// Save graph to GraphData
        /// </summary>
        public void SaveToData()
        {
            GraphData.modules.Clear();
            GraphData.cables.Clear();
            
            // Save modules
            foreach (var module in _modules.Values)
            {
                var moduleData = new WaveModuleData
                {
                    moduleID = module.ModuleID,
                    moduleType = module.ModuleType,
                    inputPorts = new List<WaveModulePort>(module.InputPorts),
                    outputPorts = new List<WaveModulePort>(module.OutputPorts)
                };
                
                // Get module type name (for deserialization)
                moduleData.moduleTypeName = module.GetType().Name;
                
                // Set parameters using the helper method
                moduleData.SetParameters(module.GetParameters());
                
                GraphData.modules.Add(moduleData);
            }
            
            // Save cables
            GraphData.cables.AddRange(_cables);
            
            // Save output
            GraphData.outputModuleID = _outputModuleID;
            GraphData.outputPortID = _outputPortID;
            GraphData.lastModifiedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        
        /// <summary>
        /// Load graph from GraphData
        /// Note: This requires a module factory to instantiate modules from type names
        /// </summary>
        public void LoadFromData(WaveModuleGraphData data)
        {
            if (data == null)
            {
                return;
            }
            
            GraphData = data;
            
            // Clear current state
            _modules.Clear();
            _cables.Clear();
            _outputModuleID = data.outputModuleID;
            _outputPortID = data.outputPortID;
            
            // Modules will be loaded by external factory (not implemented here)
            // This is because we need to instantiate concrete module types
            
            // Load cables
            if (data.cables != null)
            {
                _cables.AddRange(data.cables);
            }
            
            _isDirty = true;
        }
        
        #endregion
    }
}

