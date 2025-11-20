using Resonance.Systems.Waves;
using Resonance.Shared.Types;
using System.Collections.Generic;
using UnityEngine;

namespace Resonance.Systems.Waves.Editors
{
    /// <summary>
    /// Pulse/Square wave generator module
    /// Generates a pulse wave with configurable pulse width
    /// </summary>
    public class PulseGenerator : IWaveModule
    {
        private string _moduleID;
        private List<WaveModulePort> _inputPorts;
        private List<WaveModulePort> _outputPorts;
        
        // Parameters
        private float _pulseWidth = 0.5f; // 0.0 to 1.0, where 0.5 = square wave
        
        public string ModuleID => _moduleID;
        public string ModuleName => "Pulse Generator";
        public WaveModuleType ModuleType => WaveModuleType.Source;
        public IReadOnlyList<WaveModulePort> InputPorts => _inputPorts;
        public IReadOnlyList<WaveModulePort> OutputPorts => _outputPorts;
        public bool IsReady => true; // Sources are always ready
        
        public PulseGenerator()
        {
            _moduleID = System.Guid.NewGuid().ToString();
            _inputPorts = new List<WaveModulePort>();
            _outputPorts = new List<WaveModulePort>
            {
                new WaveModulePort("output", "Output", WaveModulePortType.Output)
            };
        }
        
        public PulseGenerator(string moduleID)
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
            // Clamp pulse width to valid range
            _pulseWidth = Mathf.Clamp01(_pulseWidth);
            
            // Sources initialize Wave data
            Wave wave = Wave.CreateDefault();
            
            // Generate pulse/square waveform table with custom duty cycle
            float[] waveformTable = WaveformTableGenerator.GenerateSquareParameterized(
                WaveConstants.DEFAULT_WAVEFORM_RESOLUTION,
                _pulseWidth
            );
            
            // Update wave properties with generated data
            wave.UpdateWaveProperties(
                WaveformType.Pulse,
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
            return _outputPorts.Count > 0 && _pulseWidth >= 0f && _pulseWidth <= 1f;
        }
        
        public Dictionary<string, object> GetParameters()
        {
            return new Dictionary<string, object>
            {
                ["pulseWidth"] = _pulseWidth
            };
        }
        
        public void SetParameters(Dictionary<string, object> parameters)
        {
            if (parameters != null)
            {
                if (parameters.TryGetValue("pulseWidth", out object pulseWidthObj))
                {
                    if (pulseWidthObj is float pw)
                    {
                        _pulseWidth = Mathf.Clamp01(pw);
                    }
                }
            }
        }
        
        public void Reset()
        {
            _pulseWidth = 0.5f;
        }
    }
}

