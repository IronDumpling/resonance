using Resonance.Systems.Waves;
using System.Collections.Generic;
using UnityEngine;

namespace Resonance.Systems.Waves.Editors
{
    /// <summary>
    /// VCF (Voltage Controlled Filter) module
    /// Filters the input wave based on cutoff frequency and resonance
    /// </summary>
    public class VCF : IWaveModule
    {
        private string _moduleID;
        private List<WaveModulePort> _inputPorts;
        private List<WaveModulePort> _outputPorts;
        
        // Parameters
        private float _cutoff = 1.0f;      // Cutoff frequency (0.1 to 10.0 typical)
        private float _resonance = 0.5f;   // Resonance/Q factor (0.0 to 1.0)
        
        public string ModuleID => _moduleID;
        public string ModuleName => "VCF (Filter)";
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
        
        public VCF()
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
        
        public VCF(string moduleID)
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
                Debug.LogWarning("VCF: No input wave provided");
                return new Dictionary<string, Wave>();
            }
            
            // Clamp parameters to valid ranges
            _cutoff = Mathf.Clamp(_cutoff, WaveConstants.MIN_FREQUENCY, WaveConstants.MAX_FREQUENCY_NORMALIZATION * 2f);
            _resonance = Mathf.Clamp01(_resonance);
            
            // Apply filter: modify frequency based on cutoff
            // Higher cutoff = less filtering (more frequencies pass through)
            // Lower cutoff = more filtering (fewer frequencies pass through)
            // Resonance affects the sharpness of the filter
            
            // Create new wave config with filtered properties
            WaveConfig config = ScriptableObject.CreateInstance<WaveConfig>();
            config.waveformType = inputWave.WaveformType;
            
            // Apply cutoff to frequency (simplified filter model)
            // Cutoff acts as a multiplier/divider on the frequency
            float filteredFrequency = inputWave.Frequency * _cutoff;
            filteredFrequency = Mathf.Clamp(filteredFrequency, WaveConstants.MIN_FREQUENCY, float.MaxValue);
            config.frequency = filteredFrequency;
            
            // Resonance affects amplitude (higher resonance = more emphasis at cutoff)
            // Simplified: resonance reduces amplitude slightly
            float amplitudeMultiplier = 1.0f - (_resonance * 0.2f); // Max 20% reduction
            config.amplitude = inputWave.Amplitude * amplitudeMultiplier;
            config.amplitude = Mathf.Max(WaveConstants.MIN_AMPLITUDE, config.amplitude);
            
            config.unit = inputWave.Unit;
            config.waveformResolution = inputWave.Resolution;
            
            Wave filteredWave = new Wave(config);
            
            Object.Destroy(config);
            
            Dictionary<string, Wave> outputs = new Dictionary<string, Wave>
            {
                ["output"] = filteredWave
            };
            
            return outputs;
        }
        
        public bool Validate()
        {
            return _inputPorts.Count > 0 && 
                   _outputPorts.Count > 0 &&
                   _cutoff >= WaveConstants.MIN_FREQUENCY &&
                   _resonance >= 0f && _resonance <= 1f;
        }
        
        public Dictionary<string, object> GetParameters()
        {
            return new Dictionary<string, object>
            {
                ["cutoff"] = _cutoff,
                ["resonance"] = _resonance
            };
        }
        
        public void SetParameters(Dictionary<string, object> parameters)
        {
            if (parameters != null)
            {
                if (parameters.TryGetValue("cutoff", out object cutoffObj))
                {
                    if (cutoffObj is float c)
                    {
                        _cutoff = Mathf.Clamp(c, WaveConstants.MIN_FREQUENCY, WaveConstants.MAX_FREQUENCY_NORMALIZATION * 2f);
                    }
                }
                
                if (parameters.TryGetValue("resonance", out object resonanceObj))
                {
                    if (resonanceObj is float r)
                    {
                        _resonance = Mathf.Clamp01(r);
                    }
                }
            }
        }
        
        public void Reset()
        {
            _cutoff = 1.0f;
            _resonance = 0.5f;
        }
    }
}

