using UnityEngine;
using Resonance.Interfaces;
using UnityEngine.UI;

namespace Resonance.Items
{
    [CreateAssetMenu(fileName = "New Info Data", menuName = "Resonance/Items/Info Data", order = 3)]
    public class InfoDataAsset : ScriptableObject
    {
        public string infoName;
        [TextArea(5, 10)]
        public string infoContent;
        public Sprite infoImage;
    }
}