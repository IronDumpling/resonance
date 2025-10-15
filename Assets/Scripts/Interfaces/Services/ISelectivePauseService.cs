using UnityEngine;

namespace Resonance.Interfaces.Services
{
    /// <summary>
    /// 选择性暂停服务接口
    /// 支持不同级别的游戏暂停, 允许某些系统在暂停时继续运行
    /// </summary>
    public interface ISelectivePauseService : IGameService
    {
        /// <summary>
        /// 是否处于游戏逻辑暂停状态(玩家、敌人、场景逻辑暂停, 但UI可交互)
        /// </summary>
        bool IsGameplayPaused { get; }
        
        /// <summary>
        /// 是否处于完全暂停状态(所有内容暂停)
        /// </summary>
        bool IsFullyPaused { get; }

        /// <summary>
        /// 暂停游戏逻辑(玩家移动、敌人行为、场景交互等), 但保持UI交互
        /// 适用于：阅读信息、查看背包、解密等场景
        /// </summary>
        void PauseGameplay();

        /// <summary>
        /// 恢复游戏逻辑
        /// </summary>
        void ResumeGameplay();

        /// <summary>
        /// 完全暂停游戏(包括UI, 除了暂停菜单)
        /// 适用于：暂停菜单
        /// </summary>
        void PauseAll();

        /// <summary>
        /// 完全恢复游戏
        /// </summary>
        void ResumeAll();

        /// <summary>
        /// 注册可暂停的组件
        /// </summary>
        /// <param name="pausable">可暂停的组件</param>
        void RegisterPausable(IPausable pausable);

        /// <summary>
        /// 注销可暂停的组件
        /// </summary>
        /// <param name="pausable">可暂停的组件</param>
        void UnregisterPausable(IPausable pausable);
    }

    /// <summary>
    /// 可暂停组件接口
    /// 实现此接口的组件可以响应选择性暂停
    /// </summary>
    public interface IPausable
    {
        /// <summary>
        /// 暂停组件(游戏逻辑暂停时调用)
        /// </summary>
        void Pause();

        /// <summary>
        /// 恢复组件(游戏逻辑恢复时调用)
        /// </summary>
        void Resume();

        /// <summary>
        /// 组件是否当前处于暂停状态
        /// </summary>
        bool IsPaused { get; }
    }
}
