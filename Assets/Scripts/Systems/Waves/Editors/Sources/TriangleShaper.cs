using Resonance.Systems.Waves;
using Resonance.Shared.Types;
using System.Collections.Generic;
using UnityEngine;

namespace Resonance.Systems.Waves.Editors
{
    /// <summary>
    /// Triangle/Sawtooth wave shaper module
    /// Generates triangle or sawtooth waves with configurable rise and fall amplitudes
    /// </summary>
    public class TriangleShaper : IWaveModule
    {
        private string _moduleID;
        private List<WaveModulePort> _inputPorts;
        private List<WaveModulePort> _outputPorts;
        
        // Parameters
        private float _riseAmplitude = 1.0f; // Amplitude during rise phase
        private float _fallAmplitude = 1.0f; // Amplitude during fall phase
        
        public string ModuleID => _moduleID;
        public string ModuleName => "Triangle Shaper";
        public WaveModuleType ModuleType => WaveModuleType.Source;
        public IReadOnlyList<WaveModulePort> InputPorts => _inputPorts;
        public IReadOnlyList<WaveModulePort> OutputPorts => _outputPorts;
        public bool IsReady => true; // Sources are always ready
        
        public TriangleShaper()
        {
            _moduleID = System.Guid.NewGuid().ToString();
            _inputPorts = new List<WaveModulePort>();
            _outputPorts = new List<WaveModulePort>
            {
                new WaveModulePort("output", "Output", WaveModulePortType.Output)
            };
        }
        
        public TriangleShaper(string moduleID)
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
            // Clamp amplitudes to valid range
            _riseAmplitude = Mathf.Max(WaveConstants.MIN_AMPLITUDE, _riseAmplitude);
            _fallAmplitude = Mathf.Max(WaveConstants.MIN_AMPLITUDE, _fallAmplitude);
            
            // Sources initialize Wave data
            Wave wave = Wave.CreateDefault();
            
            // Determine waveform type based on amplitudes
            WaveformType waveformType;
            if (Mathf.Approximately(_riseAmplitude, _fallAmplitude))
            {
                waveformType = WaveformType.Triangle; // Symmetric triangle
            }
            else
            {
                waveformType = WaveformType.Sawtooth; // Asymmetric sawtooth
            }
            
            // Generate triangle waveform table with custom rise/fall amplitudes
            float[] waveformTable = WaveformTableGenerator.GenerateTriangleParameterized(
                WaveConstants.DEFAULT_WAVEFORM_RESOLUTION,
                _riseAmplitude,
                _fallAmplitude
            );
            
            // Update wave properties with generated data
            wave.UpdateWaveProperties(
                waveformType,
                1.0f, // Default frequency
                Mathf.Max(_riseAmplitude, _fallAmplitude), // Use max amplitude
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
            return _outputPorts.Count > 0 && 
                   _riseAmplitude >= WaveConstants.MIN_AMPLITUDE &&
                   _fallAmplitude >= WaveConstants.MIN_AMPLITUDE;
        }
        
        public Dictionary<string, object> GetParameters()
        {
            return new Dictionary<string, object>
            {
                ["riseAmplitude"] = _riseAmplitude,
                ["fallAmplitude"] = _fallAmplitude
            };
        }
        
        public void SetParameters(Dictionary<string, object> parameters)
        {
            if (parameters != null)
            {
                if (parameters.TryGetValue("riseAmplitude", out object riseAmpObj))
                {
                    if (riseAmpObj is float ra)
                    {
                        _riseAmplitude = Mathf.Max(WaveConstants.MIN_AMPLITUDE, ra);
                    }
                }
                
                if (parameters.TryGetValue("fallAmplitude", out object fallAmpObj))
                {
                    if (fallAmpObj is float fa)
                    {
                        _fallAmplitude = Mathf.Max(WaveConstants.MIN_AMPLITUDE, fa);
                    }
                }
            }
        }
        
        public void Reset()
        {
            _riseAmplitude = 1.0f;
            _fallAmplitude = 1.0f;
        }
    }
}

