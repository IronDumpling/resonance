using Resonance.Gameplay.Items.Core;

namespace Resonance.Shared.Interfaces.Objects
{
    /// <summary>
    /// 可显示信息的对象接口
    /// 实现此接口的物品可以在InfoPanel中显示详细信息
    /// </summary>
    public interface IInfoable
    {
        /// <summary>
        /// 获取要在InfoPanel中显示的信息数据
        /// </summary>
        /// <returns>信息数据</returns>
        InfoData GetInfoData();

        /// <summary>
        /// 检查是否有有效的信息可以显示
        /// </summary>
        /// <returns>是否可以显示信息</returns>
        bool HasValidInfo();
    }
}
