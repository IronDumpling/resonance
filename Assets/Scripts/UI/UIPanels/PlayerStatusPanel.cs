using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Resonance.Core;
using Resonance.Interfaces.Services;
using Resonance.Player;
using Resonance.Player.Core;
using Resonance.Items;
using Resonance.Utilities;

namespace Resonance.UI
{
    /// <summary>
    /// PlayerStatusPanel displays the player's status and health information.
    /// It displays player's health health and core health.
    /// It also displays the current weapon equipped, and it's ammo count.
    /// </summary>
    public class PlayerStatusPanel : UIPanel
    {
        [Header("UI References")]
        [SerializeField] private GameObject _weaponPanel;
        [SerializeField] private GameObject _healthPanel;
        [SerializeField] private GameObject _coreHealthPanel;
        
        [Header("Weapon UI")]
        [SerializeField] private Image _weaponIcon;
        [SerializeField] private TextMeshProUGUI _ammoCount;
        
        [Header("Physical Health UI")]
        [SerializeField] private Image _healthValue;
        
        [Header("Core Health UI")]
        [SerializeField] private Image _coreHealthBar;
        [SerializeField] private Image _coreHealthValue;
        
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
            if (_coreHealthPanel == null)
                _coreHealthPanel = FindChildGameObject("CoreHealth");
            
            // Auto-find weapon UI components
            if (_weaponIcon == null && _weaponPanel != null)
                _weaponIcon = FindChildComponent<Image>(_weaponPanel, "WeaponIcon");
            if (_ammoCount == null && _weaponPanel != null)
                _ammoCount = FindChildComponent<TextMeshProUGUI>(_weaponPanel, "AmmoCount");
            
            // Auto-find health health UI components
            if (_healthValue == null && _healthPanel != null)
                _healthValue = FindChildComponent<Image>(_healthPanel, "Value");
            
            // Auto-find core health UI components
            if (_coreHealthBar == null && _coreHealthPanel != null)
                _coreHealthBar = FindChildComponent<Image>(_coreHealthPanel, "Bar");
            if (_coreHealthValue == null && _coreHealthPanel != null)
                _coreHealthValue = FindChildComponent<Image>(_coreHealthPanel, "Value");
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
            
            // Subscribe to dual health events
            _playerController.OnHealthChanged += OnHealthChanged;
            _playerController.OnCoreHealthChanged += OnCoreHealthChanged;
            
            // Subscribe to weapon events
            if (_weaponManager != null)
            {
                _weaponManager.OnWeaponEquipped += OnWeaponEquipped;
                _weaponManager.OnWeaponUnequipped += OnWeaponUnequipped;
                _weaponManager.OnAmmoChanged += OnAmmoChanged;
            }
            
            // Subscribe to ammo inventory events
            if (_playerController.Inventory != null)
            {
                _playerController.Inventory.OnAmmoChanged += OnBackupAmmoChanged;
            }
        }

