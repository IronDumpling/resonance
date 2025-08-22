using UnityEngine;

namespace Resonance.Items
{
    /// <summary>
    /// 场景中可交互的Gun物体
    /// 玩家可以拾取并装备到武器管理器中
    /// </summary>
    public class GunMonoBehaviour : MonoBehaviour
    {
        [Header("Gun Configuration")]
        [SerializeField] private GunData _gunData;
        
        [Header("Interaction")]
        [SerializeField] private float _interactionRange = 2f;
        [SerializeField] private LayerMask _playerLayerMask = 1;

        // 是否已被拾取
        private bool _isPickedUp = false;

        // Properties
        public GunData GunData => _gunData;
        public bool IsPickedUp => _isPickedUp;

        void Start()
        {
            // 确保有默认的Gun数据
            if (_gunData == null)
            {
                CreateDefaultGunData();
            }

            Debug.Log($"GunMonoBehaviour: {_gunData.weaponName} ready for pickup");
        }

        /// <summary>
        /// 创建默认的Gun数据（用于测试）
        /// </summary>
        private void CreateDefaultGunData()
        {
            _gunData = new GunData
            {
                weaponName = "Test Gun",
                weaponDescription = "A test weapon",
                maxAmmo = 8,
                currentAmmo = 8,
                ammoType = "TypeA",
                damage = 25f,
                range = 100f,
                fireRate = 1f,
                gridWidth = 2,
                gridHeight = 3
            };
        }

        /// <summary>
        /// 检查玩家是否在交互范围内
        /// </summary>
        /// <param name="playerPosition">玩家位置</param>
        /// <returns>是否可以交互</returns>
        public bool CanInteract(Vector3 playerPosition)
        {
            if (_isPickedUp) return false;
            
            float distance = Vector3.Distance(transform.position, playerPosition);
            return distance <= _interactionRange;
        }

        /// <summary>
        /// 拾取武器
        /// </summary>
        /// <returns>武器数据的副本</returns>
        public GunData PickupWeapon()
        {
            if (_isPickedUp)
            {
                Debug.LogWarning("GunMonoBehaviour: Weapon already picked up");
                return null;
            }

            _isPickedUp = true;
            
            // 创建数据副本
            GunData gunCopy = _gunData.Clone();
            
            // 隐藏物体（或销毁，根据需要）
            gameObject.SetActive(false);
            
            Debug.Log($"GunMonoBehaviour: {_gunData.weaponName} picked up");
            
            return gunCopy;
        }

        /// <summary>
        /// 重置武器状态（用于重新生成或测试）
        /// </summary>
        public void ResetWeapon()
        {
            _isPickedUp = false;
            gameObject.SetActive(true);
            Debug.Log($"GunMonoBehaviour: {_gunData.weaponName} reset");
        }

        void OnDrawGizmosSelected()
        {
            // 绘制交互范围
            Gizmos.color = _isPickedUp ? Color.gray : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _interactionRange);
        }

        void OnDrawGizmos()
        {
            // 始终显示一个小的交互指示
            if (!_isPickedUp)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.2f);
            }
        }
    }
}
