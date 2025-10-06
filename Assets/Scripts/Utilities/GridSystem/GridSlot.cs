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
        [SerializeField] private Image _itemImage;
        [SerializeField] private GameObject _highlightObject;
        [SerializeField] private GameObject _occupiedIndicator;
        
        [Header("Visual Settings")]
        [SerializeField] private Color _emptyColor = Color.white;
        [SerializeField] private Color _occupiedColor = Color.gray;
        [SerializeField] private Color _highlightColor = Color.yellow;
        [SerializeField] private Color _selectedColor = Color.blue;
        [SerializeField] private Color _invalidColor = Color.red;
        
        // 状态
        private Vector2Int _gridPosition;
        private GridItem _currentItem;
        private SlotState _currentState = SlotState.Empty;
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
            Empty,
            Occupied,
            Highlighted,
            Selected,
            Invalid
        }
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>();
            
            if (_itemImage == null)
                _itemImage = transform.Find("ItemImage")?.GetComponent<Image>();
            
            if (_highlightObject == null)
                _highlightObject = transform.Find("Highlight")?.gameObject;
            
            if (_occupiedIndicator == null)
                _occupiedIndicator = transform.Find("OccupiedIndicator")?.gameObject;
            
            _isInitialized = true;
        }
        
        /// <summary>
        /// 初始化槽位
        /// </summary>
        /// <param name="position">网格位置</param>
        public void Initialize(Vector2Int position)
        {
            _gridPosition = position;
            SetState(SlotState.Empty);
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
        /// 设置当前物品
        /// </summary>
        /// <param name="item">物品</param>
        public void SetItem(GridItem item)
        {
            _currentItem = item;
            SetState(item != null ? SlotState.Occupied : SlotState.Empty);
            UpdateVisuals();
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
            
            // 更新物品图标
            if (_itemImage != null)
            {
                if (_currentItem != null && _currentItem.itemIcon != null)
                {
                    _itemImage.sprite = _currentItem.itemIcon;
                    _itemImage.color = _currentItem.itemColor;
                    _itemImage.gameObject.SetActive(true);
                }
                else
                {
                    _itemImage.gameObject.SetActive(false);
                }
            }
            
            // 更新高亮显示
            if (_highlightObject != null)
            {
                _highlightObject.SetActive(_currentState == SlotState.Highlighted || 
                                         _currentState == SlotState.Selected);
            }
            
            // 更新占用指示器
            if (_occupiedIndicator != null)
            {
                _occupiedIndicator.SetActive(_currentState == SlotState.Occupied);
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
                case SlotState.Empty:
                    return _emptyColor;
                case SlotState.Occupied:
                    return _occupiedColor;
                case SlotState.Highlighted:
                    return _highlightColor;
                case SlotState.Selected:
                    return _selectedColor;
                case SlotState.Invalid:
                    return _invalidColor;
                default:
                    return _emptyColor;
            }
        }
        
        /// <summary>
        /// 高亮槽位
        /// </summary>
        /// <param name="highlight">是否高亮</param>
        public void SetHighlight(bool highlight)
        {
            if (highlight && _currentState == SlotState.Empty)
            {
                SetState(SlotState.Highlighted);
            }
            else if (!highlight && _currentState == SlotState.Highlighted)
            {
                SetState(SlotState.Empty);
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
                SetState(_currentItem != null ? SlotState.Occupied : SlotState.Empty);
            }
        }
        
        /// <summary>
        /// 清空槽位
        /// </summary>
        public void Clear()
        {
            _currentItem = null;
            SetState(SlotState.Empty);
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
