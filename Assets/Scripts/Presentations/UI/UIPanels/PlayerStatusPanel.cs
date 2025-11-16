using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Resonance.Core;
using Resonance.Shared.Interfaces.Services;
using Resonance.Gameplay.Player;
using Resonance.Gameplay.Player.Core;
using Resonance.Gameplay.Player.Data;
using Resonance.Gameplay.Player.Inventory;
using Resonance.Gameplay.Items;
using Resonance.Utilities;
using Resonance.Utilities.Types;

namespace Resonance.Presentations.UI
{
    /// <summary>
    /// PlayerStatusPanel displays the player's comprehensive status information.
    /// It displays player's health, core energy with dynamic slots,
    /// and the current weapon equipped with its ammo count.
    /// 
    /// UI Structure:
    /// - Health: Shows current health with different sprites based on health tier
    /// - Core: Shows current energy, locked capacity, and dynamic slot dividers
    /// - Weapon: Shows weapon icon and ammo count (current/backup)
    /// </summary>
    public class PlayerStatusPanel : UIPanel
    {
        [Header("UI References")]
        [SerializeField] private GameObject _weaponPanel;
        [SerializeField] private GameObject _healthPanel;
        [SerializeField] private GameObject _corePanel;
        
        [Header("Weapon UI")]
        [SerializeField] private Image _weaponIcon;
        
        [Header("Health UI")]
        [SerializeField] private Image _healthValue;
        
        [Header("Core Energy UI")]
        [SerializeField] private Image _coreEnergyBackground;
        [SerializeField] private Image _coreEnergyCurrentValue;
        [SerializeField] private Image _coreEnergyLockedValue;
        [SerializeField] private GameObject _coreSlotsContainer;
        
        [Header("Dynamic UI Prefabs")]
        [SerializeField] private GameObject _dividerPrefab;
        
        [Header("Physical Health Sprites")]
        [SerializeField] private Sprite _healthyHealthSprite;
        [SerializeField] private Sprite _injuredHealthSprite;
        [SerializeField] private Sprite _woundedHealthSprite;
        [SerializeField] private Sprite _criticalHealthSprite;
        
        // Services and Controllers
        private IPlayerService _playerService;
        private PlayerController _playerController;
        private WeaponManager _weaponManager;
        
        // State tracking
        private bool _isInitialized = false;
        private List<GameObject> _dynamicDividers = new List<GameObject>();
        private int _lastMaxSlots = -1;

        protected override void Awake()
        {
            base.Awake();
            
            // Set panel configuration
            _panelName = "PlayerStatusPanel";
            _layer = UILayer.Game;
            _hideOnStart = false; // Player status should be visible by default
            
            transform.localScale = Vector3.one;
            gameObject.SetActive(true);
        }

        protected override void Start()
        {
            base.Start();
            
            // Auto-discover UI components if not manually assigned
            AutoDiscoverUIComponents();
            
            // Load health sprites from Resources
            LoadHealthSprites();
            
            // Initialize with services
            InitializeWithServices();
        }

        private void AutoDiscoverUIComponents()
        {
            // Auto-find panels if not assigned
            if (_weaponPanel == null)
                _weaponPanel = FindChildGameObject("Weapon");
            if (_healthPanel == null)
                _healthPanel = FindChildGameObject("Health");
            if (_corePanel == null)
                _corePanel = FindChildGameObject("Core");
            
            // Auto-find weapon UI components
            if (_weaponIcon == null && _weaponPanel != null)
                _weaponIcon = FindChildComponent<Image>(_weaponPanel, "WeaponIcon");
            
            // Auto-find health UI components
            if (_healthValue == null && _healthPanel != null)
                _healthValue = FindChildComponent<Image>(_healthPanel, "Value");
            
            // Auto-find core energy UI components
            if (_coreEnergyBackground == null && _corePanel != null)
                _coreEnergyBackground = FindChildComponent<Image>(_corePanel, "Background");
            if (_coreEnergyCurrentValue == null && _corePanel != null)
                _coreEnergyCurrentValue = FindChildComponent<Image>(_corePanel, "CurrentValue");
            if (_coreEnergyLockedValue == null && _corePanel != null)
                _coreEnergyLockedValue = FindChildComponent<Image>(_corePanel, "LockedValue");
            if (_coreSlotsContainer == null && _corePanel != null)
                _coreSlotsContainer = FindChildGameObject(_corePanel, "SlotContainer");
            
            // Load divider prefab if not assigned
            if (_dividerPrefab == null)
                _dividerPrefab = Resources.Load<GameObject>("Prefabs/UIs/ComponentsUI/SlotDivider");
        }

