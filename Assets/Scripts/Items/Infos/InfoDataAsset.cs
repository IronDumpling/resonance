using UnityEngine;
using Resonance.Interfaces;
using Resonance.Interfaces.Objects;
using Resonance.Items.Core;

namespace Resonance.Items
{
    [CreateAssetMenu(fileName = "New Info Data", menuName = "Resonance/Items/Info Data", order = 3)]
    public class InfoDataAsset : ScriptableObject, IInfoable
    {
        [Header("Info Display")]
        [SerializeField] private InfoData _infoData;

        /// <summary>
        /// 获取要在InfoPanel中显示的信息数据
        /// </summary>
        public InfoData GetInfoData()
        {
            return _infoData;
        }

        /// <summary>
        /// 检查是否有有效的信息可以显示
        /// </summary>
        public bool HasValidInfo()
        {
            return _infoData.IsValid();
        }

        public string infoName => _infoData.name;
        public string infoContent => _infoData.content;
        public Sprite infoImage => _infoData.image;
    }
}