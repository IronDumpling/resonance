using System;
using UnityEngine;

namespace Resonance.Shared.Interfaces.Services
{
    public interface IInputService : IGameService
    {
        event Action<Vector2> OnMove;
        event Action OnInteract;
        event Action OnWaveAttack;
        event Action<bool> OnHeal;
        event Action<bool> OnRun; // true when starting to run, false when stopping
        event Action<bool> OnAim; // true when starting to aim, false when stopping
        event Action OnShoot;
        event Action<Vector2> OnLook;
        event Action OnReload; // Reload input (R key)
        event Action OnOpenInventory; // Open inventory (Player map Tab key)

        event Action OnCloseInventory; // Close inventory (Inventory map Tab key)
        event Action<Vector2> OnMoveItem;
        event Action OnRotateItemLeft;
        event Action OnRotateItemRight;

        event Action OnInformationClose; // Close information (E key)
        event Action OnNextPage; // Next page input during Information mode
        event Action OnLastPage; // Last page input during Information mode

        event Action OnAttackQTE; // QTE input during Wave mode
        event Action OnLogicBind1; // Logic bind 1 input during Wave mode

        void EnableInventoryInput();
        void DisableInventoryInput();
        void EnablePlayerInput();
        void DisablePlayerInput();
        void EnableInformationInput();
        void DisableInformationInput();
        void EnableWaveInput();
        void DisableWaveInput();
    }
}
