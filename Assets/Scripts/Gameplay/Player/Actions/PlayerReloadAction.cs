using UnityEngine;
using System.Linq;
using Resonance.Player.Core;
using Resonance.Interfaces.Operations;
using Resonance.Core;
using Resonance.Interfaces.Services;
using Resonance.Utilities;
using Resonance.Utilities.Types;

namespace Resonance.Player.Actions
{
    /// <summary>
    /// Player reload action that restores Crystal Core energy by consuming EnergyBottle from inventory
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

        // Services
        private IAudioService _audioService;

        public bool CanStart(PlayerController playerController)
        {
            // Check basic conditions
            if (!playerController.IsAlive)
            {
                Debug.Log("PlayerReloadAction: Cannot reload - player not alive");
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

            // Check if player energy is already full
            var crystalCore = playerController.Stats.crystalCore;
            if (crystalCore.CurrentEnergy >= crystalCore.MaxEnergy)
            {
                Debug.Log("PlayerReloadAction: Cannot reload - Crystal Core energy already full");
                return false;
            }

            // Check if player has EnergyBottle in inventory
            var energyBottles = playerController.Inventory?.GetItemsByType(ItemType.Consumable)
                .Where(item => item.ConsumableType == ConsumableType.EnergyBottle)
                .ToList();
            
            if (energyBottles == null || energyBottles.Count == 0)
            {
                Debug.Log("PlayerReloadAction: Cannot reload - no EnergyBottle in inventory");
                return false;
            }

            Debug.Log($"PlayerReloadAction: Can start reload - Current energy: {crystalCore.CurrentEnergy}/{crystalCore.MaxEnergy}, EnergyBottles: {energyBottles.Count}");
            return true;
        }

        public void Start(PlayerController playerController)
        {
            _isActive = true;
            IsFinished = false;
            _actionTimer = 0f;

            // Get audio service
            _audioService = ServiceRegistry.Get<IAudioService>();

            var crystalCore = playerController.Stats.crystalCore;
            Debug.Log($"PlayerReloadAction: Started energy reload - Current: {crystalCore.CurrentEnergy}/{crystalCore.MaxEnergy}");

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

            var inventory = playerController.Inventory;
            var consumableManager = playerController.ConsumableManager;

            if (inventory == null)
            {
                Debug.LogError("PlayerReloadAction: Player inventory is null, cannot complete reload");
                Cancel(playerController);
                return;
            }

            if (consumableManager == null)
            {
                Debug.LogError("PlayerReloadAction: ConsumableManager is null, cannot complete reload");
                Cancel(playerController);
                return;
            }

            // Find first EnergyBottle in inventory
            var energyBottles = inventory.GetItemsByType(ItemType.Consumable)
                .Where(item => item.ConsumableType == ConsumableType.EnergyBottle)
                .OrderBy(item => item.ItemID)
                .ToList();
            
            if (energyBottles.Count == 0)
            {
                Debug.LogWarning("PlayerReloadAction: No EnergyBottle found during reload completion");
                Cancel(playerController);
                return;
            }

            var energyBottle = energyBottles[0];
            var crystalCore = playerController.Stats.crystalCore;
            float energyBefore = crystalCore.CurrentEnergy;

            // Use the EnergyBottle through ConsumableManager
            bool success = consumableManager.UseEnergyBottle(energyBottle);
            
            if (success)
            {
                float energyAfter = crystalCore.CurrentEnergy;
                float energyRestored = energyAfter - energyBefore;
                Debug.Log($"PlayerReloadAction: Successfully consumed EnergyBottle, restored {energyRestored} energy");
                Debug.Log($"PlayerReloadAction: Crystal Core Energy: {energyAfter}/{crystalCore.MaxEnergy}");
                
                // Play reload complete audio
                PlayReloadCompleteAudio(playerController);
            }
            else
            {
                Debug.LogWarning("PlayerReloadAction: Failed to consume EnergyBottle");
            }

            _isActive = false;
            IsFinished = true;
        }

        private void PlayReloadStartAudio(PlayerController playerController)
        {
            if (_audioService == null) return;

            // Play energy reload start sound - using item pickup sound
            _audioService.PlaySFX2D(AudioClipType.ItemPickup, 0.7f, 0.9f);
        }

        private void PlayReloadCompleteAudio(PlayerController playerController)
        {
            if (_audioService == null) return;

            // Play energy reload complete sound - using crystal core charge sound
            _audioService.PlaySFX2D(AudioClipType.WeaponReloadPistol, 0.8f, 1f);
        }

        private void PlayReloadCancelAudio(PlayerController playerController)
        {
            if (_audioService == null) return;

            // Play reload cancel sound - using lower volume
            _audioService.PlaySFX2D(AudioClipType.UIButtonClick, 0.4f, 1f);
        }
    }
}
