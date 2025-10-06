using UnityEngine;
using UnityEngine.UI;

namespace Resonance.Utilities
{
    /// <summary>
    /// 单个网格槽位
    /// 负责显示槽位状态和物品信息
    /// </summary>
    public class GridSlot : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private GameObject _highlightObject;
        
        [Header("Visual Settings")]
        [SerializeField] private Color _normalColor = new Color(1f, 1f, 1f, 0.1f);
        [SerializeField] private Color _highlightColor = new Color(1f, 1f, 0f, 0.3f);
        [SerializeField] private Color _invalidColor = new Color(1f, 0f, 0f, 0.3f);
        
        // 状态
        private Vector2Int _gridPosition;
        private GridItem _currentItem; // Reference only, visual is handled by GridItemVisual
        private SlotState _currentState = SlotState.Normal;
        private bool _isInitialized = false;
        
        public Vector2Int GridPosition => _gridPosition;
        public GridItem CurrentItem => _currentItem;
        public SlotState CurrentState => _currentState;
        public bool IsOccupied => _currentItem != null;
        
        // 事件
        public System.Action<GridSlot> OnSlotClicked;
        public System.Action<GridSlot> OnSlotHovered;
        public System.Action<GridSlot> OnSlotUnhovered;
        
        public enum SlotState
        {
            Normal,      // Default state
            Highlighted, // Hover or preview state
            Invalid      // Cannot place here
        }
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>();
            
            if (_highlightObject == null)
                _highlightObject = transform.Find("Highlight")?.gameObject;
            
            _isInitialized = true;
        }
        
        /// <summary>
        /// 初始化槽位
        /// </summary>
        /// <param name="position">网格位置</param>
        public void Initialize(Vector2Int position)
        {
            _gridPosition = position;
            SetState(SlotState.Normal);
            UpdateVisuals();
        }
        
        /// <summary>
        /// 设置槽位状态
        /// </summary>
        /// <param name="state">新状态</param>
        public void SetState(SlotState state)
        {
            if (_currentState == state) return;
            
            _currentState = state;
            UpdateVisuals();
        }
        
        /// <summary>
        /// 设置当前物品（仅用于引用，不显示视觉）
        /// </summary>
        /// <param name="item">物品</param>
        public void SetItem(GridItem item)
        {
            _currentItem = item;
        }
        
        /// <summary>
        /// 更新视觉效果
        /// </summary>
        private void UpdateVisuals()
        {
            if (!_isInitialized) return;
            
            // 更新背景颜色
            Color bgColor = GetStateColor();
            if (_backgroundImage != null)
            {
                _backgroundImage.color = bgColor;
            }
            
            // 更新高亮显示
            if (_highlightObject != null)
            {
                _highlightObject.SetActive(_currentState == SlotState.Highlighted);
            }
        }
        
        /// <summary>
        /// 获取状态对应的颜色
        /// </summary>
        /// <returns>颜色</returns>
        private Color GetStateColor()
        {
            switch (_currentState)
            {
                case SlotState.Highlighted:
                    return _highlightColor;
                case SlotState.Invalid:
                    return _invalidColor;
                case SlotState.Normal:
                default:
                    return _normalColor;
            }
        }
        
        /// <summary>
        /// 高亮槽位
        /// </summary>
        /// <param name="highlight">是否高亮</param>
        public void SetHighlight(bool highlight)
        {
            if (highlight)
            {
                SetState(SlotState.Highlighted);
            }
            else if (_currentState == SlotState.Highlighted)
            {
                SetState(SlotState.Normal);
            }
        }
        
        /// <summary>
        /// 标记为无效位置
        /// </summary>
        /// <param name="invalid">是否无效</param>
        public void SetInvalid(bool invalid)
        {
            if (invalid)
            {
                SetState(SlotState.Invalid);
            }
            else if (_currentState == SlotState.Invalid)
            {
                SetState(SlotState.Normal);
            }
        }
        
        /// <summary>
        /// 清空槽位
        /// </summary>
        public void Clear()
        {
            _currentItem = null;
            SetState(SlotState.Normal);
        }
        
        /// <summary>
        /// 检查是否包含指定位置
        /// </summary>
        /// <param name="position">要检查的位置</param>
        /// <returns>是否包含</returns>
        public bool ContainsPosition(Vector2Int position)
        {
            return position == _gridPosition;
        }
        
        // UI事件处理
        public void OnPointerClick()
        {
            OnSlotClicked?.Invoke(this);
        }
        
        public void OnPointerEnter()
        {
            OnSlotHovered?.Invoke(this);
        }
        
        public void OnPointerExit()
        {
            OnSlotUnhovered?.Invoke(this);
        }
    }
}
