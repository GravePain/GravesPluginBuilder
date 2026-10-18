using System;
using System.Collections.Generic;
using Graves.Core;
using Graves.Plugins.Inventory;
using UnityEngine;

namespace Graves.Plugins.Equipment
{
    /// <summary>
    /// What an entity is wearing, and the act of putting things on and taking them off.
    /// </summary>
    /// <remarks>
    /// <b>Equipping moves an object rather than copying it.</b> The instance leaves the inventory
    /// and lives here until it is taken off. Anything else means two references to one unique sword,
    /// and the first time they disagree the player has either lost it or gained a second.
    ///
    /// <b>It raises events and computes nothing.</b> What a worn item does to a stat is not decided
    /// here, and neither is what it looks like. Both are separate concerns living in separate
    /// assemblies, which is why a game can equip things without owning a combat system.
    /// </remarks>
    [DisallowMultipleComponent]
    public class Equipment : MonoBehaviour
    {
        private EquipmentState _state;
        private EquipmentState _loose = new EquipmentState();
        private readonly List<EquipmentSlotDefinition> _slots = new List<EquipmentSlotDefinition>();

        /// <summary>Raised after something is put on, carrying the slot and the item.</summary>
        public event Action<EquipmentSlotDefinition, GameInstance> Equipped;

        /// <summary>Raised after something is taken off, carrying the slot and the item.</summary>
        public event Action<EquipmentSlotDefinition, GameInstance> Unequipped;

        /// <summary>The slots this entity has.</summary>
        public IReadOnlyList<EquipmentSlotDefinition> Slots => _slots;

        /// <summary>Everything currently worn.</summary>
        public IReadOnlyList<EquippedEntry> Worn => Storage.Worn;

        private EquipmentState Storage => _state ?? _loose;

        /// <summary>Applies the authored slots.</summary>
        public void Configure(IEnumerable<EquipmentSlotDefinition> _available)
        {
            _slots.Clear();
            if (_available == null) return;

            foreach (var slot in _available)
            {
                if (slot == null) continue;
                if (_slots.Contains(slot)) continue;
                _slots.Add(slot);
            }
        }

        /// <summary>Hands this component its storage, so a save reads and writes what it reads.</summary>
        public void Bind(EquipmentState _storage) => _state = _storage;

        /// <summary>The first thing worn in a slot, or null.</summary>
        public GameInstance InSlot(EquipmentSlotDefinition _slot)
            => _slot == null ? null : Storage.FirstIn(_slot.Uid);

        /// <summary>True when this entity has the given slot at all.</summary>
        public bool HasSlot(EquipmentSlotDefinition _slot) => _slot != null && _slots.Contains(_slot);

        /// <summary>
        /// True when something is worn in the named slot. The question a skill asks before it lets
        /// itself be used.
        /// </summary>
        public bool IsOccupied(EquipmentSlotDefinition _slot)
            => _slot != null && Storage.CountIn(_slot.Uid) > 0;

        /// <summary>
        /// Puts an item on, taking it out of the inventory it came from.
        /// </summary>
        /// <remarks>
        /// Every refusal is reported by name rather than returned as a bare false, because an item
        /// that will not equip and says nothing is indistinguishable from a broken button.
        ///
        /// A full slot is swapped rather than refused: the old item goes back to the inventory and
        /// the new one goes on. That is what a player expects from dropping a sword onto an occupied
        /// hand, and refusing would mean two clicks for every weapon change forever.
        /// </remarks>
        public bool Equip(GameInstance _item, InventoryState _from)
        {
            if (_item == null) return false;

            var definition = _item.DefinitionAs<ItemDefinition>();
            if (definition == null)
            {
                Debug.LogWarning("[Equipment] '" + name + "' was asked to equip an item whose " +
                                 "definition no longer exists (" + _item.DefinitionUid + ").", this);
                return false;
            }

            var equippable = definition.GetModule<EquippableModule>();
            if (equippable == null)
            {
                Debug.LogWarning("[Equipment] '" + definition.DisplayName + "' is not equippable - it " +
                                 "has no equippable module.", this);
                return false;
            }

            var slot = equippable.Slot;
            if (slot == null)
            {
                Debug.LogError("[Equipment] '" + definition.DisplayName + "' is marked equippable but " +
                               "names no slot, so there is nowhere to put it.", this);
                return false;
            }

            if (!HasSlot(slot))
            {
                Debug.LogWarning("[Equipment] '" + name + "' has no '" + slot.DisplayName + "' slot, " +
                                 "so '" + definition.DisplayName + "' cannot be worn.", this);
                return false;
            }

            // Full slot: take the oldest occupant off first so the new item has room. Done before
            // removing the new item from the inventory, so a failure leaves everything where it was.
            while (Storage.CountIn(slot.Uid) >= slot.Capacity)
            {
                var displaced = Storage.FirstIn(slot.Uid);
                if (displaced == null) break;
                if (!Unequip(displaced, _from)) return false;
            }

            if (_from != null && !_from.RemoveInstance(_item.InstanceId))
            {
                Debug.LogWarning("[Equipment] '" + definition.DisplayName + "' was not in the " +
                                 "inventory it was said to come from, so nothing was equipped.", this);
                return false;
            }

            Storage.Wear(slot.Uid, _item);
            Equipped?.Invoke(slot, _item);
            return true;
        }

        /// <summary>
        /// Takes an item off and puts it back in an inventory.
        /// </summary>
        /// <remarks>
        /// Refuses when the inventory is full, and leaves the item worn. The alternative is dropping
        /// it on the floor or destroying it, and a framework that silently destroys a player's sword
        /// because a bag was full has no business shipping.
        /// </remarks>
        public bool Unequip(GameInstance _item, InventoryState _into)
        {
            if (_item == null) return false;

            var slot = SlotOf(_item);

            if (_into != null)
            {
                if (_into.IsFull)
                {
                    Debug.LogWarning("[Equipment] '" + name + "' cannot take something off because " +
                                     "there is no room to put it. It stays equipped.", this);
                    return false;
                }

                if (_into.Add(_item) <= 0)
                {
                    Debug.LogWarning("[Equipment] '" + name + "' could not return an item to the " +
                                     "inventory. It stays equipped.", this);
                    return false;
                }
            }

            var removed = Storage.TakeOff(_item.InstanceId);
            if (removed == null)
            {
                // Already put back in the inventory, so removing it again would duplicate it. This
                // cannot happen through the normal path and is reported rather than patched over.
                Debug.LogError("[Equipment] '" + name + "' returned an item to the inventory that it " +
                               "was not actually wearing. The inventory now holds a copy.", this);
                return false;
            }

            Unequipped?.Invoke(slot, removed);
            return true;
        }

        /// <summary>The slot an item is worn in, or null when it is not worn.</summary>
        public EquipmentSlotDefinition SlotOf(GameInstance _item)
        {
            if (_item == null) return null;

            foreach (var entry in Storage.Worn)
            {
                if (entry?.Item == null) continue;
                if (entry.Item.InstanceId != _item.InstanceId) continue;

                for (int i = 0; i < _slots.Count; i++)
                {
                    if (_slots[i] != null && _slots[i].Uid == entry.SlotUid) return _slots[i];
                }
                return null;
            }
            return null;
        }
    }
}
