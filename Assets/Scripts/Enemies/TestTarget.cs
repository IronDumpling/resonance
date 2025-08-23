using UnityEngine;
using Resonance.Interfaces;

namespace Resonance.Enemies
{
    /// <summary>
    /// 测试用的目标，用于验证射击系统
    /// 实现IDamageable接口来接受伤害
    /// </summary>
    public class TestTarget : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _currentHealth = 100f;
        
        [Header("Visual Feedback")]
        [SerializeField] private float _damageFlashDuration = 0.2f;
        [SerializeField] private string _normalMaterialPath = "Art/Materials/Enemy_Body";
        [SerializeField] private string _damageMaterialPath = "Art/Materials/Damage_Body";
        
        [Header("Debug")]
        [SerializeField] private bool _showHealthBar = true;
        
        private Renderer _bodyRenderer;
        private Material _normalMaterial;
        private Material _damageMaterial;
        private bool _isDead = false;
        
        // 伤害统计
        private int _timesHit = 0;
        private float _totalDamageTaken = 0f;
        
        // Properties
        public bool IsAlive => !_isDead && _currentHealth > 0;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;

        void Start()
        {
            _currentHealth = _maxHealth;
            
            // 查找Body渲染器（在Visual/Body路径下）
            FindBodyRenderer();
            
            // 加载材质资源
            LoadMaterials();
            
            // 设置初始材质
            if (_bodyRenderer != null && _normalMaterial != null)
            {
                _bodyRenderer.material = _normalMaterial;
            }
            
            Debug.Log($"TestTarget: {gameObject.name} initialized with {_maxHealth} health");
        }