        private void LoadHealthSprites()
        {
            // Load health health sprites from Resources
            if (_healthyHealthSprite == null)
                _healthyHealthSprite = Resources.Load<Sprite>("Art/Sprites/Health/healthy");
            if (_injuredHealthSprite == null)
                _injuredHealthSprite = Resources.Load<Sprite>("Art/Sprites/Health/injured");
            if (_woundedHealthSprite == null)
                _woundedHealthSprite = Resources.Load<Sprite>("Art/Sprites/Health/wounded");
            if (_criticalHealthSprite == null)
                _criticalHealthSprite = Resources.Load<Sprite>("Art/Sprites/Health/critical");
            
            // Log warnings if sprites couldn't be loaded
            if (_healthyHealthSprite == null)
                Debug.LogWarning("PlayerStatusPanel: Could not load healthy_health sprite from Resources");
            if (_injuredHealthSprite == null)
                Debug.LogWarning("PlayerStatusPanel: Could not load injured_health sprite from Resources");
            if (_woundedHealthSprite == null)
                Debug.LogWarning("PlayerStatusPanel: Could not load wounded_health sprite from Resources");
            if (_criticalHealthSprite == null)
                Debug.LogWarning("PlayerStatusPanel: Could not load critical_health sprite from Resources");
        }

        private void InitializeWithServices()
        {
            // Get player service
            _playerService = ServiceRegistry.Get<IPlayerService>();
            if (_playerService == null)
            {
                Debug.LogError("PlayerStatusPanel: PlayerService not found");
                return;
            }

            // Check if CurrentPlayer exists and is initialized
            if (_playerService.CurrentPlayer != null && _playerService.CurrentPlayer.IsInitialized)
            {
                _playerController = _playerService.CurrentPlayer.Controller;
                if (_playerController != null)
                {
                    _weaponManager = _playerController.WeaponManager;
                    SubscribeToPlayerEvents();
                    _isInitialized = true;
                    
                    // Initial UI update
                    UpdateAllUI();
                    Debug.Log("PlayerStatusPanel: Initialized successfully");
                    return;
                }
            }

            // Player or Controller not ready yet, subscribe to registration event and retry
            Debug.LogWarning("PlayerStatusPanel: Player not ready yet, waiting for player registration");
            _playerService.OnPlayerRegistered += OnPlayerRegistered;
            
            // Also try again in a few frames as fallback
            Invoke(nameof(RetryInitialization), 0.1f);
        }

        private void OnPlayerRegistered(PlayerMonoBehaviour player)
        {
            Debug.Log("PlayerStatusPanel: Player registered, attempting initialization");
            
            // Unsubscribe from the event
            if (_playerService != null)
            {
                _playerService.OnPlayerRegistered -= OnPlayerRegistered;
            }
            
            // Try to initialize now that player is registered
            if (!_isInitialized)
            {
                InitializeWithServices();
            }
        }

        private void RetryInitialization()
        {
            if (!_isInitialized)
            {
                InitializeWithServices();
            }
        }

        private void SubscribeToPlayerEvents()
        {
            if (_playerController == null) return;
            
            // Subscribe to health events
            _playerController.OnHealthChanged += OnHealthChanged;
            
            // Subscribe to crystal core events
            if (_playerController.Stats?.crystalCore != null)
            {
                _playerController.Stats.crystalCore.OnEnergyChanged += OnCoreEnergyChanged;
                _playerController.Stats.crystalCore.OnCoreHealthChanged += OnCoreHealthChanged;
            }
            
            // Subscribe to weapon events
            if (_weaponManager != null)
            {
                _weaponManager.OnWeaponEquipped += OnWeaponEquipped;
                _weaponManager.OnWeaponUnequipped += OnWeaponUnequipped;
            }
        }

