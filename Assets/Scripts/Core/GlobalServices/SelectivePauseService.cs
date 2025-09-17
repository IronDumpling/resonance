using System.Collections.Generic;
using UnityEngine;
using Resonance.Core;
using Resonance.Interfaces.Services;

namespace Resonance.Core.GlobalServices
{
    /// <summary>
    /// 选择性暂停服务实现
    /// 管理不同级别的游戏暂停状态
    /// </summary>
    public class SelectivePauseService : ISelectivePauseService
    {
        public int Priority => 25; // After InputService (20) and PlayerService (20)
        public SystemState State { get; private set; } = SystemState.Uninitialized;

        private bool _isGameplayPaused = false;
        private bool _isFullyPaused = false;
        private readonly HashSet<IPausable> _pausableComponents = new HashSet<IPausable>();

        public bool IsGameplayPaused => _isGameplayPaused;
        public bool IsFullyPaused => _isFullyPaused;

        public void Initialize()
        {
            if (State != SystemState.Uninitialized)
            {
                Debug.LogWarning("SelectivePauseService already initialized");
                return;
            }

            State = SystemState.Initializing;
            Debug.Log("SelectivePauseService: Initializing");

            State = SystemState.Running;
            Debug.Log("SelectivePauseService: Initialized successfully");
        }

        public void PauseGameplay()
        {
            if (_isGameplayPaused) 
            {
                Debug.Log("SelectivePauseService: Already paused, ignoring duplicate PauseGameplay call");
                return;
            }

            Debug.Log($"SelectivePauseService: Pausing gameplay - {_pausableComponents.Count} components registered");
            _isGameplayPaused = true;

            // 暂停所有注册的可暂停组件
            int pausedCount = 0;
            foreach (var pausable in _pausableComponents)
            {
                if (pausable != null)
                {
                    pausable.Pause();
                    pausedCount++;
                    Debug.Log($"SelectivePauseService: Paused component {pausable.GetType().Name}");
                }
            }

            Debug.Log($"SelectivePauseService: Successfully paused {pausedCount} components");

            // 不设置 Time.timeScale = 0，这样UI动画等仍然可以运行
            // 具体的暂停逻辑由各个组件自己实现
        }

        public void ResumeGameplay()
        {
            if (!_isGameplayPaused) 
            {
                Debug.Log("SelectivePauseService: Not paused, ignoring ResumeGameplay call");
                return;
            }

            Debug.Log($"SelectivePauseService: Resuming gameplay - {_pausableComponents.Count} components registered");
            _isGameplayPaused = false;

            // 恢复所有注册的可暂停组件
            int resumedCount = 0;
            foreach (var pausable in _pausableComponents)
            {
                if (pausable != null)
                {
                    pausable.Resume();
                    resumedCount++;
                    Debug.Log($"SelectivePauseService: Resumed component {pausable.GetType().Name}");
                }
            }

            Debug.Log($"SelectivePauseService: Successfully resumed {resumedCount} components");
        }

        public void PauseAll()
        {
            if (_isFullyPaused) return;

            Debug.Log("SelectivePauseService: Pausing all");
            _isFullyPaused = true;

            // 先暂停游戏逻辑
            if (!_isGameplayPaused)
            {
                PauseGameplay();
            }

            // 完全暂停时间
            Time.timeScale = 0f;
        }

        public void ResumeAll()
        {
            if (!_isFullyPaused) return;

            Debug.Log("SelectivePauseService: Resuming all");
            _isFullyPaused = false;

            // 恢复时间
            Time.timeScale = 1f;

            // 恢复游戏逻辑
            ResumeGameplay();
        }

        public void RegisterPausable(IPausable pausable)
        {
            if (pausable == null)
            {
                Debug.LogWarning("SelectivePauseService: Cannot register null pausable component");
                return;
            }

            if (_pausableComponents.Add(pausable))
            {
                Debug.Log($"SelectivePauseService: Registered pausable component {pausable.GetType().Name}");
                
                // 如果当前已经暂停，立即暂停新注册的组件
                if (_isGameplayPaused)
                {
                    pausable.Pause();
                }
            }
        }

        public void UnregisterPausable(IPausable pausable)
        {
            if (pausable == null) return;

            if (_pausableComponents.Remove(pausable))
            {
                Debug.Log($"SelectivePauseService: Unregistered pausable component {pausable.GetType().Name}");
            }
        }

        public void Shutdown()
        {
            if (State == SystemState.Shutdown)
                return;

            Debug.Log("SelectivePauseService: Shutting down");

            // 确保恢复所有状态
            if (_isFullyPaused)
            {
                ResumeAll();
            }
            else if (_isGameplayPaused)
            {
                ResumeGameplay();
            }

            _pausableComponents.Clear();
            State = SystemState.Shutdown;
        }
    }
}
