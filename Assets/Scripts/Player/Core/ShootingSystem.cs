using UnityEngine;
using Resonance.Items;
using Resonance.Interfaces;
using Resonance.Utilities;

namespace Resonance.Player.Core
{
    /// <summary>
    /// HitScan射击系统
    /// 处理射线检测、伤害计算和视觉效果
    /// </summary>
    public class ShootingSystem
    {
        // 射击配置
        private LayerMask _targetLayerMask = -1; // 默认检测所有层
        private LayerMask _obstacleLayerMask = -1; // 阻挡层
        
        // 射击线条视觉效果
        private LineRenderer _shootingLineRenderer;
        private float _lineDisplayDuration = 0.1f;
        private bool _showShootingLine = true;
        
        // 射击统计
        private int _totalShots = 0;
        private int _hits = 0;
        
        // 事件
        public System.Action<Vector3, float> OnShoot; // 射击位置, 伤害
        public System.Action<Vector3, GameObject, float> OnHit; // 命中位置, 目标, 伤害
        public System.Action<Vector3> OnMiss; // 未命中位置

        public ShootingSystem(GameObject playerObject)
        {
            SetupLineRenderer(playerObject);
        }

        #region Public Methods

        /// <summary>
        /// 执行HitScan射击
        /// </summary>
        /// <param name="shootOrigin">射击起始位置</param>
        /// <param name="shootDirection">射击方向</param>
        /// <param name="gunData">武器数据</param>
        /// <returns>射击结果</returns>
        public ShootingResult PerformShoot(Vector3 shootOrigin, Vector3 shootDirection, GunData gunData)
        {
            if (gunData == null)
            {
                Debug.LogError("ShootingSystem: GunData is null");
                return new ShootingResult { success = false };
            }

            _totalShots++;
            
            // 执行射线检测
            RaycastHit hitInfo;
            bool hasHit = Physics.Raycast(shootOrigin, shootDirection, out hitInfo, gunData.range, _targetLayerMask);
            
            Vector3 endPoint = hasHit ? hitInfo.point : shootOrigin + shootDirection * gunData.range;
            
            // 显示射击线条
            if (_showShootingLine)
            {
                ShowShootingLine(shootOrigin, endPoint);
            }
            
            // 触发射击事件
            OnShoot?.Invoke(shootOrigin, gunData.damage);
            
            // 创建射击结果
            ShootingResult result = new ShootingResult
            {
                success = true,
                hasHit = hasHit,
                startPosition = shootOrigin,
                endPosition = endPoint,
                direction = shootDirection,
                range = gunData.range,
                damage = gunData.damage
            };

            if (hasHit)
            {
                result.hitObject = hitInfo.collider.gameObject;
                result.hitPoint = hitInfo.point;
                result.hitNormal = hitInfo.normal;
                result.hitDistance = hitInfo.distance;
                
                // 处理伤害
                ProcessHit(hitInfo, gunData.damage, shootOrigin);
                _hits++;
                
                Debug.Log($"ShootingSystem: Hit {hitInfo.collider.name} at distance {hitInfo.distance:F2}m for {gunData.damage} damage");
            }
            else
            {
                OnMiss?.Invoke(endPoint);
                Debug.Log($"ShootingSystem: Shot missed, max range {gunData.range}m reached");
            }

            return result;
        }

        /// <summary>
        /// 设置目标检测层
        /// </summary>
        /// <param name="layerMask">层遮罩</param>
        public void SetTargetLayerMask(LayerMask layerMask)
        {
            _targetLayerMask = layerMask;
        }

        /// <summary>
        /// 设置是否显示射击线条
        /// </summary>
        /// <param name="show">是否显示</param>
        public void SetShowShootingLine(bool show)
        {
            _showShootingLine = show;
        }

        /// <summary>
        /// 设置射击线条显示时长
        /// </summary>
        /// <param name="duration">显示时长（秒）</param>
        public void SetLineDisplayDuration(float duration)
        {
            _lineDisplayDuration = Mathf.Max(0.01f, duration);
        }

        /// <summary>
        /// 获取射击统计
        /// </summary>
        /// <returns>命中率</returns>
        public float GetAccuracy()
        {
            if (_totalShots == 0) return 0f;
            return (float)_hits / _totalShots;
        }

