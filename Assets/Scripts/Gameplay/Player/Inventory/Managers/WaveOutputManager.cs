// Scripts/Gameplay/Player/Inventory/WaveOutputManager.cs
using UnityEngine;
using System.Collections.Generic;
using Resonance.Systems.GridSystem;
using Resonance.Systems.Waves;
using Resonance.Systems.Waves.Editors;
using Resonance.Shared.Types;
using Resonance.Gameplay.Items.Core;


namespace Resonance.Gameplay.Player.Inventory
{
    /// <summary>
    /// WaveOutputManager - Manages all wave output devices (WaveGun, CrystalCore, WaveDiffuser)
    /// Replaces WeaponManager with wave-centric functionality
    /// Responsibilities:
    /// - Equip/unequip wave output devices
    /// - Set output waves from WaveModuleGraph
    /// - Trigger wave output actions
    /// </summary>
    public class WaveOutputManager
    {
        private PlayerInventory _inventory;
        private WaveModuleManager _moduleManager;
        
        // Current equipped output device
        private int _equippedOutputID = -1;
        private WaveOutputDataAsset _cachedOutputAsset;
        private WaveOutputType _currentOutputType;
        
        // Current wave (from module graph execution)
        private Wave _currentWave;
        
        // Events
        public System.Action<WaveOutputDataAsset, WaveOutputType> OnOutputEquipped;
        public System.Action OnOutputUnequipped;
        public System.Action<Wave> OnWaveUpdated;
        
        // Properties
        public bool HasEquippedOutput => _equippedOutputID != -1 && _cachedOutputAsset != null;
        public WaveOutputDataAsset CurrentOutput => _cachedOutputAsset;
        public WaveOutputType CurrentOutputType => _currentOutputType;
        public int EquippedOutputID => _equippedOutputID;
        public Wave CurrentWave => _currentWave;
        
        public WaveOutputManager(PlayerInventory inventory, WaveModuleManager moduleManager)
        {
            _inventory = inventory;
            _moduleManager = moduleManager;
            
            // Listen to inventory events
            _inventory.OnWeaponEquipped += OnInventoryOutputEquipped;
            _inventory.OnWeaponUnequipped += OnInventoryOutputUnequipped;
            
            // Listen to module manager events
            if (_moduleManager != null)
            {
                _moduleManager.OnGraphExecuted += OnModuleGraphExecuted;
            }
            
            Debug.Log("WaveOutputManager: Initialized");
        }
        
        #region Output Equip/Unequip
        
        /// <summary>
        /// Equip wave output device by ID
        /// </summary>
        public bool EquipOutput(int outputItemID)
        {
            var outputData = _inventory.GetItemByID(outputItemID);
            if (outputData == null || outputData.ItemType != ItemType.Weapon)
            {
                Debug.LogWarning($"WaveOutputManager: Output {outputItemID} not found or not a weapon type");
                return false;
            }
            
            // Unequip current
            if (_equippedOutputID != -1)
            {
                UnequipOutput();
            }
            
            // Load output asset
            _cachedOutputAsset = LoadOutputAssetFromData(outputData);
            if (_cachedOutputAsset == null)
            {
                Debug.LogError($"WaveOutputManager: Failed to load output asset");
                return false;
            }
            
            _equippedOutputID = outputItemID;
            _currentOutputType = _cachedOutputAsset.outputType;
            
            // Update inventory
            _inventory.EquipWeapon(_equippedOutputID);
            
            // If we have a module graph, execute it to get initial wave
            if (_moduleManager != null && _moduleManager.HasActiveGraph)
            {
                ExecuteModuleGraph();
            }
            
            OnOutputEquipped?.Invoke(_cachedOutputAsset, _currentOutputType);
            
            Debug.Log($"WaveOutputManager: Equipped {_cachedOutputAsset.outputName} ({_currentOutputType})");
            return true;
        }
        
