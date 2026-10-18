using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Inventory
{
    /// <summary>
    /// Marks an item definition as stackable, and says how high a stack may go.
    /// </summary>
    /// <remarks>
    /// Attached to an <c>ItemDefinition</c>. An item with no such module never stacks, which is the
    /// safer default: a thing that should have stacked and did not is a mild annoyance, whereas two
    /// distinct objects merged into one is data loss.
    ///
    /// <b>This module gives permission; it does not decide.</b> The inventory also refuses to stack
    /// anything carrying per-instance state, however this is configured — see
    /// <see cref="InventoryState.CanStack"/>. A sword marked stackable by accident still cannot merge
    /// with another sword once either has durability, because they are no longer interchangeable.
    /// </remarks>
    [CreateAssetMenu(fileName = "StackableModule", menuName = "Graves/Modules/Stackable")]
    public class StackableModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("Most that may sit in one slot. One means it occupies a slot but never merges.")]
        [Min(1)] private int _maxStack = 99;

        /// <summary>Largest number of this item permitted in a single slot.</summary>
        public int MaxStack => Mathf.Max(1, _maxStack);
    }
}