        private void UnsubscribeFromPlayerEvents()
        {
            if (_playerController != null)
            {
                _playerController.OnHealthChanged -= OnHealthChanged;
                
                // Unsubscribe from crystal core events
                if (_playerController.Stats?.crystalCore != null)
                {
                    _playerController.Stats.crystalCore.OnEnergyChanged -= OnCoreEnergyChanged;
                    _playerController.Stats.crystalCore.OnCoreHealthChanged -= OnCoreHealthChanged;
                }
            }
            
            if (_weaponManager != null)
            {
                _weaponManager.OnWeaponEquipped -= OnWeaponEquipped;
                _weaponManager.OnWeaponUnequipped -= OnWeaponUnequipped;
            }
            
            // Unsubscribe from player service events
            if (_playerService != null)
            {
                _playerService.OnPlayerRegistered -= OnPlayerRegistered;
            }
        }

        #region Event Handlers

        private void OnHealthChanged(float currentHealth, float maxHealth)
        {
            UpdateHealthUI(currentHealth, maxHealth);
        }

        private void OnCoreEnergyChanged(float currentEnergy, float maxEnergy)
        {
            UpdateCoreUI();
        }
        
        private void OnCoreHealthChanged(float currentCoreHealth, float maxCoreHealth)
        {
            UpdateCoreUI();
            UpdateCoreSlotsUI();
        }

        private void OnWeaponEquipped(WeaponDataAsset gunData)
        {
            UpdateWeaponUI();
        }

        private void OnWeaponUnequipped()
        {
            UpdateWeaponUI();
        }

        #endregion

        #region UI Update Methods

        private void UpdateAllUI()
        {
            if (!_isInitialized) return;
            
            UpdateWeaponUI();
            UpdateHealthUI();
            UpdateCoreUI();
            UpdateCoreSlotsUI();
        }

        private void UpdateWeaponUI()
        {
            if (_weaponManager == null) return;
            
            bool hasWeapon = _weaponManager.HasEquippedWeapon;
            WeaponDataAsset currentWeapon = _weaponManager.CurrentWeapon;
            
            // Update weapon icon
            if (_weaponIcon != null)
            {
                if (hasWeapon && currentWeapon != null && currentWeapon.weaponIcon != null)
                {
                    _weaponIcon.sprite = currentWeapon.weaponIcon;
                    _weaponIcon.color = Color.white;
                }
                else
                {
                    _weaponIcon.sprite = Resources.Load<Sprite>("Art/Sprites/WeaponIcon/empty_icon");
                    _weaponIcon.color = Color.white;
                }
            }
        }

        private void UpdateHealthUI(float currentHealth = -1, float maxHealth = -1)
        {
            if (_healthValue == null || _playerController == null) return;

            var stats = _playerController.Stats;
            
            // Get current values if not provided
            if (currentHealth < 0 || maxHealth < 0)
            {
                currentHealth = stats.currentHealth;
                maxHealth = stats.maxHealth;
            }
            
            // Determine which sprite to use based on health percentage
            Sprite healthSprite = null;

            switch (stats.healthTier)
            {
                case HealthTier.Healthy:
                    healthSprite = _healthyHealthSprite;
                    break;
                case HealthTier.Injured:
                    healthSprite = _injuredHealthSprite;
                    break;
                case HealthTier.Wounded:
                    healthSprite = _woundedHealthSprite;
                    break;
                case HealthTier.Critical:
                    healthSprite = _criticalHealthSprite;
                    break;
            }
            
            // Update the image
            if (healthSprite != null)
            {
                _healthValue.sprite = healthSprite;
            }
        }

