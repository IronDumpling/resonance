using UnityEngine;
using Resonance.Core;
using Resonance.Utilities;
using Resonance.Player.Core;
using Resonance.Interfaces.Services;

namespace Resonance.Player.States
{
    /// <summary>
    /// Aiming state where player can move slowly, look around, and shoot.
    /// Integrates with WeaponAccuracySystem and WeaponRecoilSystem.
    /// Cannot interact with objects while aiming.
    /// </summary>
    public class PlayerAimingState : IState
    {
        private PlayerController _playerController;
        private Vector3 _shootOrigin;
        private IUIService _uiService;
        
        public string Name => "Aiming";

        public PlayerAimingState(PlayerController playerController)
        {
            _playerController = playerController;
            _uiService = ServiceRegistry.Get<IUIService>();
        }

        public void Enter()
        {
            Debug.Log("PlayerState: Entered Aiming state");
            
            // Safety check: Ensure player has a weapon
            if (!_playerController.HasEquippedWeapon)
            {
                Debug.LogWarning("PlayerAimingState: Player entered aiming state without weapon! This should not happen.");
                return;
            }
            
            // Initialize weapon systems in ShootingSystem
            if (_playerController.ShootingSystem != null)
            {
                _playerController.ShootingSystem.InitializeWeapon(_playerController.WeaponManager.CurrentWeapon);
                Debug.Log("PlayerAimingState: Weapon systems initialized");
            }
            
            // Calculate shoot origin (player position + height offset)
            _shootOrigin = _playerController.PlayerGameObject.transform.position + Vector3.up * 1.5f;
            
            // Show crosshair UI
            ShowCrosshairUI();
        }

        public void Update()
        {
            // Safety check: If weapon is removed while aiming, exit aiming state
            if (!_playerController.HasEquippedWeapon)
            {
                Debug.Log("PlayerAimingState: Weapon removed while aiming, exiting aiming state");
                _playerController.StateMachine.StopAiming();
                return;
            }
            
            // Update shoot origin
            _shootOrigin = _playerController.PlayerGameObject.transform.position + Vector3.up * 1.5f;
            
            // Update weapon systems (accuracy and recoil)
            if (_playerController.ShootingSystem != null)
            {
                _playerController.ShootingSystem.UpdateWeaponSystems(
                    Time.deltaTime, 
                    isAiming: true, 
                    _playerController.Movement.IsMoving);
                
                // Update aiming line visualization
                _playerController.ShootingSystem.UpdateAimingLine(_shootOrigin);
            }
            
            // Handle shooting input (left mouse button)
            if (UnityEngine.InputSystem.Mouse.current != null && 
                UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryShoot();
            }
            
            // Handle exit aiming input (right mouse button released)
            if (UnityEngine.InputSystem.Mouse.current != null && 
                UnityEngine.InputSystem.Mouse.current.rightButton.wasReleasedThisFrame)
            {
                _playerController.StateMachine.StopAiming();
            }
        }

        public void Exit()
        {
            Debug.Log("PlayerState: Exited Aiming state");
            
            // Cleanup weapon systems
            if (_playerController.ShootingSystem != null)
            {
                _playerController.ShootingSystem.CleanupWeapon();
                _playerController.ShootingSystem.HideAimingLine();
                Debug.Log("PlayerAimingState: Weapon systems cleaned up");
            }
            
            // Hide crosshair UI
            HideCrosshairUI();
        }

        public bool CanTransitionTo(IState newState)
        {
            // Can transition back to Normal or to death/stun states
            return newState.Name == "Normal" || 
                   newState.Name == "Stun" || 
                   newState.Name == "Death";
        }
        
        /// <summary>
        /// Try to shoot if conditions are met
        /// </summary>
        private void TryShoot()
        {
            // Use PlayerController.PerformShoot() which handles all the logic
            // including ammo consumption, events, and ShootingSystem integration
            var result = _playerController.PerformShoot(_shootOrigin);
            
            if (result.success)
            {
                Debug.Log($"PlayerAimingState: Shot fired! Damage: {result.GetTotalActualDamage():F1}, Hit: {result.hasHit}");
            }
            else
            {
                Debug.Log("PlayerAimingState: Shot failed - conditions not met");
            }
        }
        
        /// <summary>
        /// Show crosshair UI using UIService
        /// </summary>
        private void ShowCrosshairUI()
        {
            if (_uiService != null)
            {
                _uiService.ShowPanel("CrosshairPanel");
                Debug.Log("PlayerAimingState: Crosshair UI shown");
            }
            else
            {
                Debug.LogWarning("PlayerAimingState: UIService not found, cannot show crosshair UI");
            }
        }
        
        /// <summary>
        /// Hide crosshair UI using UIService
        /// </summary>
        private void HideCrosshairUI()
        {
            if (_uiService != null)
            {
                _uiService.HidePanel("CrosshairPanel");
                Debug.Log("PlayerAimingState: Crosshair UI hidden");
            }
            else
            {
                Debug.LogWarning("PlayerAimingState: UIService not found, cannot hide crosshair UI");
            }
        }
    }
}
