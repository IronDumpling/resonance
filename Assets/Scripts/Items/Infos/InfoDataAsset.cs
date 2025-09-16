using UnityEngine;
using Resonance.Interfaces;

namespace Resonance.Items
{
    [CreateAssetMenu(fileName = "New Info Data", menuName = "Resonance/Items/Info Data", order = 3)]
    public class InfoDataAsset : ScriptableObject
    {
        public string infoName;
        public string infoContent;
    }
}