        /// <summary>
        /// 查找Body渲染器组件
        /// </summary>
        private void FindBodyRenderer()
        {
            // 尝试查找Visual/Body路径
            Transform visualChild = transform.Find("Visual");
            if (visualChild != null)
            {
                Transform bodyChild = visualChild.Find("Body");
                if (bodyChild != null)
                {
                    _bodyRenderer = bodyChild.GetComponent<Renderer>();
                    if (_bodyRenderer != null)
                    {
                        Debug.Log($"TestTarget: Found Body renderer at Visual/Body path");
                    }
                    else
                    {
                        Debug.LogWarning($"TestTarget: Body child found but no Renderer component on {bodyChild.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"TestTarget: Visual child found but no Body child in {gameObject.name}");
                }
            }
            else
            {
                Debug.LogWarning($"TestTarget: No Visual child found in {gameObject.name}");
            }

            // 如果没找到，尝试在当前对象上查找
            if (_bodyRenderer == null)
            {
                _bodyRenderer = GetComponent<Renderer>();
                if (_bodyRenderer != null)
                {
                    Debug.LogWarning($"TestTarget: Using renderer from root object as fallback");
                }
                else
                {
                    Debug.LogError($"TestTarget: No renderer found anywhere on {gameObject.name}!");
                }
            }
        }

        /// <summary>
        /// 从Resources加载材质资源
        /// </summary>
        private void LoadMaterials()
        {
            // 加载正常材质
            _normalMaterial = Resources.Load<Material>(_normalMaterialPath);
            if (_normalMaterial == null)
            {
                Debug.LogError($"TestTarget: Failed to load normal material from {_normalMaterialPath}");
            }
            else
            {
                Debug.Log($"TestTarget: Loaded normal material: {_normalMaterial.name}");
            }

            // 加载受伤材质
            _damageMaterial = Resources.Load<Material>(_damageMaterialPath);
            if (_damageMaterial == null)
            {
                Debug.LogError($"TestTarget: Failed to load damage material from {_damageMaterialPath}");
            }
            else
            {
                Debug.Log($"TestTarget: Loaded damage material: {_damageMaterial.name}");
            }
        }

        #region IDamageable Implementation

        public void TakeDamage(float damage, Vector3 damageSource, string damageType = "Normal")
        {
            if (_isDead || damage <= 0f) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            _timesHit++;
            _totalDamageTaken += damage;
            
            Debug.Log($"TestTarget: {gameObject.name} took {damage} {damageType} damage. " +
                     $"Health: {_currentHealth}/{_maxHealth}");

            // 视觉反馈
            ShowDamageEffect();
            
            // 检查是否死亡
            if (_currentHealth <= 0f && !_isDead)
            {
                Die();
            }
        }

        #endregion

        #region Private Methods

        private void ShowDamageEffect()
        {
            if (_bodyRenderer != null && _damageMaterial != null)
            {
                StartCoroutine(DamageFlashCoroutine());
            }
        }

        private System.Collections.IEnumerator DamageFlashCoroutine()
        {
            // 切换到受伤材质
            if (_bodyRenderer != null && _damageMaterial != null)
            {
                _bodyRenderer.material = _damageMaterial;
                Debug.Log("TestTarget: Switched to damage material");
            }
            
            yield return new WaitForSeconds(_damageFlashDuration);
            
            // 恢复正常材质
            if (_bodyRenderer != null && _normalMaterial != null && !_isDead)
            {
                _bodyRenderer.material = _normalMaterial;
                Debug.Log("TestTarget: Switched back to normal material");
            }
        }

        private void Die()
        {
            _isDead = true;
            
            Debug.Log($"TestTarget: {gameObject.name} destroyed! Stats: " +
                     $"Times hit: {_timesHit}, Total damage: {_totalDamageTaken}");
            
            // 死亡效果 - 保持受伤材质或者你可以创建一个死亡材质
            if (_bodyRenderer != null && _damageMaterial != null)
            {
                _bodyRenderer.material = _damageMaterial;
                Debug.Log("TestTarget: Applied death visual effect");
            }
            
            // 可以添加死亡动画、音效等
            // 这里我们在3秒后销毁对象
            Destroy(gameObject, 3f);
        }

        #endregion

        #region Gizmos and Debug

        void OnDrawGizmos()
        {
            if (_showHealthBar && Application.isPlaying)
            {
                // 绘制血量条
                Vector3 barPosition = transform.position + Vector3.up * 2f;
                float barWidth = 2f;
                float barHeight = 0.2f;
                
                // 背景（红色）
                Gizmos.color = Color.red;
                Gizmos.DrawCube(barPosition, new Vector3(barWidth, barHeight, 0.1f));
                
                // 当前血量（绿色）
                float healthPercentage = _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;
                Gizmos.color = Color.green;
                Vector3 healthBarSize = new Vector3(barWidth * healthPercentage, barHeight, 0.1f);
                Vector3 healthBarPosition = barPosition + Vector3.left * (barWidth * (1f - healthPercentage) * 0.5f);
                Gizmos.DrawCube(healthBarPosition, healthBarSize);
                
                // 边框
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(barPosition, new Vector3(barWidth, barHeight, 0.1f));
            }
            
            // 显示可攻击标识
            if (!_isDead)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.3f);
            }
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// 重置目标状态（用于测试）
        /// </summary>
        public void ResetTarget()
        {
            _currentHealth = _maxHealth;
            _isDead = false;
            _timesHit = 0;
            _totalDamageTaken = 0f;
            
            // 恢复正常材质
            if (_bodyRenderer != null && _normalMaterial != null)
            {
                _bodyRenderer.material = _normalMaterial;
            }
            
            Debug.Log($"TestTarget: {gameObject.name} reset to full health");
        }

        /// <summary>
        /// 获取目标统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public string GetStats()
        {
            return $"Health: {_currentHealth:F1}/{_maxHealth}, Hits: {_timesHit}, Damage: {_totalDamageTaken:F1}";
        }

        /// <summary>
        /// 手动切换到受伤材质（用于测试）
        /// </summary>
        public void TestDamageMaterial()
        {
            if (_bodyRenderer != null && _damageMaterial != null)
            {
                _bodyRenderer.material = _damageMaterial;
                Debug.Log("TestTarget: Manually switched to damage material");
            }
        }

        /// <summary>
        /// 手动切换到正常材质（用于测试）
        /// </summary>
        public void TestNormalMaterial()
        {
            if (_bodyRenderer != null && _normalMaterial != null)
            {
                _bodyRenderer.material = _normalMaterial;
                Debug.Log("TestTarget: Manually switched to normal material");
            }
        }

        /// <summary>
        /// 获取当前使用的材质信息
        /// </summary>
        /// <returns>材质信息</returns>
        public string GetMaterialInfo()
        {
            if (_bodyRenderer == null) return "No renderer found";
            
            Material currentMat = _bodyRenderer.material;
            string currentName = currentMat != null ? currentMat.name : "None";
            
            return $"Current: {currentName}, Normal: {(_normalMaterial != null ? _normalMaterial.name : "None")}, Damage: {(_damageMaterial != null ? _damageMaterial.name : "None")}";
        }

        #endregion
    }
}
