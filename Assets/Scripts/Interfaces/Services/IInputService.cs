using System;
using UnityEngine;

namespace Resonance.Core
{
    public interface IInputService : IGameService
    {
        event Action<Vector2> OnMove;
        event Action OnJump;
        event Action OnInteract;

        bool IsEnabled { get; set; }
        void EnablePlayerInput();
        void DisablePlayerInput();
        void EnableUIInput();
        void DisableUIInput();
    }
}
