using System;
using UnityEngine;

namespace Resonance.Interfaces.Services
{
    public interface IInputService : IGameService
    {
        event Action<Vector2> OnMove;
        event Action OnInteract;
        event Action OnWave;
        event Action<bool> OnHeal;
        event Action<bool> OnRun; // true when starting to run, false when stopping
        event Action<bool> OnAim; // true when starting to aim, false when stopping
        event Action OnShoot;
        event Action<Vector2> OnLook;
        event Action OnQTE; // QTE input during Wave mode
        event Action OnReload; // Reload input (R key)

        bool IsEnabled { get; set; }
        bool IsWaveMode { get; set; } // Control input mode switching
        void EnablePlayerInput();
        void DisablePlayerInput();
        void EnableUIInput();
        void DisableUIInput();
    }
}
