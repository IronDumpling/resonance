using UnityEngine;

namespace Resonance.Enemies
{
    /// <summary>
    /// 敌人检测触发器类型
    /// </summary>
    public enum TriggerType
    {
        Detection,  // 检测范围
        Attack      // 攻击范围
    }

    /// <summary>
    /// 敌人检测触发器组件，用于标识和处理不同类型的触发器事件
    /// </summary>
    public class EnemyDetectionTrigger : MonoBehaviour
    {
        private EnemyMonoBehaviour _enemyMono;
        private TriggerType _triggerType;
        private bool _isInitialized = false;

        /// <summary>
        /// 初始化触发器
        /// </summary>
        /// <param name="enemyMono">敌人MonoBehaviour引用</param>
        /// <param name="triggerType">触发器类型</param>
        public void Initialize(EnemyMonoBehaviour enemyMono, TriggerType triggerType)
        {
            _enemyMono = enemyMono;
            _triggerType = triggerType;
            _isInitialized = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!_isInitialized || _enemyMono == null) return;

            // 只检测玩家
            if (other.CompareTag("Player"))
            {
                _enemyMono.HandleTriggerEnter(_triggerType, other);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (!_isInitialized || _enemyMono == null) return;

            // 只检测玩家
            if (other.CompareTag("Player"))
            {
                _enemyMono.HandleTriggerExit(_triggerType, other);
            }
        }

        void OnTriggerStay(Collider other)
        {
            if (!_isInitialized || _enemyMono == null) return;

            // 只检测玩家
            if (other.CompareTag("Player"))
            {
                _enemyMono.HandleTriggerStay(_triggerType, other);
            }
        }
    }
}
