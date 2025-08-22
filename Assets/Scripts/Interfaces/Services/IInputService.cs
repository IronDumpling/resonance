using System;
using UnityEngine;

namespace Resonance.Core
{
    public interface IInputService : IGameService
    {
        event Action<Vector2> OnMove;
        event Action OnInteract;
        event Action<bool> OnRun; // true when starting to run, false when stopping
        event Action<bool> OnAim; // true when starting to aim, false when stopping
        event Action OnShoot;
        event Action<Vector2> OnLook;

        bool IsEnabled { get; set; }
        void EnablePlayerInput();
        void DisablePlayerInput();
        void EnableUIInput();
        void DisableUIInput();
    }
}
