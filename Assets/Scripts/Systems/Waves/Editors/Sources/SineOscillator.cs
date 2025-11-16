using Resonance.Systems.Waves;
using Resonance.Shared.Types;
using System.Collections.Generic;
using UnityEngine;

namespace Resonance.Systems.Waves.Editors
{
    /// <summary>
    /// Sine wave oscillator module
    /// Generates a pure sine wave with no parameters
    /// </summary>
    public class SineOscillator : IWaveModule
    {
        private string _moduleID;
        private List<WaveModulePort> _inputPorts;
        private List<WaveModulePort> _outputPorts;
        
        public string ModuleID => _moduleID;
        public string ModuleName => "Sine Oscillator";
        public WaveModuleType ModuleType => WaveModuleType.Source;
        public IReadOnlyList<WaveModulePort> InputPorts => _inputPorts;
        public IReadOnlyList<WaveModulePort> OutputPorts => _outputPorts;
        public bool IsReady => true; // Sources are always ready (no inputs required)
        
        public SineOscillator()
        {
            _moduleID = System.Guid.NewGuid().ToString();
            _inputPorts = new List<WaveModulePort>();
            _outputPorts = new List<WaveModulePort>
            {
                new WaveModulePort("output", "Output", WaveModulePortType.Output)
            };
        }
        
        public SineOscillator(string moduleID)
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
            // Generate sine wave
            // Create a WaveConfig for sine wave
            WaveConfig config = ScriptableObject.CreateInstance<WaveConfig>();
            config.waveformType = WaveformType.Sine;
            config.frequency = 1.0f; // Default frequency
            config.amplitude = 1.0f; // Default amplitude
            config.unit = 1.0f;
            config.waveformResolution = WaveConstants.DEFAULT_WAVEFORM_RESOLUTION;
            
            Wave sineWave = new Wave(config);
            
            // Cleanup temporary config
            Object.Destroy(config);
            
            Dictionary<string, Wave> outputs = new Dictionary<string, Wave>
            {
                ["output"] = sineWave
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

