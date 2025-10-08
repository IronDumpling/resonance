using UnityEngine;
using Resonance.Interfaces.Operations;

namespace Resonance.Player.Inventory.Operations
{
    public class WeaponOperationHandler : BaseItemOperationHandler, IItemUsable, IItemCombinable
    {
        public void Use()
        {
            Debug.Log("WeaponOperationHandler: Use");
        }

        public void Combine()
        {
            Debug.Log("WeaponOperationHandler: Combine");
        }
    }
}