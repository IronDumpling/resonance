// Scripts/Gameplay/Items/WaveOutputs/WaveOutputDataAsset.cs
using UnityEngine;
using Resonance.Shared.Types;
using Resonance.Shared.Interfaces.Objects;
using Resonance.Systems.Waves;

namespace Resonance.Gameplay.Items.Core
{
    /// <summary>
    /// Base ScriptableObject for all wave output devices
    /// Contains common properties for WaveGun, CrystalCore, WaveDiffuser
    /// </summary>
    [CreateAssetMenu(fileName = "New Wave Output", menuName = "Resonance/Items/Wave Output")]
    public class WaveOutputDataAsset : ScriptableObject, IInfoable
    {
        [Header("Basic Info")]
        public string outputName = "Wave Output";
        public WaveOutputType outputType = WaveOutputType.WaveGun;
        
        [TextArea(2, 4)]
        public string description = "";

        [Header("Grid Properties")]
        public int gridWidth = 2;
        public int gridHeight = 3;
        public Sprite outputIcon;
        public GameObject itemPrefab;

        [Header("Wave Processing")]
        public float energyCostPerUse = 10f;
        public float cooldownTime = 0.5f;

        [Header("Wave Output Properties")]
        public bool allowsCustomWaves = true;
        public WaveConfig defaultWaveConfig;

        /// <summary>
        /// Create a runtime copy of this asset
        /// </summary>
        public virtual WaveOutputDataAsset CreateRuntimeCopy()
        {
            return Instantiate(this);
        }

        /// <summary>
        /// Validate configuration
        /// </summary>
        public virtual bool Validate()
        {
            if (string.IsNullOrEmpty(outputName))
            {
                Debug.LogError("WaveOutputDataAsset: outputName is empty");
                return false;
            }

            if (gridWidth <= 0 || gridHeight <= 0)
            {
                Debug.LogError("WaveOutputDataAsset: Invalid grid size");
                return false;
            }

            return true;
        }

        #region IInfoable Implementation

        /// <summary>
        /// Get information data to display in InfoPanel
        /// </summary>
        public InfoData GetInfoData()
        {
            return new InfoData(
                name: outputName,
                content: description,
                image: outputIcon
            );
        }

        /// <summary>
        /// Check if there is valid information to display
        /// </summary>
        public bool HasValidInfo()
        {
            var info = GetInfoData();
            return info.IsValid();
        }

        #endregion
    }
}