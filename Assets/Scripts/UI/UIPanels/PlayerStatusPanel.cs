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
    /// It displays player's physical health and mental health.
    /// It also displays the current weapon equipped, and it's ammo count.
    /// </summary>
    public class PlayerStatusPanel : UIPanel
    {
        [Header("UI References")]
        [SerializeField] private GameObject _weaponPanel;
        [SerializeField] private GameObject _physicalHealthPanel;
        [SerializeField] private GameObject _mentalHealthPanel;
        
        [Header("Weapon UI")]
        [SerializeField] private Image _weaponIcon;
        [SerializeField] private TextMeshProUGUI _ammoCount;
        
        [Header("Physical Health UI")]
        [SerializeField] private Image _physicalHealthValue;
        
        [Header("Mental Health UI")]
        [SerializeField] private Image _mentalHealthBar;
        [SerializeField] private Image _mentalHealthValue;
        
        [Header("Physical Health Sprites")]
        [SerializeField] private Sprite _normalHealthSprite;
        [SerializeField] private Sprite _medianHealthSprite;
        [SerializeField] private Sprite _badHealthSprite;
        
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
            
            // Force correct scale and visibility (override prefab settings)
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
            if (_physicalHealthPanel == null)
                _physicalHealthPanel = FindChildGameObject("PhysicalHealth");
            if (_mentalHealthPanel == null)
                _mentalHealthPanel = FindChildGameObject("MentalHealth");
            
            // Auto-find weapon UI components
            if (_weaponIcon == null && _weaponPanel != null)
                _weaponIcon = FindChildComponent<Image>(_weaponPanel, "WeaponIcon");
            if (_ammoCount == null && _weaponPanel != null)
                _ammoCount = FindChildComponent<TextMeshProUGUI>(_weaponPanel, "AmmoCount");
            
            // Auto-find physical health UI components
            if (_physicalHealthValue == null && _physicalHealthPanel != null)
                _physicalHealthValue = FindChildComponent<Image>(_physicalHealthPanel, "Value");
            
            // Auto-find mental health UI components
            if (_mentalHealthBar == null && _mentalHealthPanel != null)
                _mentalHealthBar = FindChildComponent<Image>(_mentalHealthPanel, "Bar");
            if (_mentalHealthValue == null && _mentalHealthPanel != null)
                _mentalHealthValue = FindChildComponent<Image>(_mentalHealthPanel, "Value");
        }

        private void LoadHealthSprites()
        {
            // Load physical health sprites from Resources
            if (_normalHealthSprite == null)
                _normalHealthSprite = Resources.Load<Sprite>("Art/Sprites/PhysicalHealth/normal_health");
            if (_medianHealthSprite == null)
                _medianHealthSprite = Resources.Load<Sprite>("Art/Sprites/PhysicalHealth/median_health");
            if (_badHealthSprite == null)
                _badHealthSprite = Resources.Load<Sprite>("Art/Sprites/PhysicalHealth/bad_health");
            
            // Log warnings if sprites couldn't be loaded
            if (_normalHealthSprite == null)
                Debug.LogWarning("PlayerStatusPanel: Could not load normal_health sprite from Resources");
            if (_medianHealthSprite == null)
                Debug.LogWarning("PlayerStatusPanel: Could not load median_health sprite from Resources");
            if (_badHealthSprite == null)
                Debug.LogWarning("PlayerStatusPanel: Could not load bad_health sprite from Resources");
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
            _playerController.OnPhysicalHealthChanged += OnPhysicalHealthChanged;
            _playerController.OnMentalHealthChanged += OnMentalHealthChanged;
            
            // Subscribe to weapon events
            if (_weaponManager != null)
            {
                _weaponManager.OnWeaponEquipped += OnWeaponEquipped;
                _weaponManager.OnWeaponUnequipped += OnWeaponUnequipped;
                _weaponManager.OnAmmoChanged += OnAmmoChanged;
            }
        }

        private void UnsubscribeFromPlayerEvents()
        {
            if (_playerController != null)
            {
                _playerController.OnPhysicalHealthChanged -= OnPhysicalHealthChanged;
                _playerController.OnMentalHealthChanged -= OnMentalHealthChanged;
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

        private void OnPhysicalHealthChanged(float currentHealth, float maxHealth)
        {
            UpdatePhysicalHealthUI(currentHealth, maxHealth);
        }

        private void OnMentalHealthChanged(float currentHealth, float maxHealth)
        {
            UpdateMentalHealthUI(currentHealth, maxHealth);
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

        #endregion

        #region UI Update Methods

        private void UpdateAllUI()
        {
            if (!_isInitialized) return;
            
            UpdateWeaponUI();
            UpdatePhysicalHealthUI();
            UpdateMentalHealthUI();
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
                    _weaponIcon.color = Color.white; // Make sure it's visible
                }
                else
                {
                    _weaponIcon.sprite = null;
                    _weaponIcon.color = Color.clear; // Hide if no weapon
                }
            }
            
            // Update ammo count
            UpdateAmmoUI();
        }

        private void UpdateAmmoUI()
        {
            if (_ammoCount == null || _weaponManager == null) return;
            
            bool hasWeapon = _weaponManager.HasEquippedWeapon;
            
            if (hasWeapon)
            {
                int currentAmmo = _weaponManager.CurrentAmmo;
                int maxAmmo = _weaponManager.MaxAmmo;
                _ammoCount.text = $"{currentAmmo}/{maxAmmo}";
            }
            else
            {
                _ammoCount.text = ""; // Hide text if no weapon
            }
        }

        private void UpdatePhysicalHealthUI(float currentHealth = -1, float maxHealth = -1)
        {
            if (_physicalHealthValue == null || _playerController == null) return;
            
            // Get current values if not provided
            if (currentHealth < 0 || maxHealth < 0)
            {
                var stats = _playerController.Stats;
                currentHealth = stats.currentPhysicalHealth;
                maxHealth = stats.maxPhysicalHealth;
            }
            
            // Calculate health percentage
            float healthPercentage = maxHealth > 0 ? currentHealth / maxHealth : 0f;
            
            // Determine which sprite to use based on health percentage
            Sprite healthSprite = null;
            if (healthPercentage > 0.7f)
                healthSprite = _normalHealthSprite;
            else if (healthPercentage > 0.3f)
                healthSprite = _medianHealthSprite;
            else
                healthSprite = _badHealthSprite;
            
            // Update the image
            if (healthSprite != null)
            {
                _physicalHealthValue.sprite = healthSprite;
            }
        }

        private void UpdateMentalHealthUI(float currentHealth = -1, float maxHealth = -1)
        {
            if (_mentalHealthValue == null || _playerController == null) return;
            
            // Get current values if not provided
            if (currentHealth < 0 || maxHealth < 0)
            {
                var stats = _playerController.Stats;
                currentHealth = stats.currentMentalHealth;
                maxHealth = stats.maxMentalHealth;
            }
            
            // Calculate health percentage
            float healthPercentage = maxHealth > 0 ? currentHealth / maxHealth : 0f;
            
            // Update fill amount
            _mentalHealthValue.fillAmount = healthPercentage;
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