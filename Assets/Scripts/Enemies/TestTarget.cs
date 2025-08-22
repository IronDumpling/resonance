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
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _damageColor = Color.red;
        [SerializeField] private float _damageFlashDuration = 0.2f;
        
        [Header("Debug")]
        [SerializeField] private bool _showHealthBar = true;
        
        private Renderer _renderer;
        private Material _originalMaterial;
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
            _renderer = GetComponent<Renderer>();
            
            if (_renderer != null)
            {
                _originalMaterial = _renderer.material;
            }
            
            Debug.Log($"TestTarget: {gameObject.name} initialized with {_maxHealth} health");
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
            if (_renderer != null)
            {
                StartCoroutine(DamageFlashCoroutine());
            }
        }

        private System.Collections.IEnumerator DamageFlashCoroutine()
        {
            // 改变颜色为受伤颜色
            if (_renderer != null)
            {
                _renderer.material.color = _damageColor;
            }
            
            yield return new WaitForSeconds(_damageFlashDuration);
            
            // 恢复原始颜色
            if (_renderer != null && !_isDead)
            {
                _renderer.material.color = _normalColor;
            }
        }

        private void Die()
        {
            _isDead = true;
            
            Debug.Log($"TestTarget: {gameObject.name} destroyed! Stats: " +
                     $"Times hit: {_timesHit}, Total damage: {_totalDamageTaken}");
            
            // 死亡效果
            if (_renderer != null)
            {
                _renderer.material.color = Color.gray;
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

        void OnDrawGizmosSelected()
        {
            // 显示详细信息
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, transform.localScale);
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
            
            if (_renderer != null)
            {
                _renderer.material.color = _normalColor;
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

        #endregion
    }
}
