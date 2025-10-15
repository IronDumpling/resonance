using UnityEngine;
using Resonance.Interfaces;
using Resonance.Interfaces.Objects;
using Resonance.Items.Core;

namespace Resonance.Items
{
    [CreateAssetMenu(fileName = "New Healant Data", menuName = "Resonance/Items/Healant Data", order = 3)]
    public class HealantDataAsset : ScriptableObject, IInfoable
    {
        [Header("Basic Info")]
        public string healantName = "Basic Healant";
        [TextArea(2, 4)]
        public string healantDescription = "A basic healant";
        public Sprite healantIcon;

        public InfoData GetInfoData()
        {
            return new InfoData(
                name: healantName,
                content: healantDescription,
                image: healantIcon
            );
        }

        public bool HasValidInfo()
        {
            return GetInfoData().IsValid();
        }
    }
}