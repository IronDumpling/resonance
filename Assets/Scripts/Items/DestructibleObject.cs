using UnityEngine;
using Resonance.Interfaces;

namespace Resonance.Items
{
    /// <summary>
    /// 可破坏的环境物体，如箱子、桶、墙壁等
    /// 实现IDestructible接口
    /// </summary>
    public class DestructibleObject : MonoBehaviour, IDestructible
    {
        [Header("Durability Settings")]
        [SerializeField] private float _maxDurability = 50f;
        [SerializeField] private float _currentDurability = 50f;
        
        [Header("Destruction Settings")]
        [SerializeField] private GameObject _destroyedPrefab; // 破坏后的替换物体
        [SerializeField] private bool _dropItems = false;
        [SerializeField] private GameObject[] _dropItemPrefabs;
        [SerializeField] private int _minDropCount = 1;
        [SerializeField] private int _maxDropCount = 3;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem _hitEffect;
        [SerializeField] private ParticleSystem _destroyEffect;
        [SerializeField] private AudioClip _hitSound;
        [SerializeField] private AudioClip _destroySound;
        
        [Header("Visual Feedback")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _damageColor = Color.orange;
        [SerializeField] private float _damageFlashDuration = 0.15f;
        
        private Renderer _renderer;
        private AudioSource _audioSource;
        private bool _isDestroyed = false;
        
        // 破坏统计
        private int _timesHit = 0;
        private float _totalDamageTaken = 0f;
        
        // Properties
        public bool IsDestroyed => _isDestroyed;
        public float CurrentDurability => _currentDurability;
        public float MaxDurability => _maxDurability;

        void Start()
        {
            _currentDurability = _maxDurability;
            _renderer = GetComponent<Renderer>();
            _audioSource = GetComponent<AudioSource>();
            
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            Debug.Log($"DestructibleObject: {gameObject.name} initialized with {_maxDurability} durability");
        }

        #region IDestructible Implementation

        public void TakeDamage(float damage, Vector3 damageSource)
        {
            if (_isDestroyed || damage <= 0f) return;

            _currentDurability = Mathf.Max(0f, _currentDurability - damage);
            _timesHit++;
            _totalDamageTaken += damage;
            
            Debug.Log($"DestructibleObject: {gameObject.name} took {damage} damage. " +
                     $"Durability: {_currentDurability}/{_maxDurability}");

            // 视觉和音效反馈
            ShowHitEffect(damageSource);
            PlayHitSound();
            
            // 检查是否被摧毁
            if (_currentDurability <= 0f && !_isDestroyed)
            {
                OnDestroyed();
            }
        }

        public void OnDestroyed()
        {
            if (_isDestroyed) return;
            
            _isDestroyed = true;
            
            Debug.Log($"DestructibleObject: {gameObject.name} destroyed! Stats: " +
                     $"Times hit: {_timesHit}, Total damage: {_totalDamageTaken}");
            
            // 播放破坏效果
            ShowDestroyEffect();
            PlayDestroySound();
            
            // 掉落物品
            if (_dropItems)
            {
                DropItems();
            }
            
            // 替换为破坏后的物体
            if (_destroyedPrefab != null)
            {
                GameObject destroyed = Instantiate(_destroyedPrefab, transform.position, transform.rotation);
                destroyed.name = gameObject.name + "_Destroyed";
            }
            
            // 延迟销毁以播放音效
            Destroy(gameObject, 1f);
        }

        #endregion

        #region Private Methods

        private void ShowHitEffect(Vector3 damageSource)
        {
            // 粒子效果
            if (_hitEffect != null)
            {
                _hitEffect.transform.position = transform.position;
                _hitEffect.Play();
            }
            
            // 颜色闪烁
            if (_renderer != null)
            {
                StartCoroutine(DamageFlashCoroutine());
            }
        }

        private void ShowDestroyEffect()
        {
            if (_destroyEffect != null)
            {
                _destroyEffect.transform.position = transform.position;
                _destroyEffect.Play();
            }
        }

        private void PlayHitSound()
        {
            if (_hitSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_hitSound);
            }
        }