        /// <summary>
        /// Unequip current output
        /// </summary>
        public void UnequipOutput()
        {
            if (_equippedOutputID == -1) return;
            
            _inventory.UnequipCurrentWeapon();
            
            _equippedOutputID = -1;
            _cachedOutputAsset = null;
            _currentWave = null;
            
            OnOutputUnequipped?.Invoke();
            Debug.Log("WaveOutputManager: Output unequipped");
        }
        
        #endregion
        
        #region Wave Management
        
        /// <summary>
        /// Execute module graph and update current wave
        /// Called when module graph changes or output is equipped
        /// </summary>
        public void ExecuteModuleGraph()
        {
            if (_moduleManager == null || !_moduleManager.HasActiveGraph)
            {
                Debug.LogWarning("WaveOutputManager: No active module graph");
                _currentWave = null;
                return;
            }
            
            Wave generatedWave = _moduleManager.ExecuteGraph();
            if (generatedWave != null)
            {
                SetCurrentWave(generatedWave);
            }
            else
            {
                Debug.LogWarning("WaveOutputManager: Module graph execution returned null");
            }
        }
        
        /// <summary>
        /// Set current wave (from module graph or default)
        /// </summary>
        private void SetCurrentWave(Wave wave)
        {
            // Clean up old wave
            _currentWave?.Cleanup();
            
            _currentWave = wave;
            
            OnWaveUpdated?.Invoke(_currentWave);
            
            Debug.Log($"WaveOutputManager: Wave updated - Energy: {_currentWave?.EnergyStrength ?? 0}");
        }
        
        /// <summary>
        /// Use current output device (fire, activate, etc.)
        /// </summary>
        public bool UseOutput()
        {
            if (!HasEquippedOutput)
            {
                Debug.LogWarning("WaveOutputManager: No output equipped");
                return false;
            }
            
            if (_currentWave == null)
            {
                Debug.LogWarning("WaveOutputManager: No wave set - executing graph");
                ExecuteModuleGraph();
                
                if (_currentWave == null)
                {
                    Debug.LogError("WaveOutputManager: Failed to generate wave");
                    return false;
                }
            }
            
            // Validate wave
            if (_currentWave.IsExtremeState())
            {
                Debug.LogWarning("WaveOutputManager: Wave is in extreme state!");
                // Could trigger special effects or warnings
            }
            
            // Dispatch to appropriate handler based on output type
            switch (_currentOutputType)
            {
                case WaveOutputType.WaveGun:
                    return UseWaveGun();
                    
                case WaveOutputType.CrystalCore:
                    return UseCrystalCore();
                    
                case WaveOutputType.WaveDiffuser:
                    return UseWaveDiffuser();
                    
                default:
                    Debug.LogError($"WaveOutputManager: Unknown output type {_currentOutputType}");
                    return false;
            }
        }
        
        private bool UseWaveGun()
        {
            // Fire wave projectile
            Debug.Log($"WaveOutputManager: Firing WaveGun with wave energy {_currentWave.EnergyStrength}");
            // TODO: Implement projectile firing
            return true;
        }
        
        private bool UseCrystalCore()
        {
            // Activate crystal resonance
            Debug.Log($"WaveOutputManager: Activating CrystalCore");
            // TODO: Implement crystal core activation
            return true;
        }
        
