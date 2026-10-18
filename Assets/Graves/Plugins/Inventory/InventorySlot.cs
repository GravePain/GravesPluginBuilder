using System;
using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Inventory
{
    /// <summary>
    /// One place in an inventory. It always exists; it may or may not have anything in it.
    /// </summary>
    /// <remarks>
    /// <b>An empty slot is a slot, not an absence.</b> The first version of this removed empty slots
    /// and compacted the list, which made the inventory a list wearing a grid's clothes: use one
    /// potion from the third slot and everything after it shuffles up. A player who put something
    /// somewhere expects it to still be there.
    ///
    /// Found by comparison against the reference framework, whose <c>ItemSlot</c> is a struct where <c>amount == 0</c>
    /// means empty-but-present, and whose inventory is a fixed-length list. Same conclusion, and they
    /// reached it first.
    ///
    /// <b>One instance plus a count, not a list of instances.</b> Twenty arrows are one entry with a
    /// count of twenty, because twenty identical arrows have nothing to tell apart. The moment an item
    /// has something to tell apart — durability, an enchantment, any per-instance state — it stops
    /// being stackable and takes a slot of its own. See <see cref="InventoryState.CanStack"/>.
    /// </remarks>
    [Serializable]
    public class InventorySlot
    {
        [SerializeReference]
        [Tooltip("The item, or nothing. Serialized by reference so its per-instance state survives.")]
        private GameInstance _item;

        [SerializeField]
        [Tooltip("How many are here. Zero means the slot exists and is empty.")]
        private int _count;

        /// <summary>Creates an empty slot.</summary>
        public InventorySlot() { }

        /// <summary>Creates a slot holding an instance.</summary>
        public InventorySlot(GameInstance _instance, int _quantity)
        {
            _item = _instance;
            _count = Mathf.Max(0, _quantity);
            if (_count == 0) _item = null;
        }

        /// <summary>The item here, or null when the slot is empty.</summary>
        public GameInstance Item => _item;

        /// <summary>How many are here. Zero when empty.</summary>
        public int Count => _count;

        /// <summary>True when nothing is in this slot.</summary>
        public bool IsEmpty => _item == null || _count <= 0;

        /// <summary>The item's definition, or null when empty or unresolvable.</summary>
        public ItemDefinition Definition => _item != null ? _item.DefinitionAs<ItemDefinition>() : null;

        /// <summary>True when occupied by an item that still resolves to a definition.</summary>
        public bool IsValid => _item != null && _item.IsValid;

        /// <summary>Puts an instance here, replacing whatever was here.</summary>
        public void Set(GameInstance _instance, int _quantity)
        {
            if (_instance == null || _quantity <= 0) { Clear(); return; }
            _item = _instance;
            _count = _quantity;
        }

        /// <summary>Empties the slot, leaving it in place.</summary>
        public void Clear()
        {
            _item = null;
            _count = 0;
        }

        /// <summary>Adds to the count and returns the new total.</summary>
        public int Add(int _quantity)
        {
            if (_item == null) return 0;
            _count = Mathf.Max(0, _count + Mathf.Max(0, _quantity));
            return _count;
        }

        /// <summary>
        /// Removes up to <paramref name="_quantity"/> and returns how many were actually removed.
        /// </summary>
        /// <remarks>
        /// Returns the amount removed rather than a success flag, so a caller taking five from a slot
        /// of three learns it got three. A boolean would force the caller to check the count first and
        /// then act, which is two chances to get it wrong.
        ///
        /// Emptying a slot clears it in place. It does not disappear.
        /// </remarks>
        public int Remove(int _quantity)
        {
            var taken = Mathf.Clamp(_quantity, 0, _count);
            _count -= taken;
            if (_count <= 0) Clear();
            return taken;
        }

        /// <summary>Exchanges contents with another slot.</summary>
        public void SwapWith(InventorySlot _other)
        {
            if (_other == null || ReferenceEquals(_other, this)) return;

            var item = _other._item;
            var count = _other._count;

            _other._item = _item;
            _other._count = _count;

            _item = item;
            _count = count;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (IsEmpty) return "<empty>";
            return _count > 1 ? _item + " x" + _count : _item.ToString();
        }
    }
}
