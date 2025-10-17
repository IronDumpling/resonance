using UnityEngine;
using Resonance.Player.Core;
using Resonance.Interfaces.Operations;
using Resonance.Core;
using Resonance.Interfaces.Services;
using Resonance.Utilities;

namespace Resonance.Player.Actions
{
    /// <summary>
    /// Player reload action that handles weapon reloading from player's ammo inventory
    /// </summary>
    public class PlayerReloadAction : IPlayerAction
    {
        // Action properties
        public string Name => "Reload";
        public float Duration => 2.0f; // 2 second reload time
        public bool CanInterrupt => true;
        public bool BlocksMovement => false;
        public bool ProvidesInvulnerability => false;
        public bool IsFinished { get; private set; }

        // Runtime state
        private float _actionTimer;
        private bool _isActive;
        private string _ammoType;
        private int _ammoNeeded;
        private int _playerAmmoAvailable;

        // Services
        private IAudioService _audioService;

        public bool CanStart(PlayerController playerController)
        {
            // Check basic conditions
            if (!playerController.IsAlive)
            {
                Debug.Log("PlayerReloadAction: Cannot reload - player not healthly alive");
                return false;
            }

            if (!playerController.HasEquippedWeapon)
            {
                Debug.Log("PlayerReloadAction: Cannot reload - no weapon equipped");
                return false;
            }

            if (playerController.IsAiming)
            {
                Debug.Log("PlayerReloadAction: Cannot reload - player is aiming");
                return false;
            }

            // Check if player is shooting (current action might be shooting)
            if (playerController.IsActionActive())
            {
                string currentAction = playerController.GetCurrentActionName();
                if (currentAction == "Shoot" || currentAction == "Aiming")
                {
                    Debug.Log($"PlayerReloadAction: Cannot reload - player is {currentAction}");
                    return false;
                }
            }

            var weaponManager = playerController.WeaponManager;
            if (weaponManager?.CurrentWeapon == null)
            {
                Debug.Log("PlayerReloadAction: Cannot reload - no current gun");
                return false;
            }

            var currentWeapon = weaponManager.CurrentWeapon;
            
            // Check if weapon is already full
            if (currentWeapon.CurrentAmmo >= currentWeapon.maxAmmo)
            {
                Debug.Log("PlayerReloadAction: Cannot reload - weapon already full");
                return false;
            }

            // Check if player has compatible ammo in inventory
            string weaponAmmoType = currentWeapon.ammoType;
            int playerAmmoCount = playerController.Inventory?.GetAmmoCount(weaponAmmoType) ?? 0;
            
            if (playerAmmoCount <= 0)
            {
                Debug.Log($"PlayerReloadAction: Cannot reload - no {weaponAmmoType} ammo in inventory");
                return false;
            }

            Debug.Log($"PlayerReloadAction: Can start reload - weapon needs {currentWeapon.maxAmmo - currentWeapon.CurrentAmmo} ammo, player has {playerAmmoCount}");
            return true;
        }

        public void Start(PlayerController playerController)
        {
            _isActive = true;
            IsFinished = false;
            _actionTimer = 0f;

            // Get audio service
            _audioService = ServiceRegistry.Get<IAudioService>();

            var currentWeapon = playerController.WeaponManager.CurrentWeapon;
            _ammoType = currentWeapon.ammoType;
            _ammoNeeded = currentWeapon.maxAmmo - currentWeapon.CurrentAmmo;
            _playerAmmoAvailable = playerController.Inventory?.GetAmmoCount(_ammoType) ?? 0;

            Debug.Log($"PlayerReloadAction: Started reload - need {_ammoNeeded} {_ammoType} ammo, player has {_playerAmmoAvailable}");

            // Play reload start audio
            PlayReloadStartAudio(playerController);
        }

        public void Update(PlayerController playerController, float deltaTime)
        {
            if (!_isActive) return;

            _actionTimer += deltaTime;

            // Check if reload duration is complete
            if (_actionTimer >= Duration)
            {
                CompleteReload(playerController);
            }
        }

        public void Cancel(PlayerController playerController)
        {
            if (_isActive)
            {
                Debug.Log("PlayerReloadAction: Reload cancelled");
                _isActive = false;
                IsFinished = true;

                // Play cancel audio
                PlayReloadCancelAudio(playerController);
            }
        }

        public void OnDamageTaken(PlayerController playerController)
        {
            // Reload can be interrupted by damage
            Debug.Log("PlayerReloadAction: Reload interrupted by damage");
            Cancel(playerController);
        }

        private void CompleteReload(PlayerController playerController)
        {
            if (!_isActive) return;

            var currentWeapon = playerController.WeaponManager.CurrentWeapon;
            var inventory = playerController.Inventory;

            if (inventory == null)
            {
                Debug.LogError("PlayerReloadAction: Player inventory is null, cannot complete reload");
                Cancel(playerController);
                return;
            }

            // Calculate actual ammo transfer
            int ammoToTransfer;
            if (_playerAmmoAvailable >= _ammoNeeded)
            {
                // Player has enough ammo to fill weapon completely
                ammoToTransfer = _ammoNeeded;
                currentWeapon.SetCurrentAmmo(currentWeapon.maxAmmo);
                inventory.ConsumeAmmo(_ammoType, _ammoNeeded);
            }
            else
            {
                // Player doesn't have enough ammo, transfer all available
                ammoToTransfer = _playerAmmoAvailable;
                currentWeapon.SetCurrentAmmo(currentWeapon.CurrentAmmo + _playerAmmoAvailable);
                inventory.ConsumeAmmo(_ammoType, _playerAmmoAvailable);
            }

            Debug.Log($"PlayerReloadAction: Reload completed - transferred {ammoToTransfer} {_ammoType} ammo");
            Debug.Log($"PlayerReloadAction: Weapon ammo: {currentWeapon.CurrentAmmo}/{currentWeapon.maxAmmo}");
            Debug.Log($"PlayerReloadAction: Player {_ammoType} ammo remaining: {inventory.GetAmmoCount(_ammoType)}");

            // Play reload complete audio
            PlayReloadCompleteAudio(playerController);

            _isActive = false;
            IsFinished = true;
        }

        private void PlayReloadStartAudio(PlayerController playerController)
        {
            if (_audioService == null) return;

            // Play reload start sound - using existing WeaponReload type
            string weaponAmmoType = playerController.WeaponManager.CurrentWeapon.ammoType;
            if (weaponAmmoType == "Pisto")
            {
                _audioService.PlaySFX2D(AudioClipType.WeaponReloadPistol, 0.8f, 1f);
            }
            else if (weaponAmmoType == "Rifle")
            {
                _audioService.PlaySFX2D(AudioClipType.WeaponReloadRifle, 0.8f, 1f);
            }
        }

        private void PlayReloadCompleteAudio(PlayerController playerController)
        {
            if (_audioService == null) return;

            // Play reload complete sound - using WeaponCock for completion sound
            _audioService.PlaySFX2D(AudioClipType.WeaponCock, 0.8f, 1f);
        }

        private void PlayReloadCancelAudio(PlayerController playerController)
        {
            if (_audioService == null) return;

            // Play reload cancel sound - using lower volume WeaponEmpty for cancel
            _audioService.PlaySFX2D(AudioClipType.WeaponEmpty, 0.4f, 1f);
        }
    }
}