        /// <summary>
        /// 重置射击统计
        /// </summary>
        public void ResetStats()
        {
            _totalShots = 0;
            _hits = 0;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 设置射击线条渲染器
        /// </summary>
        /// <param name="playerObject">玩家对象</param>
        private void SetupLineRenderer(GameObject playerObject)
        {
            // 创建用于显示射击线条的子对象
            GameObject lineObject = new GameObject("ShootingLine");
            lineObject.transform.SetParent(playerObject.transform);
            
            _shootingLineRenderer = lineObject.AddComponent<LineRenderer>();
            _shootingLineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.red, 0.0f), new GradientColorKey(Color.red, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f),    new GradientAlphaKey(1.0f, 1.0f) }
            );
            _shootingLineRenderer.colorGradient = gradient;

            _shootingLineRenderer.startWidth = 0.02f;
            _shootingLineRenderer.endWidth = 0.01f;
            _shootingLineRenderer.positionCount = 2;
            _shootingLineRenderer.enabled = false;
            
            Debug.Log("ShootingSystem: Line renderer setup complete");
        }

        /// <summary>
        /// 显示射击线条
        /// </summary>
        /// <param name="start">起始位置</param>
        /// <param name="end">结束位置</param>
        private void ShowShootingLine(Vector3 start, Vector3 end)
        {
            if (_shootingLineRenderer == null) return;

            _shootingLineRenderer.SetPosition(0, start);
            _shootingLineRenderer.SetPosition(1, end);
            _shootingLineRenderer.enabled = true;
            
            // 创建一个简单的脚本来处理协程
            LineDisplayController controller = _shootingLineRenderer.gameObject.GetComponent<LineDisplayController>();
            if (controller == null)
            {
                controller = _shootingLineRenderer.gameObject.AddComponent<LineDisplayController>();
            }
            controller.ShowLineTemporarily(_shootingLineRenderer, _lineDisplayDuration);
        }



        /// <summary>
        /// 处理命中目标
        /// </summary>
        /// <param name="hitInfo">射线命中信息</param>
        /// <param name="damage">伤害值</param>
        /// <param name="damageSource">伤害来源</param>
        private void ProcessHit(RaycastHit hitInfo, float damage, Vector3 damageSource)
        {
            GameObject hitObject = hitInfo.collider.gameObject;
            
            // 尝试对可受伤害的对象造成伤害
            IDamageable damageable = hitObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, damageSource, "Bullet");
                OnHit?.Invoke(hitInfo.point, hitObject, damage);
                Debug.Log($"ShootingSystem: Dealt {damage} damage to {hitObject.name}");
                return;
            }

            // 尝试破坏可破坏的对象
            IDestructible destructible = hitObject.GetComponent<IDestructible>();
            if (destructible != null)
            {
                destructible.TakeDamage(damage, damageSource);
                OnHit?.Invoke(hitInfo.point, hitObject, damage);
                Debug.Log($"ShootingSystem: Dealt {damage} damage to destructible {hitObject.name}");
                return;
            }

            // 如果不是可受伤害或可破坏的对象，仍然触发命中事件（用于音效、粒子效果等）
            OnHit?.Invoke(hitInfo.point, hitObject, 0f);
            Debug.Log($"ShootingSystem: Hit non-damageable object {hitObject.name}");
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            if (_shootingLineRenderer != null)
            {
                Object.Destroy(_shootingLineRenderer.gameObject);
            }
            
            OnShoot = null;
            OnHit = null;
            OnMiss = null;
        }

        #endregion
    }

    /// <summary>
    /// 射击结果数据结构
    /// </summary>
    [System.Serializable]
    public struct ShootingResult
    {
        public bool success;        // 射击是否成功执行
        public bool hasHit;         // 是否命中目标
        public Vector3 startPosition; // 射击起始位置
        public Vector3 endPosition;   // 射击结束位置
        public Vector3 direction;     // 射击方向
        public float range;           // 射击距离
        public float damage;          // 伤害值
        
        // 命中信息（如果hasHit为true）
        public GameObject hitObject;  // 命中的对象
        public Vector3 hitPoint;      // 命中点
        public Vector3 hitNormal;     // 命中面的法线
        public float hitDistance;     // 命中距离
    }
}