        private bool UseWaveDiffuser()
        {
            // Create wave field
            Debug.Log($"WaveOutputManager: Activating WaveDiffuser");
            // TODO: Implement diffuser activation
            return true;
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnInventoryOutputEquipped(int outputID)
        {
            Debug.Log($"WaveOutputManager: Inventory equipped output {outputID}");
        }
        
        private void OnInventoryOutputUnequipped()
        {
            Debug.Log("WaveOutputManager: Inventory unequipped output");
        }
        
        private void OnModuleGraphExecuted(Wave wave)
        {
            Debug.Log($"WaveOutputManager: Module graph executed, updating wave");
            SetCurrentWave(wave);
        }
        
        #endregion
        
        #region Helper Methods
        
        private WaveOutputDataAsset LoadOutputAssetFromData(GridItem outputData)
        {
            // Try CustomData first
            if (outputData.CustomData.ContainsKey("originalAsset"))
            {
                if (outputData.CustomData["originalAsset"] is WaveOutputDataAsset asset)
                {
                    return asset;
                }
            }
            
            // Try AssetPath
            if (!string.IsNullOrEmpty(outputData.AssetPath))
            {
                return LoadAssetFromPath<WaveOutputDataAsset>(outputData.AssetPath);
            }
            
            Debug.LogError($"WaveOutputManager: Cannot load output asset for {outputData.ItemName}");
            return null;
        }
        
        private T LoadAssetFromPath<T>(string path) where T : ScriptableObject
        {
            #if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            #else
            // Runtime loading logic
            string resourcePath = path;
            if (path.StartsWith("Assets/Resources/"))
            {
                resourcePath = path.Substring("Assets/Resources/".Length);
            }
            if (resourcePath.EndsWith(".asset"))
            {
                resourcePath = resourcePath.Substring(0, resourcePath.Length - ".asset".Length);
            }
            return Resources.Load<T>(resourcePath);
            #endif
        }
        
        #endregion
        
        #region Save/Load
        
        public WaveOutputManagerSaveData GetSaveData()
        {
            return new WaveOutputManagerSaveData
            {
                equippedOutputID = _equippedOutputID,
                outputName = _cachedOutputAsset?.outputName ?? "",
                outputType = _currentOutputType,
                assetPath = _cachedOutputAsset != null ? GetAssetPath(_cachedOutputAsset) : "",
                currentWaveSaveData = _currentWave?.GetSaveData()
            };
        }
        
        public void LoadFromSaveData(WaveOutputManagerSaveData saveData)
        {
            if (saveData == null || saveData.equippedOutputID == -1)
            {
                _equippedOutputID = -1;
                _cachedOutputAsset = null;
                _currentWave = null;
                return;
            }
            
            var outputData = _inventory.GetItemByID(saveData.equippedOutputID);
            if (outputData != null)
            {
                _cachedOutputAsset = LoadOutputAssetFromData(outputData);
                if (_cachedOutputAsset != null)
                {
                    _equippedOutputID = saveData.equippedOutputID;
                    _currentOutputType = saveData.outputType;
                    _inventory.EquipWeapon(_equippedOutputID);
                    
                    // Restore wave
                    if (saveData.currentWaveSaveData != null)
                    {
                        _currentWave = Wave.CreateDefault();
                        _currentWave.LoadFromSaveData(saveData.currentWaveSaveData);
                    }
                    
                    OnOutputEquipped?.Invoke(_cachedOutputAsset, _currentOutputType);
                }
            }
        }
        
        private string GetAssetPath(ScriptableObject asset)
        {
            #if UNITY_EDITOR
            return UnityEditor.AssetDatabase.GetAssetPath(asset);
            #else
            return asset.name;
            #endif
        }
        
        #endregion
        
        #region Cleanup
        
        public void Cleanup()
        {
            if (_inventory != null)
            {
                _inventory.OnWeaponEquipped -= OnInventoryOutputEquipped;
                _inventory.OnWeaponUnequipped -= OnInventoryOutputUnequipped;
            }
            
            if (_moduleManager != null)
            {
                _moduleManager.OnGraphExecuted -= OnModuleGraphExecuted;
            }
            
            _currentWave?.Cleanup();
            
            OnOutputEquipped = null;
            OnOutputUnequipped = null;
            OnWaveUpdated = null;
        }
        
        #endregion
    }
    
    [System.Serializable]
    public class WaveOutputManagerSaveData
    {
        public int equippedOutputID;
        public string outputName;
        public WaveOutputType outputType;
        public string assetPath;
        public WaveSaveData currentWaveSaveData;
    }
}