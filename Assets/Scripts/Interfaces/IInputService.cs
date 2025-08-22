using System;
using UnityEngine;

namespace Resonance.Core
{
    public interface IInputService : IGameSystem
    {
        event Action<Vector2> OnMove;
        event Action<Vector2> OnLook;
        event Action OnAttack;
        event Action OnJump;
        event Action OnCrouch;
        event Action OnSprint;
        event Action OnInteract;
        event Action OnNext;
        event Action OnPrevious;

        bool IsEnabled { get; set; }
        void EnablePlayerInput();
        void DisablePlayerInput();
        void EnableUIInput();
        void DisableUIInput();
    }
}
