using Resonance.Systems.Waves;
using System.Collections.Generic;
using UnityEngine;

namespace Resonance.Systems.Waves.Editors
{
    /// <summary>
    /// VCA (Voltage Controlled Amplifier) module
    /// Amplifies or attenuates the input wave based on gain parameter
    /// </summary>
    public class VCA : IWaveModule
    {
        private string _moduleID;
        private List<WaveModulePort> _inputPorts;
        private List<WaveModulePort> _outputPorts;
        
        // Parameters
        private float _gain = 1.0f; // Gain multiplier (0.0 to infinity, 1.0 = no change)
        
        public string ModuleID => _moduleID;
        public string ModuleName => "VCA (Amplifier)";
        public WaveModuleType ModuleType => WaveModuleType.Processor;
        public IReadOnlyList<WaveModulePort> InputPorts => _inputPorts;
        public IReadOnlyList<WaveModulePort> OutputPorts => _outputPorts;
        
        public bool IsReady
        {
            get
            {
                // Check if input port has a connection
                var inputPort = _inputPorts[0];
                return inputPort.ConnectionCount > 0;
            }
        }
        
        public VCA()
        {
            _moduleID = System.Guid.NewGuid().ToString();
            _inputPorts = new List<WaveModulePort>
            {
                new WaveModulePort("input", "Input", WaveModulePortType.Input, true)
            };
            _outputPorts = new List<WaveModulePort>
            {
                new WaveModulePort("output", "Output", WaveModulePortType.Output)
            };
        }
        
        public VCA(string moduleID)
        {
            _moduleID = moduleID;
            _inputPorts = new List<WaveModulePort>
            {
                new WaveModulePort("input", "Input", WaveModulePortType.Input, true)
            };
            _outputPorts = new List<WaveModulePort>
            {
                new WaveModulePort("output", "Output", WaveModulePortType.Output)
            };
        }
        
        public Dictionary<string, Wave> Process(Dictionary<string, Wave> inputWaves)
        {
            if (!inputWaves.TryGetValue("input", out Wave inputWave) || inputWave == null)
            {
                Debug.LogWarning("VCA: No input wave provided");
                return new Dictionary<string, Wave>();
            }
            
            // Clamp gain to prevent negative values (attenuation is handled by gain < 1.0)
            _gain = Mathf.Max(0f, _gain);
            
            // Processors modify existing Wave - clone input to avoid modifying original
            Wave amplifiedWave = inputWave.Clone();
            
            // Handle extreme gain values
            bool isExtremeGain = WaveConstants.IsExtremeHigh(_gain) || _gain <= 0f;
            
            // Apply gain to amplitude
            float newAmplitude = amplifiedWave.Amplitude * _gain;
            
            // Handle extreme values
            if (isExtremeGain)
            {
                if (_gain <= 0f)
                {
                    // Zero or negative gain = silence
                    newAmplitude = 0f;
                }
                else if (WaveConstants.IsExtremeHigh(_gain))
                {
                    // Extreme gain = extreme amplitude (will be clamped by Wave)
                    newAmplitude = float.MaxValue * 0.1f; // Prevent overflow
                }
            }
            
            // Ensure minimum amplitude
            if (!isExtremeGain)
            {
                newAmplitude = Mathf.Max(WaveConstants.MIN_AMPLITUDE, newAmplitude);
            }
            
            // Update wave properties (keep waveform table, only modify amplitude)
            amplifiedWave.UpdateWaveProperties(
                amplifiedWave.WaveformType,
                amplifiedWave.Frequency,
                newAmplitude,
                amplifiedWave.Unit,
                amplifiedWave.WaveformTable // Keep original waveform table
            );
            
            Dictionary<string, Wave> outputs = new Dictionary<string, Wave>
            {
                ["output"] = amplifiedWave
            };
            
            return outputs;
        }
        
        public bool Validate()
        {
            return _inputPorts.Count > 0 && 
                   _outputPorts.Count > 0 &&
                   _gain >= 0f;
        }
        
        public Dictionary<string, object> GetParameters()
        {
            return new Dictionary<string, object>
            {
                ["gain"] = _gain
            };
        }
        
        public void SetParameters(Dictionary<string, object> parameters)
        {
            if (parameters != null)
            {
                if (parameters.TryGetValue("gain", out object gainObj))
                {
                    if (gainObj is float g)
                    {
                        _gain = Mathf.Max(0f, g);
                    }
                }
            }
        }
        
        public void Reset()
        {
            _gain = 1.0f;
        }
    }
}

