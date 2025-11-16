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
            // Sources initialize Wave data
            Wave wave = Wave.CreateDefault();
            
            // Generate noise waveform table
            float[] waveformTable = WaveformTableGenerator.GenerateNoise(
                WaveConstants.DEFAULT_WAVEFORM_RESOLUTION
            );
            
            // Update wave properties with generated data
            // Note: Noise uses Custom waveform type since it's not a standard waveform
            wave.UpdateWaveProperties(
                WaveformType.Custom, // Noise is a custom waveform
                1.0f, // Default frequency
                1.0f, // Default amplitude
                1.0f, // Default unit
                waveformTable
            );
            
            Dictionary<string, Wave> outputs = new Dictionary<string, Wave>
            {
                ["output"] = wave
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

