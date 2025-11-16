using Resonance.Systems.Waves;
using System.Collections.Generic;

namespace Resonance.Systems.Waves.Editors
{
    /// <summary>
    /// Interface for all wave modules in the modular synthesizer system
    /// Modules can be Sources (generators), Processors, or Outputs
    /// </summary>
    public interface IWaveModule
    {
        /// <summary>
        /// Unique identifier for this module instance
        /// </summary>
        string ModuleID { get; }
        
        /// <summary>
        /// Display name of the module
        /// </summary>
        string ModuleName { get; }
        
        /// <summary>
        /// Module type (Source, Processor, Output)
        /// </summary>
        WaveModuleType ModuleType { get; }
        
        /// <summary>
        /// Get all input ports (for receiving waves from other modules)
        /// </summary>
        IReadOnlyList<WaveModulePort> InputPorts { get; }
        
        /// <summary>
        /// Get all output ports (for sending waves to other modules)
        /// </summary>
        IReadOnlyList<WaveModulePort> OutputPorts { get; }
        
        /// <summary>
        /// Check if this module is ready to process (all required inputs connected)
        /// </summary>
        bool IsReady { get; }
        
        /// <summary>
        /// Process the module and generate output wave(s)
        /// Called during graph execution in topological order
        /// </summary>
        /// <param name="inputWaves">Dictionary of port ID to input wave</param>
        /// <returns>Dictionary of port ID to output wave</returns>
        Dictionary<string, Wave> Process(Dictionary<string, Wave> inputWaves);
        
        /// <summary>
        /// Validate module configuration and parameters
        /// </summary>
        bool Validate();
        
        /// <summary>
        /// Get module parameters as serializable dictionary
        /// </summary>
        Dictionary<string, object> GetParameters();
        
        /// <summary>
        /// Set module parameters from dictionary
        /// </summary>
        void SetParameters(Dictionary<string, object> parameters);
        
        /// <summary>
        /// Reset module to initial state
        /// </summary>
        void Reset();
    }
    
    /// <summary>
    /// Module type enumeration
    /// </summary>
    public enum WaveModuleType
    {
        Source,      // Wave generators (Sine, Pulse, etc.)
        Processor,   // Wave modifiers (VCF, VCA, etc.)
        Output       // Output destinations (WaveGun, CrystalCore, Diffuser)
    }
}

