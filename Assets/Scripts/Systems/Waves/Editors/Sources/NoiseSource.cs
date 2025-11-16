using Resonance.Systems.Waves;
using Resonance.Shared.Types;
using System.Collections.Generic;
using UnityEngine;

namespace Resonance.Systems.Waves.Editors
{
    /// <summary>
    /// Noise source module
    /// Generates random noise wave with no parameters
    /// </summary>
    public class NoiseSource : IWaveModule
    {
        private string _moduleID;
        private List<WaveModulePort> _inputPorts;
        private List<WaveModulePort> _outputPorts;
        
        public string ModuleID => _moduleID;
        public string ModuleName => "Noise Source";
        public WaveModuleType ModuleType => WaveModuleType.Source;
        public IReadOnlyList<WaveModulePort> InputPorts => _inputPorts;
        public IReadOnlyList<WaveModulePort> OutputPorts => _outputPorts;
        public bool IsReady => true; // Sources are always ready
        
        public NoiseSource()
        {
            _moduleID = System.Guid.NewGuid().ToString();
            _inputPorts = new List<WaveModulePort>();
            _outputPorts = new List<WaveModulePort>
            {
                new WaveModulePort("output", "Output", WaveModulePortType.Output)
            };
        }
        
        public NoiseSource(string moduleID)
        {
            _moduleID = moduleID;
            _inputPorts = new List<WaveModulePort>();
            _outputPorts = new List<WaveModulePort>
            {
                new WaveModulePort("output", "Output", WaveModulePortType.Output)
            };
        }
        
        public Dictionary<string, Wave> Process(Dictionary<string, Wave> inputWaves)
        {
            // Generate noise wave
            // For noise, we'll use a custom waveform type
            // In practice, noise would be generated differently, but we'll use
            // a Constant waveform as a placeholder (or you could implement custom noise generation)
            
            WaveConfig config = ScriptableObject.CreateInstance<WaveConfig>();
            config.waveformType = WaveformType.Constant; // Placeholder - noise would need custom implementation
            config.frequency = 1.0f;
            config.amplitude = 1.0f;
            config.unit = 1.0f;
            config.waveformResolution = WaveConstants.DEFAULT_WAVEFORM_RESOLUTION;
            
            Wave noiseWave = new Wave(config);
            
            // Note: True noise generation would require generating random samples
            // in the waveform table. This is a simplified implementation.
            
            Object.Destroy(config);
            
            Dictionary<string, Wave> outputs = new Dictionary<string, Wave>
            {
                ["output"] = noiseWave
            };
            
            return outputs;
        }
        
        public bool Validate()
        {
            return _outputPorts.Count > 0;
        }
        
        public Dictionary<string, object> GetParameters()
        {
            return new Dictionary<string, object>(); // No parameters
        }
        
        public void SetParameters(Dictionary<string, object> parameters)
        {
            // No parameters to set
        }
        
        public void Reset()
        {
            // Nothing to reset
        }
    }
}

