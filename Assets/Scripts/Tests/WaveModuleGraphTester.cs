using UnityEngine;
using Resonance.Systems.Waves;
using Resonance.Systems.Waves.Editors;
using System.Collections.Generic;

namespace Resonance.Tests
{
    /// <summary>
    /// Manual testing tool for WaveModuleGraph without UI
    /// Can be attached to any GameObject in a test scene
    /// </summary>
    public class WaveModuleGraphTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestOnStart = true;
        [SerializeField] private bool logDetailedResults = true;
        
        private void Start()
        {
            if (runTestOnStart)
            {
                RunAllTests();
            }
        }
        
        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            Debug.Log("=== Starting Wave Module Graph Tests ===\n");
            
            TestBasicWaveCreation();
            TestModuleConnection();
            TestGraphExecution();
            TestComplexGraph();
            
            Debug.Log("\n=== All Tests Complete ===");
        }
        
        [ContextMenu("Test 1: Basic Wave Creation")]
        private void TestBasicWaveCreation()
        {
            Debug.Log("Test 1: Basic Wave Creation");
            
            // Create a simple wave
            WaveConfig config = ScriptableObject.CreateInstance<WaveConfig>();
            config.waveformType = Shared.Types.WaveformType.Sine;
            config.frequency = 2.0f;
            config.amplitude = 1.0f;
            config.unit = 1.0f;
            
            Wave wave = new Wave(config);
            
            // Generate waveform table
            float[] table = WaveformTableGenerator.Generate(
                Shared.Types.WaveformType.Sine, 
                WaveConstants.DEFAULT_WAVEFORM_RESOLUTION
            );
            wave.UpdateWaveProperties(
                Shared.Types.WaveformType.Sine, 
                2.0f, 1.0f, 1.0f, table
            );
            
            // Verify properties
            bool passed = wave.Frequency == 2.0f && 
                          wave.Amplitude == 1.0f && 
                          wave.EnergyStrength > 0;
            
            Debug.Log($"  Result: {(passed ? "✓ PASS" : "✗ FAIL")}");
            if (logDetailedResults)
            {
                Debug.Log($"  Wave Energy: {wave.EnergyStrength:F2}");
                Debug.Log($"  Wave Speed: {wave.Speed:F2}");
            }
            
            Destroy(config);
            wave.Cleanup();
        }
        
        [ContextMenu("Test 2: Module Connection")]
        private void TestModuleConnection()
        {
            Debug.Log("\nTest 2: Module Connection");
            
            // Create graph
            WaveModuleGraph graph = new WaveModuleGraph();
            
            // Create modules (you'll need to create actual instances)
            // Example with SineOscillator
            var sineSource = new SineOscillator();
            var vcaProcessor = new VCA();
            
            // Add to graph
            bool addedSource = graph.AddModule(sineSource);
            bool addedProcessor = graph.AddModule(vcaProcessor);
            
            // Connect them
            bool connected = false;
            if (addedSource && addedProcessor && 
                sineSource.OutputPorts.Count > 0 && 
                vcaProcessor.InputPorts.Count > 0)
            {
                connected = graph.ConnectModules(
                    sineSource.ModuleID, 
                    sineSource.OutputPorts[0].PortID,
                    vcaProcessor.ModuleID, 
                    vcaProcessor.InputPorts[0].PortID
                );
            }
            
            bool passed = addedSource && addedProcessor && connected;
            Debug.Log($"  Result: {(passed ? "✓ PASS" : "✗ FAIL")}");
            if (logDetailedResults)
            {
                Debug.Log($"  Modules added: {graph.ModuleCount}");
                Debug.Log($"  Connections: {graph.ConnectionCount}");
            }
        }
        
        [ContextMenu("Test 3: Graph Execution")]
        private void TestGraphExecution()
        {
            Debug.Log("\nTest 3: Graph Execution");
            
            // Create a simple graph: Sine -> Output
            WaveModuleGraph graph = new WaveModuleGraph();
            
            var sineSource = new SineOscillator();
            graph.AddModule(sineSource);
            
            // Set as output
            if (sineSource.OutputPorts.Count > 0)
            {
                graph.SetOutput(sineSource.ModuleID, sineSource.OutputPorts[0].PortID);
            }
            
            // Execute graph
            Wave resultWave = graph.Execute();
            
            bool passed = resultWave != null && resultWave.Frequency > 0;
            Debug.Log($"  Result: {(passed ? "✓ PASS" : "✗ FAIL")}");
            
            if (logDetailedResults && resultWave != null)
            {
                Debug.Log($"  Output Wave Type: {resultWave.WaveformType}");
                Debug.Log($"  Output Energy: {resultWave.EnergyStrength:F2}");
            }
            
            resultWave?.Cleanup();
        }
        
        [ContextMenu("Test 4: Complex Graph")]
        private void TestComplexGraph()
        {
            Debug.Log("\nTest 4: Complex Graph (Sine -> VCF -> VCA -> Output)");
            
            WaveModuleGraph graph = new WaveModuleGraph();
            
            // Create modules
            var sine = new SineOscillator();
            var vcf = new VCF();
            var vca = new VCA();
            
            // Add modules
            graph.AddModule(sine);
            graph.AddModule(vcf);
            graph.AddModule(vca);
            
            // Connect: Sine -> VCF -> VCA
            bool conn1 = false, conn2 = false;
            
            if (sine.OutputPorts.Count > 0 && vcf.InputPorts.Count > 0)
            {
                conn1 = graph.ConnectModules(
                    sine.ModuleID, sine.OutputPorts[0].PortID,
                    vcf.ModuleID, vcf.InputPorts[0].PortID
                );
            }
            
            if (vcf.OutputPorts.Count > 0 && vca.InputPorts.Count > 0)
            {
                conn2 = graph.ConnectModules(
                    vcf.ModuleID, vcf.OutputPorts[0].PortID,
                    vca.ModuleID, vca.InputPorts[0].PortID
                );
            }
            
            // Set VCA as output
            if (vca.OutputPorts.Count > 0)
            {
                graph.SetOutput(vca.ModuleID, vca.OutputPorts[0].PortID);
            }
            
            // Execute
            Wave result = graph.Execute();
            
            bool passed = conn1 && conn2 && result != null;
            Debug.Log($"  Result: {(passed ? "✓ PASS" : "✗ FAIL")}");
            
            if (logDetailedResults)
            {
                Debug.Log($"  Graph modules: {graph.ModuleCount}");
                Debug.Log($"  Graph connections: {graph.ConnectionCount}");
                if (result != null)
                {
                    Debug.Log($"  Final wave energy: {result.EnergyStrength:F2}");
                }
            }
            
            result?.Cleanup();
        }
        
        // Add more specific tests as needed
        [ContextMenu("Test Serialization")]
        private void TestSerialization()
        {
            Debug.Log("\nTest: Graph Serialization");
            
            // Create graph with modules
            WaveModuleGraph graph = new WaveModuleGraph();
            var sine = new SineOscillator();
            graph.AddModule(sine);
            
            // Get graph data
            var graphData = graph.GraphData;
            
            // Create new graph from data
            WaveModuleGraph newGraph = new WaveModuleGraph(graphData);
            
            bool passed = newGraph.ModuleCount == graph.ModuleCount;
            Debug.Log($"  Result: {(passed ? "✓ PASS" : "✗ FAIL")}");
        }
    }
}