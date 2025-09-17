using UnityEngine;

namespace Resonance.Items.Core
{
    /// <summary>
    /// 标准化的信息数据结构
    /// 用于所有需要在InfoPanel中显示信息的物品
    /// </summary>
    [System.Serializable]
    public struct InfoData
    {
        [Header("Display Information")]
        public string name;
        
        [TextArea(2, 6)]
        public string content;
        
        public Sprite image;

        /// <summary>
        /// 构造函数
        /// </summary>
        public InfoData(string name, string content, Sprite image = null)
        {
            this.name = name;
            this.content = content;
            this.image = image;
        }

        /// <summary>
        /// 验证信息数据是否有效
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(content);
        }

        /// <summary>
        /// 创建一个空的信息数据
        /// </summary>
        public static InfoData Empty => new InfoData("", "", null);

        /// <summary>
        /// 检查是否为空
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(name) && string.IsNullOrEmpty(content);
    }
}
