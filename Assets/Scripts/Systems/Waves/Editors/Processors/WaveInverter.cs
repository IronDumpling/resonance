using Resonance.Systems.Waves;
using System.Collections.Generic;
using UnityEngine;

namespace Resonance.Systems.Waves.Editors
{
    /// <summary>
    /// Wave Inverter module
    /// Inverts the waveform (multiplies by -1)
    /// Replaces the old WaveModifier functionality
    /// </summary>
    public class WaveInverter : IWaveModule
    {
        private string _moduleID;
        private List<WaveModulePort> _inputPorts;
        private List<WaveModulePort> _outputPorts;
        
        public string ModuleID => _moduleID;
        public string ModuleName => "Wave Inverter";
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
        
        public WaveInverter()
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
        
        public WaveInverter(string moduleID)
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
                Debug.LogWarning("WaveInverter: No input wave provided");
                return new Dictionary<string, Wave>();
            }
            
            // Processors modify existing Wave - clone input to avoid modifying original
            Wave invertedWave = inputWave.Clone();
            
            // Invert waveform table (multiply by -1)
            float[] waveformTable = invertedWave.WaveformTable;
            if (waveformTable != null)
            {
                float[] invertedTable = new float[waveformTable.Length];
                for (int i = 0; i < waveformTable.Length; i++)
                {
                    invertedTable[i] = -waveformTable[i];
                }
                
                // Update waveform table
                invertedWave.UpdateWaveformTable(invertedTable);
            }
            
            Dictionary<string, Wave> outputs = new Dictionary<string, Wave>
            {
                ["output"] = invertedWave
            };
            
            return outputs;
        }
        
        public bool Validate()
        {
            return _inputPorts.Count > 0 && _outputPorts.Count > 0;
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

