using UnityEngine;
using UnityEngine.UI;

namespace Resonance.Utilities
{
    /// <summary>
    /// Grid可视化组件
    /// 负责管理Grid的视觉表现和交互
    /// </summary>
    public class GridVisualizer : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private Image _gridBackground;
        [SerializeField] private RectTransform _gridContainer;
        [SerializeField] private GameObject _gridLinesContainer;
        
        [Header("Visual Settings")]
        [SerializeField] private Color _gridLineColor = Color.white;
        [SerializeField] private float _gridLineWidth = 1f;
        [SerializeField] private bool _showGridLines = true;
        
        [Header("Animation Settings")]
        [SerializeField] private AnimationCurve _itemMoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        // 引用
        private GridSystem _gridSystem;
        private Camera _uiCamera;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            SetupGridSystem();
        }
        
        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponents()
        {
            // 获取GridSystem引用
            _gridSystem = GetComponent<GridSystem>();
            if (_gridSystem == null)
            {
                _gridSystem = GetComponentInParent<GridSystem>();
            }
            
            // 获取UI相机
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                _uiCamera = canvas.worldCamera;
            }
            
            // 自动查找组件
            if (_gridBackground == null)
                _gridBackground = GetComponent<Image>();
            
            if (_gridContainer == null)
                _gridContainer = GetComponent<RectTransform>();
            
            if (_gridLinesContainer == null)
            {
                _gridLinesContainer = new GameObject("GridLines");
                _gridLinesContainer.transform.SetParent(transform, false);
            }
        }
        
        /// <summary>
        /// 设置Grid系统
        /// </summary>
        private void SetupGridSystem()
        {
            if (_gridSystem == null) return;
            
            // 订阅Grid系统事件
            _gridSystem.OnItemPlaced += OnItemPlaced;
            _gridSystem.OnItemMoved += OnItemMoved;
            _gridSystem.OnItemRotated += OnItemRotated;
            _gridSystem.OnItemRemoved += OnItemRemoved;
            _gridSystem.OnItemSelected += OnItemSelected;
            _gridSystem.OnItemDeselected += OnItemDeselected;
            
            // 创建网格线
            if (_showGridLines)
            {
                CreateGridLines();
            }
        }
        
        /// <summary>
        /// 创建网格线
        /// </summary>
        private void CreateGridLines()
        {
            if (_gridSystem == null || _gridLinesContainer == null) return;
            
            // 清除现有线条
            foreach (Transform child in _gridLinesContainer.transform)
            {
                DestroyImmediate(child.gameObject);
            }
            
            // 创建垂直线
            for (int x = 0; x <= _gridSystem.GridWidth; x++)
            {
                CreateGridLine(new Vector2(x, 0), new Vector2(x, _gridSystem.GridHeight), true);
            }
            
            // 创建水平线
            for (int y = 0; y <= _gridSystem.GridHeight; y++)
            {
                CreateGridLine(new Vector2(0, y), new Vector2(_gridSystem.GridWidth, y), false);
            }
        }
        
        /// <summary>
        /// 创建单条网格线
        /// </summary>
        /// <param name="start">起点</param>
        /// <param name="end">终点</param>
        /// <param name="isVertical">是否垂直</param>
        private void CreateGridLine(Vector2 start, Vector2 end, bool isVertical)
        {
            GameObject lineObj = new GameObject($"GridLine_{start.x}_{start.y}");
            lineObj.transform.SetParent(_gridLinesContainer.transform, false);
            
            Image lineImage = lineObj.AddComponent<Image>();
            lineImage.color = _gridLineColor;
            
            RectTransform lineRect = lineObj.GetComponent<RectTransform>();
            
            if (isVertical)
            {
                lineRect.sizeDelta = new Vector2(_gridLineWidth, _gridSystem.GridHeight * 64f); // 假设每个格子64像素
                lineRect.anchoredPosition = new Vector2(start.x * 64f, -_gridSystem.GridHeight * 32f);
            }
            else
            {
                lineRect.sizeDelta = new Vector2(_gridSystem.GridWidth * 64f, _gridLineWidth);
                lineRect.anchoredPosition = new Vector2(_gridSystem.GridWidth * 32f, -start.y * 64f);
            }
        }
        
        /// <summary>
        /// 显示/隐藏网格线
        /// </summary>
        /// <param name="show">是否显示</param>
        public void SetGridLinesVisible(bool show)
        {
            _showGridLines = show;
            if (_gridLinesContainer != null)
            {
                _gridLinesContainer.SetActive(show);
            }
        }
        
        /// <summary>
        /// 设置网格背景颜色
        /// </summary>
        /// <param name="color">颜色</param>
        public void SetGridBackgroundColor(Color color)
        {
            if (_gridBackground != null)
            {
                _gridBackground.color = color;
            }
        }
        
        /// <summary>
        /// 高亮指定区域
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="size">大小</param>
        /// <param name="color">高亮颜色</param>
        public void HighlightArea(Vector2Int position, Vector2Int size, Color color)
        {
            // 这里可以实现区域高亮逻辑
            Debug.Log($"GridVisualizer: Highlighting area at {position} with size {size}");
        }
        
        /// <summary>
        /// 清除所有高亮
        /// </summary>
        public void ClearHighlights()
        {
            // 这里可以实现清除高亮逻辑
            Debug.Log("GridVisualizer: Clearing all highlights");
        }
        
        #region Grid System Event Handlers
        
        private void OnItemPlaced(GridItem item)
        {
            Debug.Log($"GridVisualizer: Item {item.itemName} placed at {item.gridPosition}");
            // 可以在这里添加放置动画
        }
        
        private void OnItemMoved(GridItem item)
        {
            Debug.Log($"GridVisualizer: Item {item.itemName} moved to {item.gridPosition}");
            // 可以在这里添加移动动画
        }
        
        private void OnItemRotated(GridItem item)
        {
            Debug.Log($"GridVisualizer: Item {item.itemName} rotated");
            // 可以在这里添加旋转动画
        }
        
        private void OnItemRemoved(GridItem item)
        {
            Debug.Log($"GridVisualizer: Item {item.itemName} removed");
            // 可以在这里添加移除动画
        }
        
        private void OnItemSelected(GridItem item)
        {
            Debug.Log($"GridVisualizer: Item {item.itemName} selected");
            // 可以在这里添加选择效果
        }
        
        private void OnItemDeselected(GridItem item)
        {
            Debug.Log($"GridVisualizer: Item {item.itemName} deselected");
            // 可以在这里移除选择效果
        }
        
        #endregion
        
        #region Animation Methods
        
        /// <summary>
        /// 播放物品移动动画
        /// </summary>
        /// <param name="item">物品</param>
        /// <param name="fromPosition">起始位置</param>
        /// <param name="toPosition">目标位置</param>
        public void PlayItemMoveAnimation(GridItem item, Vector2Int fromPosition, Vector2Int toPosition)
        {
            // 这里可以实现移动动画
            Debug.Log($"GridVisualizer: Playing move animation for {item.itemName} from {fromPosition} to {toPosition}");
        }
        
        /// <summary>
        /// 播放物品旋转动画
        /// </summary>
        /// <param name="item">物品</param>
        public void PlayItemRotateAnimation(GridItem item)
        {
            // 这里可以实现旋转动画
            Debug.Log($"GridVisualizer: Playing rotate animation for {item.itemName}");
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnDestroy()
        {
            // 取消订阅事件
            if (_gridSystem != null)
            {
                _gridSystem.OnItemPlaced -= OnItemPlaced;
                _gridSystem.OnItemMoved -= OnItemMoved;
                _gridSystem.OnItemRotated -= OnItemRotated;
                _gridSystem.OnItemRemoved -= OnItemRemoved;
                _gridSystem.OnItemSelected -= OnItemSelected;
                _gridSystem.OnItemDeselected -= OnItemDeselected;
            }
        }
        
        #endregion
    }
}