        private void UnsubscribeFromPlayerEvents()
        {
            if (_playerController != null)
            {
                _playerController.OnHealthChanged -= OnHealthChanged;
                _playerController.OnCoreHealthChanged -= OnCoreHealthChanged;
                
                // Unsubscribe from ammo inventory events
                if (_playerController.Inventory != null)
                {
                    _playerController.Inventory.OnAmmoChanged -= OnBackupAmmoChanged;
                }
            }
            
            if (_weaponManager != null)
            {
                _weaponManager.OnWeaponEquipped -= OnWeaponEquipped;
                _weaponManager.OnWeaponUnequipped -= OnWeaponUnequipped;
                _weaponManager.OnAmmoChanged -= OnAmmoChanged;
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

        private void OnCoreHealthChanged(float currentHealth, float maxHealth)
        {
            UpdateCoreHealthUI(currentHealth, maxHealth);
        }

        private void OnWeaponEquipped(GunDataAsset gunData)
        {
            UpdateWeaponUI();
        }

        private void OnWeaponUnequipped()
        {
            UpdateWeaponUI();
        }

        private void OnAmmoChanged(int currentAmmo)
        {
            UpdateAmmoUI();
        }
        
        private void OnBackupAmmoChanged(string ammoType, int oldAmount, int newAmount)
        {
            // Only update UI if the changed ammo type matches the current weapon's ammo type
            if (_weaponManager != null && _weaponManager.HasEquippedWeapon && 
                string.Equals(ammoType, _weaponManager.AmmoType, System.StringComparison.OrdinalIgnoreCase))
            {
                UpdateAmmoUI();
            }
        }

        #endregion

        #region UI Update Methods

        private void UpdateAllUI()
        {
            if (!_isInitialized) return;
            
            UpdateWeaponUI();
            UpdateHealthUI();
            UpdateCoreHealthUI();
        }

        private void UpdateWeaponUI()
        {
            if (_weaponManager == null) return;
            
            bool hasWeapon = _weaponManager.HasEquippedWeapon;
            GunDataAsset currentGun = _weaponManager.CurrentGun;
            
            // Update weapon icon
            if (_weaponIcon != null)
            {
                if (hasWeapon && currentGun != null && currentGun.weaponIcon != null)
                {
                    _weaponIcon.sprite = currentGun.weaponIcon;
                    _weaponIcon.color = Color.white;
                }
                else
                {
                    _weaponIcon.sprite = Resources.Load<Sprite>("Art/Sprites/WeaponIcon/empty_icon");
                    _weaponIcon.color = Color.white;
                }
            }
            
            // Update ammo count
            UpdateAmmoUI();
        }

        private void UpdateAmmoUI()
        {
            if (_ammoCount == null || _weaponManager == null || _playerController == null) return;
            
            bool hasWeapon = _weaponManager.HasEquippedWeapon;
            
            if (hasWeapon)
            {
                int currentAmmo = _weaponManager.CurrentAmmo;
                int backupAmmo = _playerController.Inventory?.GetAmmoCount(_weaponManager.AmmoType) ?? 0;
                _ammoCount.text = $"{currentAmmo}/{backupAmmo}";
            }
            else
            {
                _ammoCount.text = ""; // Hide text if no weapon
            }
        }

        private void UpdateHealthUI(float currentHealth = -1, float maxHealth = -1)
        {
            if (_healthValue == null || _playerController == null) return;
            
            // Get current values if not provided
            if (currentHealth < 0 || maxHealth < 0)
            {
                var stats = _playerController.Stats;
                currentHealth = stats.currentHealth;
                maxHealth = stats.maxHealth;
            }
            
            // Calculate health percentage
            float healthPercentage = maxHealth > 0 ? currentHealth / maxHealth : 0f;
            
            // Determine which sprite to use based on health percentage
            Sprite healthSprite = null;
            if (healthPercentage > 0.7f)
                healthSprite = _healthyHealthSprite;
            else if (healthPercentage > 0.3f)
                healthSprite = _woundedHealthSprite;
            else
                healthSprite = _criticalHealthSprite;
            
            // Update the image
            if (healthSprite != null)
            {
                _healthValue.sprite = healthSprite;
            }
        }

        private void UpdateCoreHealthUI(float currentHealth = -1, float maxHealth = -1)
        {
            if (_coreHealthValue == null || _playerController == null) return;
            
            // Get current values if not provided
            if (currentHealth < 0 || maxHealth < 0)
            {
                var stats = _playerController.Stats;
                currentHealth = stats.crystalCore.CurrentEnergy;
                maxHealth = stats.crystalCore.CurrentEnergyCapacity;
            }
            
            // Calculate health percentage
            float healthPercentage = maxHealth > 0 ? currentHealth / maxHealth : 0f;
            
            // Update fill amount
            _coreHealthValue.fillAmount = healthPercentage;
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