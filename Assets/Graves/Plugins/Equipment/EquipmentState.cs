using System;
using System.Collections.Generic;
using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Equipment
{
    /// <summary>
    /// One occupied place on a body: which slot, and which particular object is in it.
    /// </summary>
    /// <remarks>
    /// The slot is stored as a uid rather than as a reference, so renaming or moving the slot asset
    /// cannot orphan a save. The item is a whole instance rather than an item uid, because what is
    /// worn is a particular sword with its own history, not a kind of sword.
    /// </remarks>
    [Serializable]
    public class EquippedEntry
    {
        [Tooltip("The permanent id of the slot this is worn in.")]
        public string SlotUid;

        [SerializeReference]
        [Tooltip("The actual object worn here, with all of its own state.")]
        public GameInstance Item;
    }

    /// <summary>
    /// What this particular thing is wearing.
    /// </summary>
    /// <remarks>
    /// <b>Equipment owns the instances it holds, exactly as an inventory does.</b> Equipping takes
    /// the object out of the inventory and puts it here; unequipping does the reverse. Copying it
    /// into both would give a player two of a unique sword the first time anything went wrong, and
    /// leaving it in the inventory while worn means every inventory operation has to know which
    /// entries are secretly spoken for.
    ///
    /// <b>This is the storage, not a copy of it.</b> The equipment component reads and writes
    /// through this object so that a load works with nothing being notified. See
    /// <c>Agent Memory/decisions/F-002-state-is-the-storage.md</c> for the load that reported six
    /// entities restored and changed nothing.
    /// </remarks>
    [Serializable]
    public class EquipmentState : InstanceState
    {
        [SerializeField]
        [Tooltip("Everything currently worn. An empty slot is simply absent rather than present " +
                 "and blank, because unlike an inventory grid there is nothing positional to keep.")]
        private List<EquippedEntry> _worn = new List<EquippedEntry>();

        /// <summary>Everything currently worn.</summary>
        public IReadOnlyList<EquippedEntry> Worn => _worn;

        /// <summary>
        /// A stable name for this state in a save file, written as a literal so a rename cannot
        /// orphan an existing save.
        /// </summary>
        public override string StateKey => "graves.equipment";

        /// <summary>How many things are worn in a given slot.</summary>
        public int CountIn(string _slotUid)
        {
            if (string.IsNullOrEmpty(_slotUid)) return 0;

            int count = 0;
            for (int i = 0; i < _worn.Count; i++)
            {
                if (_worn[i] != null && _worn[i].SlotUid == _slotUid && _worn[i].Item != null) count++;
            }
            return count;
        }

        /// <summary>The first thing worn in a slot, or null.</summary>
        public GameInstance FirstIn(string _slotUid)
        {
            if (string.IsNullOrEmpty(_slotUid)) return null;

            for (int i = 0; i < _worn.Count; i++)
            {
                if (_worn[i] != null && _worn[i].SlotUid == _slotUid && _worn[i].Item != null)
                    return _worn[i].Item;
            }
            return null;
        }

        /// <summary>Records something as worn in a slot.</summary>
        public void Wear(string _slotUid, GameInstance _item)
        {
            if (string.IsNullOrEmpty(_slotUid) || _item == null) return;
            _worn.Add(new EquippedEntry { SlotUid = _slotUid, Item = _item });
        }

        /// <summary>
        /// Removes a worn item by its instance id and returns it, or null when it was not worn.
        /// </summary>
        /// <remarks>
        /// By instance id rather than by slot, because that is the question a caller actually has:
        /// take this specific sword off. Removing "whatever is in the main hand" is ambiguous the
        /// moment a slot holds two things.
        /// </remarks>
        public GameInstance TakeOff(string _instanceId)
        {
            if (string.IsNullOrEmpty(_instanceId)) return null;

            for (int i = 0; i < _worn.Count; i++)
            {
                var entry = _worn[i];
                if (entry == null || entry.Item == null) continue;
                if (entry.Item.InstanceId != _instanceId) continue;

                var item = entry.Item;
                _worn.RemoveAt(i);
                return item;
            }
            return null;
        }

        /// <summary>
        /// Reports anything wrong: an entry with no slot, no item, or an item whose definition has
        /// been deleted.
        /// </summary>
        /// <remarks>
        /// Reports rather than repairs, in keeping with the rest of the framework. A worn item whose
        /// definition was deleted stays worn and is named, because throwing it away would be a
        /// silent, permanent loss of something the player earned.
        /// </remarks>
        public List<string> Validate()
        {
            var problems = new List<string>();

            for (int i = 0; i < _worn.Count; i++)
            {
                var entry = _worn[i];
                if (entry == null) { problems.Add("Entry " + i + " is null."); continue; }
                if (string.IsNullOrEmpty(entry.SlotUid)) problems.Add("Entry " + i + " names no slot.");
                if (entry.Item == null) { problems.Add("Entry " + i + " holds nothing."); continue; }
                if (!entry.Item.IsValid)
                    problems.Add("Entry " + i + " holds an item whose definition no longer exists (" +
                                 entry.Item.DefinitionUid + ").");
            }

            return problems;
        }
    }
}
