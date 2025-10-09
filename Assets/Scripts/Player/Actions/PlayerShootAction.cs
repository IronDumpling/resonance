using Resonance.Core;
using Resonance.Utilities;
using Resonance.Player.Core;
using Resonance.Interfaces.Objects;
using Resonance.Interfaces.Services;

namespace Resonance.Player.Actions
{
    public class PlayerShootAction : IPlayerAction
    {
        public string Name => "Shoot";
        public bool BlocksMovement => true;
        public bool ProvidesInvulnerability => false;
        public bool CanInterrupt => true;

        // Runtime state
        private bool _isActive = false;
        private bool _isFinished = false;
        private float _actionStartTime = 0f;

        public bool IsFinished => _isFinished;
        
        public bool CanStart(PlayerController player)
        {
            return player.IsAlive && player.CurrentState == "Normal";
        }
        
        public void Start(PlayerController player)
        {
            _isFinished = false;
        }

        public void Update(PlayerController player, float deltaTime)
        {

        }

        public void Cancel(PlayerController player)
        {

        }
        
        public void OnDamageTaken(PlayerController player)
        {

        }
    }
}