using UnityEngine;
using System.Collections.Generic;

namespace Resonance.Shared.Types
{
    [System.Serializable]
    public static class LayerDict
    {
        public static readonly Dictionary<string, LayerMask> Layers = new Dictionary<string, LayerMask>
        {
            { "Default", 1 << 0 },
            { "Player", 1 << 3 },
            { "Water", 1 << 4 },
            { "UI", 1 << 5 },
            { "Environment", 1 << 6 },
            { "Interactable", 1 << 7 },
            { "Enemy", 1 << 8 }
        };

        public static LayerMask GetLayer(string layerName)
        {
            if (Layers.TryGetValue(layerName, out LayerMask layer))
            {
                return layer;
            }

            Debug.LogWarning($"LayerDict: Layer {layerName} not found");
            return Layers["Default"];
        }
    }
}