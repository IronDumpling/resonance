using UnityEngine;
using Resonance.Player;

namespace Resonance.Player.Triggers
{
    public class PlayerCrystalCoreHitbox : MonoBehaviour
    {
        private PlayerMonoBehaviour _playerMono;
        
        public void Initialize(PlayerMonoBehaviour playerMono)
        {
            _playerMono = playerMono;
        }
    }
}