        private void PlayDestroySound()
        {
            if (_destroySound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_destroySound);
            }
        }

        private System.Collections.IEnumerator DamageFlashCoroutine()
        {
            Color originalColor = _renderer.material.color;
            
            // 闪烁为伤害颜色
            _renderer.material.color = _damageColor;
            yield return new WaitForSeconds(_damageFlashDuration);
            
            // 恢复原始颜色
            if (!_isDestroyed && _renderer != null)
            {
                _renderer.material.color = originalColor;
            }
        }

        private void DropItems()
        {
            if (_dropItemPrefabs == null || _dropItemPrefabs.Length == 0) return;
            
            int dropCount = Random.Range(_minDropCount, _maxDropCount + 1);
            
            for (int i = 0; i < dropCount; i++)
            {
                GameObject itemPrefab = _dropItemPrefabs[Random.Range(0, _dropItemPrefabs.Length)];
                if (itemPrefab != null)
                {
                    Vector3 dropPosition = transform.position + Random.insideUnitSphere * 1f;
                    dropPosition.y = transform.position.y; // 保持在地面水平
                    
                    GameObject droppedItem = Instantiate(itemPrefab, dropPosition, Random.rotation);
                    
                    // 添加一点随机力
                    Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddForce(Random.insideUnitSphere * 3f, ForceMode.Impulse);
                    }
                }
            }
            
            Debug.Log($"DestructibleObject: Dropped {dropCount} items");
        }

        #endregion

        #region Gizmos and Debug

        void OnDrawGizmos()
        {
            if (!_isDestroyed)
            {
                // 绘制耐久度条
                Vector3 barPosition = transform.position + Vector3.up * (transform.localScale.y + 0.5f);
                float barWidth = 1.5f;
                float barHeight = 0.15f;
                
                // 背景（深色）
                Gizmos.color = Color.black;
                Gizmos.DrawCube(barPosition, new Vector3(barWidth, barHeight, 0.1f));
                
                // 当前耐久度（蓝色）
                float durabilityPercentage = _maxDurability > 0 ? _currentDurability / _maxDurability : 0f;
                Gizmos.color = Color.cyan;
                Vector3 durabilityBarSize = new Vector3(barWidth * durabilityPercentage, barHeight, 0.1f);
                Vector3 durabilityBarPosition = barPosition + Vector3.left * (barWidth * (1f - durabilityPercentage) * 0.5f);
                Gizmos.DrawCube(durabilityBarPosition, durabilityBarSize);
                
                // 边框
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(barPosition, new Vector3(barWidth, barHeight, 0.1f));
                
                // 可破坏标识
                Gizmos.color = Color.orange;
                Gizmos.DrawWireSphere(transform.position, 0.2f);
            }
        }

        void OnDrawGizmosSelected()
        {
            // 显示破坏范围
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, transform.localScale * 1.1f);
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// 重置物体状态（用于测试）
        /// </summary>
        public void ResetObject()
        {
            _currentDurability = _maxDurability;
            _isDestroyed = false;
            _timesHit = 0;
            _totalDamageTaken = 0f;
            
            if (_renderer != null)
            {
                _renderer.material.color = _normalColor;
            }
            
            gameObject.SetActive(true);
            Debug.Log($"DestructibleObject: {gameObject.name} reset to full durability");
        }

        /// <summary>
        /// 获取物体统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public string GetStats()
        {
            return $"Durability: {_currentDurability:F1}/{_maxDurability}, Hits: {_timesHit}, Damage: {_totalDamageTaken:F1}";
        }

        /// <summary>
        /// 立即摧毁物体
        /// </summary>
        public void ForceDestroy()
        {
            _currentDurability = 0f;
            OnDestroyed();
        }

        #endregion
    }
}