        private void UpdateCoreUI(float currentEnergy = -1, float maxEnergy = -1)
        {
            if (_playerController?.Stats?.crystalCore == null) return;
            
            var crystalCore = _playerController.Stats.crystalCore;
            
            // Get current values if not provided
            if (currentEnergy < 0 || maxEnergy < 0)
            {
                currentEnergy = crystalCore.CurrentEnergy;
                maxEnergy = crystalCore.MaxEnergy;
            }
            
            float currentCoreHealth = crystalCore.CurrentCoreHealth;
            float maxCoreHealth = crystalCore.MaxCoreHealth;
            
            // Update background (represents max capacity)
            if (_coreEnergyBackground != null)
            {
                // Background always shows the full max capacity
                _coreEnergyBackground.fillAmount = 1f;
            }
            
            // Update current value (shows current energy as fill amount)
            if (_coreEnergyCurrentValue != null)
            {
                float energyPercentage = maxCoreHealth > 0 ? currentEnergy / maxCoreHealth : 0f;
                _coreEnergyCurrentValue.fillAmount = energyPercentage;
            }
            
            // Update locked value (shows locked capacity)
            if (_coreEnergyLockedValue != null)
            {
                float lockedHealth = maxCoreHealth - currentCoreHealth;
                float lockedPercentage = maxCoreHealth > 0 ? lockedHealth / maxCoreHealth : 0f;
                _coreEnergyLockedValue.fillAmount = lockedPercentage;
            }
        }
        
        private void UpdateCoreSlotsUI()
        {
            if (_playerController?.Stats?.crystalCore == null || _coreSlotsContainer == null) return;
            
            var crystalCore = _playerController.Stats.crystalCore;
            int maxSlots = crystalCore.MaxSlots;
            
            // Only update if slot count changed
            if (_lastMaxSlots == maxSlots) return;
            _lastMaxSlots = maxSlots;
            
            // Clear existing dynamic dividers
            foreach (var divider in _dynamicDividers)
            {
                if (divider != null)
                    DestroyImmediate(divider);
            }
            _dynamicDividers.Clear();
            
            // Generate dividers (maxSlots - 1 dividers between slots)
            int dividersNeeded = Mathf.Max(0, maxSlots - 1);
            
            if (_dividerPrefab != null && dividersNeeded > 0)
            {
                // Calculate positions for equal distribution
                RectTransform containerRect = _coreSlotsContainer.GetComponent<RectTransform>();
                float containerWidth = containerRect.rect.width;
                float spacing = containerWidth / maxSlots;
                
                for (int i = 0; i < dividersNeeded; i++)
                {
                    GameObject divider = Instantiate(_dividerPrefab, _coreSlotsContainer.transform);
                    divider.name = $"Divider_{i + 1}";
                    
                    // Position divider
                    RectTransform dividerRect = divider.GetComponent<RectTransform>();
                    if (dividerRect != null)
                    {
                        // Position at slot boundaries
                        float xPos = spacing * (i + 1);
                        dividerRect.anchoredPosition = new Vector2(xPos, 0);
                    }
                    
                    _dynamicDividers.Add(divider);
                }
            }
        }

        #endregion

        #region Utility Methods

        private GameObject FindChildGameObject(string childName)
        {
            // First try to find in direct children
            Transform child = transform.Find(childName);
            if (child != null) return child.gameObject;
            
            // Then try to find in Panel child
            Transform panel = transform.Find("Panel");
            if (panel != null)
            {
                child = panel.Find(childName);
                if (child != null) return child.gameObject;
            }
            
            return null;
        }
        
        private GameObject FindChildGameObject(GameObject parent, string childName)
        {
            if (parent == null) return null;
            
            Transform child = parent.transform.Find(childName);
            if (child != null) return child.gameObject;
            
            return null;
        }

        private T FindChildComponent<T>(GameObject parent, string childName) where T : Component
        {
            if (parent == null) return null;
            
            Transform child = parent.transform.Find(childName);
            if (child != null)
            {
                return child.GetComponent<T>();
            }
            
            return null;
        }

        #endregion

        #region UIPanel Overrides

        protected override void OnInitialize()
        {
            Debug.Log("PlayerStatusPanel: OnInitialize called");
        }

        protected override void OnShow()
        {
            Debug.Log("PlayerStatusPanel: Shown");
            UpdateAllUI();
        }

        protected override void OnHide()
        {
            Debug.Log("PlayerStatusPanel: Hidden");
        }

        protected override void OnCleanup()
        {
            UnsubscribeFromPlayerEvents();
            _isInitialized = false;
            Debug.Log("PlayerStatusPanel: Cleaned up");
        }

        #endregion

        #region Unity Lifecycle

        void OnDestroy()
        {
            UnsubscribeFromPlayerEvents();
        }

        #endregion
    }
}