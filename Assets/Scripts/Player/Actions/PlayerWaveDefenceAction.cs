using UnityEngine;
using Resonance.Player.Core;
using Resonance.Interfaces.Operations;

namespace Resonance.Player.Actions
{
    public class PlayerWaveDefenceAction : IPlayerAction
    {
        private bool _isFinished = false;

        public string Name => "WaveDefence";
        public bool BlocksMovement => true;
        public bool ProvidesInvulnerability => true;
        public bool CanInterrupt => false;
        public bool IsFinished => _isFinished;

        public bool CanStart(PlayerController player)
        {
            return true;
        }

        public void Start(PlayerController player)
        {
            _isFinished = true;
        }

        public void Update(PlayerController player, float deltaTime)
        {
            _isFinished = true;
        }

        public void Cancel(PlayerController player)
        {
            _isFinished = true;
        }

        public void OnDamageTaken(PlayerController player)
        {
            _isFinished = true;
        }
    }
}