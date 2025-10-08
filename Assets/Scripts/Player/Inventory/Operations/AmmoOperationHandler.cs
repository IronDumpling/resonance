using UnityEngine;
using Resonance.Interfaces.Operations;

namespace Resonance.Player.Inventory.Operations
{
    public class AmmoOperationHandler : BaseItemOperationHandler, IItemCombinable, IItemDroppable
    {
        public void Combine()
        {
            Debug.Log("AmmoOperationHandler: Combine");
        }

        public void Drop()
        {
            Debug.Log("AmmoOperationHandler: Drop");
        }
    }
}