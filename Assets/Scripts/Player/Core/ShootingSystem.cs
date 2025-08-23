using UnityEngine;
using Resonance.Items;
using Resonance.Interfaces;
using Resonance.Utilities;

namespace Resonance.Player.Core
{
    /// <summary>
    /// HitScan射击系统
    /// 实现两阶段射击：
    /// 1. 鼠标射线 → 目标点（Ground/Enemy/Objects）
    /// 2. 玩家位置 → 目标点 → 实际命中点
    /// </summary>
    public class ShootingSystem
    {
        // 射击配置
        private LayerMask _targetLayerMask = -1; // 射击目标检测层
        private LayerMask _mouseRaycastLayerMask = -1; // 鼠标射线检测层（Ground, Enemy, Objects）
        
        // 相机引用
        private Camera _mainCamera;
        private Transform _playerTransform;
        
        // 射击线条视觉效果
        private LineRenderer _shootingLineRenderer;  // 射击瞬间的闪烁线条
        private LineRenderer _aimingLineRenderer;    // 瞄准时的持续线条
        private float _lineDisplayDuration = 0.1f;
        private bool _showShootingLine = true;
        private bool _showAimingLine = true;
        
        // 射击统计
        private int _totalShots = 0;
        private int _hits = 0;
        
        // 事件
        public System.Action<Vector3, float> OnShoot; // 射击位置, 伤害
        public System.Action<Vector3, GameObject, float> OnHit; // 命中位置, 目标, 伤害
        public System.Action<Vector3> OnMiss; // 未命中位置

        public ShootingSystem(GameObject playerObject)
        {
            _playerTransform = playerObject.transform;
            SetupCamera();
            SetupLineRenderers(playerObject);
            
            // 设置默认层级
            SetDefaultLayerMasks();
        }

        /// <summary>
        /// 设置默认的层级遮罩
        /// </summary>
        private void SetDefaultLayerMasks()
        {
            // 鼠标射线检测层：Ground (Default), Enemy, Environment等
            _mouseRaycastLayerMask = (1 << 0) | (1 << 6) | (1 << 7); // Default, Enemy, Environment
            
            // 射击目标检测层：Enemy, Environment, Destructible等
            _targetLayerMask = (1 << 6) | (1 << 7); // Enemy, Environment
            
            Debug.Log($"ShootingSystem: Set default layer masks - Mouse: {_mouseRaycastLayerMask}, Target: {_targetLayerMask}");
        }

        #region Public Methods

