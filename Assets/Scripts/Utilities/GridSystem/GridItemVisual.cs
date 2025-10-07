using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Resonance.Utilities
{
    /// <summary>
    /// Manages the visual representation of a GridItem in the inventory
    /// Handles prefab instantiation, sizing, rotation, and selection
    /// This component is added to the instantiated ItemPrefab which already has a Button component
    /// </summary>
    public class GridItemVisual : MonoBehaviour
    {
        [Header("Item Components")]
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _quantityText;
        
        [Header("Alpha Settings")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _normalAlpha = 0.95f;
        [SerializeField] private float _selectedAlpha = 1f;
        [SerializeField] private float _dragAlpha = 0.7f;

        private GridItem _gridItem;
        private float _slotSize;
        private bool _isSelected = false;

        // Events
        public System.Action<GridItemVisual> OnItemClicked;
        
        public GridItem GridItem => _gridItem;
        public RectTransform RectTransform => _rectTransform;
        public bool IsSelected => _isSelected;
        
        private void Awake()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
            
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            // Get Button component (should be on the prefab)
            if (_button == null)
                _button = GetComponent<Button>();
            
            // Subscribe to button click
            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClicked);
            }
            else
            {
                Debug.LogWarning("GridItemVisual: Button component not found on prefab. Please add a Button component.");
            }

            // Get quantity text
            if (_quantityText == null)
            {
                _quantityText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }
        
        /// <summary>
        /// Handle button click (Unity UI Button)
        /// </summary>
        private void OnButtonClicked()
        {
            OnItemClicked?.Invoke(this);
        }
        
        /// <summary>
        /// Initialize the visual with a GridItem
        /// Note: This component is attached to the already instantiated prefab
        /// </summary>
        public void Initialize(GridItem item, float slotSize, Transform parent)
        {
            _gridItem = item;
            _slotSize = slotSize;
            
            // Set parent
            transform.SetParent(parent, false);

            // Set normal alpha
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = _normalAlpha;
            }
            
            // Update size and position
            UpdateSizeAndPosition();
        }
        
        /// <summary>
        /// Update the size and position based on grid item properties
        /// </summary>
        public void UpdateSizeAndPosition()
        {
            if (_gridItem == null || _rectTransform == null) return;
            
            // Calculate size in pixels (with spacing)
            float width = _gridItem.CurrentWidth * _slotSize;
            float height = _gridItem.CurrentHeight * _slotSize;
            
            // Set size - stretch to cover all occupied slots
            _rectTransform.sizeDelta = new Vector2(width, height);
            
            // Set position - aligned to top-left of the first slot
            Vector2 position = new Vector2(
                _gridItem.gridPosition.x * _slotSize,
                -_gridItem.gridPosition.y * _slotSize
            );
            _rectTransform.anchoredPosition = position;
            
            // Apply rotation if needed
            if (_gridItem.isRotated)
            {
                _rectTransform.localRotation = Quaternion.Euler(0, 0, 90);
            }
            else
            {
                _rectTransform.localRotation = Quaternion.identity;
            }

            // Update quantity text
            if (_quantityText != null)
            {
                if (_gridItem.Quantity == 1)
                {
                    _quantityText.text = "";
                }
                else
                {
                    _quantityText.text = _gridItem.Quantity.ToString();
                }
            }
        }
        
        /// <summary>
        /// Set selection state
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            
            // Update alpha
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = selected ? _selectedAlpha : _normalAlpha;
            }
            
            // Update grid item state
            if (_gridItem != null)
            {
                _gridItem.isSelected = selected;
            }
        }
        
        /// <summary>
        /// Set dragging state
        /// </summary>
        public void SetDragging(bool dragging)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = dragging ? _dragAlpha : _normalAlpha;
                _canvasGroup.blocksRaycasts = !dragging;
            }
            
            // Update grid item state
            if (_gridItem != null)
            {
                _gridItem.isDragging = dragging;
            }
        }
        
        /// <summary>
        /// Clean up
        /// </summary>
        private void OnDestroy()
        {
            // Unsubscribe from button
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClicked);
            }
            
            OnItemClicked = null;
        }
    }
}