        /// <summary>
        /// 执行基于鼠标的两阶段射击
        /// 阶段1：鼠标射线 → 获取目标点
        /// 阶段2：玩家 → 目标点 → 实际命中点
        /// </summary>
        /// <param name="shootOrigin">射击起始位置</param>
        /// <param name="gunData">武器数据</param>
        /// <returns>射击结果</returns>
        public ShootingResult PerformMouseBasedShoot(Vector3 shootOrigin, GunDataAsset gunData)
        {
            if (gunData == null)
            {
                Debug.LogError("ShootingSystem: GunData is null");
                return new ShootingResult { success = false };
            }

            if (_mainCamera == null)
            {
                Debug.LogError("ShootingSystem: No camera found for mouse raycast");
                return new ShootingResult { success = false };
            }

            _totalShots++;

            // 阶段1：获取鼠标射线的目标点
            Vector3 targetPoint = GetMouseTargetPoint();
            
            // 阶段2：从玩家向目标点射击
            Vector3 shootDirection = (targetPoint - shootOrigin).normalized;
            
            // 执行射线检测
            RaycastHit hitInfo;
            bool hasHit = Physics.Raycast(shootOrigin, shootDirection, out hitInfo, gunData.range, _targetLayerMask);
            
            Vector3 endPoint = hasHit ? hitInfo.point : targetPoint;
            
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
                damage = gunData.damage,
                mouseTargetPoint = targetPoint
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
                Debug.Log($"ShootingSystem: Shot missed, aimed at {targetPoint}");
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
        /// 设置鼠标射线检测层
        /// </summary>
        /// <param name="layerMask">层遮罩</param>
        public void SetMouseRaycastLayerMask(LayerMask layerMask)
        {
            _mouseRaycastLayerMask = layerMask;
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
        /// 设置是否显示瞄准线条
        /// </summary>
        /// <param name="show">是否显示</param>
        public void SetShowAimingLine(bool show)
        {
            _showAimingLine = show;
            if (!show && _aimingLineRenderer != null)
            {
                _aimingLineRenderer.enabled = false;
            }
        }

        /// <summary>
        /// 更新瞄准线显示（在瞄准状态下调用）
        /// </summary>
        /// <param name="shootOrigin">射击起始位置</param>
        public void UpdateAimingLine(Vector3 shootOrigin)
        {
            if (!_showAimingLine || _aimingLineRenderer == null) return;

            // 获取鼠标目标点
            Vector3 targetPoint = GetMouseTargetPoint();
            
            // 显示瞄准线
            ShowAimingLine(shootOrigin, targetPoint);
        }

        /// <summary>
        /// 隐藏瞄准线
        /// </summary>
        public void HideAimingLine()
        {
            if (_aimingLineRenderer != null)
            {
                _aimingLineRenderer.enabled = false;
            }
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
        /// 设置相机引用
        /// </summary>
        private void SetupCamera()
        {
            // 首先尝试找到CameraManager的主相机
            var cameraManager = Object.FindAnyObjectByType<Resonance.Cameras.CameraManager>();
            if (cameraManager != null && cameraManager.Brain != null)
            {
                _mainCamera = cameraManager.Brain.OutputCamera;
                Debug.Log("ShootingSystem: Found camera from CameraManager");
            }
            
            // 备用方案：寻找Main Camera
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera != null)
                {
                    Debug.Log("ShootingSystem: Using Camera.main");
                }
            }
            
            // 最后备用方案：寻找任何相机
            if (_mainCamera == null)
            {
                _mainCamera = Object.FindAnyObjectByType<Camera>();
                if (_mainCamera != null)
                {
                    Debug.Log("ShootingSystem: Using first found camera");
                }
            }
            
            if (_mainCamera == null)
            {
                Debug.LogError("ShootingSystem: No camera found! Mouse-based shooting will not work.");
            }
        }

        /// <summary>
        /// 获取鼠标指向的目标点
        /// 阶段1：鼠标射线 → Ground/Enemy/Objects
        /// 如果没有命中，使用平面交点作为备用
        /// </summary>
        /// <returns>目标点世界坐标</returns>
        private Vector3 GetMouseTargetPoint()
        {
            if (_mainCamera == null)
            {
                return _playerTransform.position + _playerTransform.forward * 10f;
            }

            // 获取鼠标位置
            Vector2 mousePosition = UnityEngine.InputSystem.Mouse.current?.position.ReadValue() ?? Vector2.zero;
            
            if (mousePosition == Vector2.zero)
            {
                Debug.LogWarning("ShootingSystem: Mouse position is zero");
                return _playerTransform.position + _playerTransform.forward * 10f;
            }

            // 创建从相机通过鼠标的射线
            Ray mouseRay = _mainCamera.ScreenPointToRay(mousePosition);
            
            // 阶段1：尝试射线检测 Ground/Enemy/Objects
            RaycastHit hitInfo;
            if (Physics.Raycast(mouseRay, out hitInfo, Mathf.Infinity, _mouseRaycastLayerMask))
            {
                Debug.Log($"ShootingSystem: Mouse hit {hitInfo.collider.name} at {hitInfo.point}");
                return hitInfo.point;
            }
            
            // 备用方案：使用平面交点
            Vector3 fallbackPoint = IntersectPlane(mouseRay, _playerTransform.position.y);
            Debug.Log($"ShootingSystem: Using plane intersection at {fallbackPoint}");
            return fallbackPoint;
        }

        /// <summary>
        /// 计算射线与指定高度平面的交点
        /// </summary>
        /// <param name="ray">射线</param>
        /// <param name="y">平面高度</param>
        /// <returns>交点世界坐标</returns>
        private Vector3 IntersectPlane(Ray ray, float y)
        {
            Plane plane = new Plane(Vector3.up, new Vector3(0f, y, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }
            
            // 实在没打到就保持朝向
            return _playerTransform.position + _playerTransform.forward * 10f;
        }

        /// <summary>
        /// 设置射击线条渲染器
        /// </summary>
        /// <param name="playerObject">玩家对象</param>
        private void SetupLineRenderers(GameObject playerObject)
        {
            // 创建射击线条（红色，闪烁）
            GameObject shootingLineObject = new GameObject("ShootingLine");
            shootingLineObject.transform.SetParent(playerObject.transform);
            
            _shootingLineRenderer = shootingLineObject.AddComponent<LineRenderer>();
            _shootingLineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            Gradient shootingGradient = new Gradient();
            shootingGradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.red, 0.0f), new GradientColorKey(Color.red, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
            );
            _shootingLineRenderer.colorGradient = shootingGradient;
            _shootingLineRenderer.startWidth = 0.03f;
            _shootingLineRenderer.endWidth = 0.02f;
            _shootingLineRenderer.positionCount = 2;
            _shootingLineRenderer.enabled = false;
            
            // 创建瞄准线条（绿色，持续）
            GameObject aimingLineObject = new GameObject("AimingLine");
            aimingLineObject.transform.SetParent(playerObject.transform);
            
            _aimingLineRenderer = aimingLineObject.AddComponent<LineRenderer>();
            _aimingLineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            Gradient aimingGradient = new Gradient();
            aimingGradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.yellow, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.7f, 0.0f), new GradientAlphaKey(0.5f, 1.0f) }
            );
            _aimingLineRenderer.colorGradient = aimingGradient;
            _aimingLineRenderer.startWidth = 0.015f;
            _aimingLineRenderer.endWidth = 0.01f;
            _aimingLineRenderer.positionCount = 2;
            _aimingLineRenderer.enabled = false;
            
            Debug.Log("ShootingSystem: Shooting and aiming line renderers setup complete");
        }

        /// <summary>
        /// 显示瞄准线条
        /// </summary>
        /// <param name="start">起始位置</param>
        /// <param name="end">结束位置</param>
        private void ShowAimingLine(Vector3 start, Vector3 end)
        {
            if (_aimingLineRenderer == null) return;

            _aimingLineRenderer.SetPosition(0, start);
            _aimingLineRenderer.SetPosition(1, end);
            _aimingLineRenderer.enabled = true;
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
            
            if (_aimingLineRenderer != null)
            {
                Object.Destroy(_aimingLineRenderer.gameObject);
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
        public Vector3 mouseTargetPoint; // 鼠标指向的目标点（阶段1结果）
        
        // 命中信息（如果hasHit为true）
        public GameObject hitObject;  // 命中的对象
        public Vector3 hitPoint;      // 命中点
        public Vector3 hitNormal;     // 命中面的法线
        public float hitDistance;     // 命中距离
    }
}
