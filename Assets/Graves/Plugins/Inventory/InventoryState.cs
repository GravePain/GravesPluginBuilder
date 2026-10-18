using System;
using System.Collections.Generic;
using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Inventory
{
    /// <summary>
    /// What an entity is actually carrying. Per-instance state, so two goblins of the same definition
    /// have different pockets.
    /// </summary>
    /// <remarks>
    /// <b>Slots have positions and empty slots stay put.</b> The list is the inventory's shape, not a
    /// list of what happens to be held. Slot three is slot three whether or not anything is in it, so
    /// using one potion does not shuffle everything after it.
    ///
    /// <b>Contents are instances, not definitions.</b> A bag holds particular swords, each with its
    /// own identity and its own state, which is the entire reason instances exist. An inventory of
    /// definitions could not tell a worn sword from a new one.
    ///
    /// <b>Nothing here refuses to hold a broken item.</b> An item whose definition was deleted stays
    /// in the bag, invalid and reportable, because dropping it would destroy the player's property to
    /// tidy up a database. Compare the reference framework, which throws <c>KeyNotFoundException</c> on access to an
    /// item whose ScriptableItem is gone — a deleted item takes the load with it.
    /// </remarks>
    [Serializable]
    public class InventoryState : InstanceState
    {
        [SerializeField]
        private List<InventorySlot> _slots = new List<InventorySlot>();

        [SerializeField]
        private int _capacity;

        [SerializeField]
        private bool _seeded;

        /// <inheritdoc />
        /// <summary>The key an ordinary carried bag is stored under.</summary>
        /// <remarks>
        /// Named as a constant so that a second container of the same kind cannot be found by asking
        /// for this one. A purse and a bag are both inventories, and the typed lookup would return
        /// whichever module happened to attach first.
        /// </remarks>
        public const string Key = "inventory";

        /// <inheritdoc />
        public override string StateKey => Key;

        /// <summary>Every slot, occupied or not, in position order.</summary>
        public IReadOnlyList<InventorySlot> Slots => _slots;

        /// <summary>
        /// How many slots exist. Zero or less means the inventory grows as needed.
        /// </summary>
        /// <remarks>
        /// Setting this resizes immediately. Shrinking never discards an occupied slot - the
        /// inventory keeps the extra slots and reports being over capacity instead, because silently
        /// destroying carried items to satisfy a number is exactly the kind of tidying this framework
        /// refuses to do.
        /// </remarks>
        public int Capacity
        {
            get => _capacity;
            set { _capacity = value; EnsureSize(); }
        }

        /// <summary>True once starting contents have been granted, so they are never granted twice.</summary>
        /// <remarks>
        /// Serialized deliberately. Without it, loading a save re-runs the module's attach and hands
        /// the player their starting gear again, on top of whatever they had — every load, forever.
        /// The bug looks like duplication rather than like a save fault, so it tends to get chased in
        /// the wrong place.
        /// </remarks>
        public bool Seeded
        {
            get => _seeded;
            set => _seeded = value;
        }

        /// <summary>Number of slots with something in them.</summary>
        public int UsedSlots
        {
            get
            {
                var used = 0;
                foreach (var slot in _slots) if (!slot.IsEmpty) used++;
                return used;
            }
        }

        /// <summary>True when no slot is free and no more can be added.</summary>
        public bool IsFull
        {
            get
            {
                foreach (var slot in _slots) if (slot.IsEmpty) return false;
                return _capacity > 0 && _slots.Count >= _capacity;
            }
        }

        /// <summary>True when occupied slots exceed the stated capacity.</summary>
        public bool IsOverCapacity => _capacity > 0 && _slots.Count > _capacity;

        /// <summary>
        /// Decides whether an instance may join an existing stack.
        /// </summary>
        /// <remarks>
        /// <b>Two conditions, and the second is the one that matters.</b> The definition must permit
        /// stacking via <see cref="StackableModule"/>, AND neither instance may carry any per-instance
        /// state.
        ///
        /// State is what makes one object distinguishable from another. Merging two objects that can
        /// be told apart destroys one of them — silently, and in a way that only shows up when someone
        /// notices their enchantment is missing. So the rule is derived from the data rather than
        /// trusted to a flag: mark swords stackable by accident and they still will not merge once
        /// either has durability.
        ///
        /// The reference framework reached the same conclusion independently, in a comment on its <c>Item</c> struct:
        /// "Comparing the name is NOT enough for cases where dynamic stats differ. E.g. two pets with
        /// different levels shouldn't be merged." Their rule is enforced by a note telling authors not
        /// to give durability to a stackable item; this one cannot be got wrong by mis-authoring.
        /// </remarks>
        public static bool CanStack(GameInstance _a, GameInstance _b)
        {
            if (_a == null || _b == null) return false;
            if (_a.DefinitionUid != _b.DefinitionUid) return false;

            // Anything that can be told apart must not be merged. The core answers that - it owns
            // identity, and crafting, shops and quests ask the same question without wanting a bag.
            if (!_a.IsPlain || !_b.IsPlain) return false;

            var definition = _a.Definition;
            return definition != null && definition.HasModule<StackableModule>();
        }

        /// <summary>Largest stack permitted for an instance, or one when it does not stack.</summary>
        public static int MaxStackFor(GameInstance _instance)
        {
            var definition = _instance != null ? _instance.Definition : null;
            var stackable = definition != null ? definition.GetModule<StackableModule>() : null;
            return stackable != null ? stackable.MaxStack : 1;
        }

        /// <summary>
        /// Puts an instance in, stacking where permitted and otherwise taking the first free slot.
        /// </summary>
        /// <returns>How many were actually stored. Less than asked for means the bag filled up.</returns>
        public int Add(GameInstance _instance, int _quantity = 1)
        {
            if (_instance == null || _quantity <= 0) return 0;

            EnsureSize();

            var remaining = _quantity;
            var maxStack = MaxStackFor(_instance);
            var placedOriginal = false;

            if (maxStack > 1)
            {
                for (int i = 0; i < _slots.Count && remaining > 0; i++)
                {
                    var slot = _slots[i];
                    if (slot.IsEmpty || !CanStack(slot.Item, _instance)) continue;

                    var room = maxStack - slot.Count;
                    if (room <= 0) continue;

                    var moved = Mathf.Min(room, remaining);
                    slot.Add(moved);
                    remaining -= moved;
                }
            }

            while (remaining > 0)
            {
                var free = FirstEmptyIndex();
                if (free < 0) break;

                var portion = Mathf.Min(maxStack, remaining);

                // A fresh identity per new slot. Reusing one instance across two slots would give two
                // separate objects the same identity, which is the same failure as two definitions
                // sharing a uid - and it would make a save restore one of them onto the other.
                var occupant = placedOriginal ? _instance.ShallowCopy() : _instance;
                placedOriginal = true;

                _slots[free].Set(occupant, portion);
                remaining -= portion;
            }

            return _quantity - remaining;
        }

        /// <summary>
        /// Moves the contents of one slot onto another: merging, swapping, or moving into empty.
        /// </summary>
        /// <remarks>
        /// This is the operation a drag between two slots performs, and it is why slots need
        /// positions at all. Merge when the two stack, move when the target is empty, otherwise swap -
        /// a swap being the behaviour that makes dragging feel right, because nothing is ever
        /// destroyed or bounced back.
        /// </remarks>
        /// <returns>False when either index is out of range or the move is a no-op.</returns>
        /// <summary>
        /// Takes some of a stack out into a slot of its own, giving what leaves a fresh identity.
        /// Returns the new instance, or null when the split could not happen.
        /// </summary>
        /// <remarks>
        /// <b>This exists because the framework could describe an enhanced arrow and could not make
        /// one.</b> Twenty arrows are one instance with a count beside it, because twenty identical
        /// arrows have nothing to tell apart. The moment one of them is enhanced they do differ, and
        /// the stacking rule already refuses to merge anything carrying state - so the enhanced one
        /// must become its own object. Nothing could perform that transition until now.
        ///
        /// <b>The new instance gets a new id, and that is the whole point.</b> The stack's identity
        /// stands for all of them; handing it to one arrow would give two different things the same
        /// identity, which is the failure the uid design exists to prevent and would make a save
        /// restore one onto the other.
        ///
        /// <b>On the shallow copy, which looks like a landmine here and is not.</b> Copying an
        /// instance duplicates the state LIST and not the state objects in it, so a split followed by
        /// a change would ordinarily alter both. It cannot here: a slot holding more than one thing
        /// can only hold things with no state at all, because that is what stacking requires. The
        /// list being copied is always empty, so there is nothing to share.
        ///
        /// That is a guarantee resting on another rule rather than on this code, so it is checked
        /// rather than trusted - a stack that somehow holds state is a broken invariant elsewhere and
        /// is reported instead of quietly producing two arrows that sharpen together.
        /// </remarks>
        public GameInstance Split(int _slotIndex, int _quantity = 1)
        {
            if (_slotIndex < 0 || _slotIndex >= _slots.Count) return null;
            if (_quantity <= 0) return null;

            var slot = _slots[_slotIndex];
            if (slot == null || slot.IsEmpty) return null;

            // Leaving nothing behind is a move, not a split, and the caller almost certainly meant
            // one of them rather than either.
            if (_quantity >= slot.Count) return null;

            if (!slot.Item.IsPlain)
            {
                Debug.LogError("[Inventory] Slot " + _slotIndex + " holds " + slot.Count + " of '" +
                               (slot.Definition != null ? slot.Definition.DisplayName : "?") +
                               "' and carries per-object state. Those cannot both be true - anything " +
                               "with state is unstackable - so this stack was built by something " +
                               "that bypassed the stacking rule. Refusing to split it, because the " +
                               "copies would share that state and change together.");
                return null;
            }

            var destination = FirstEmptyIndex();
            if (destination < 0)
            {
                Debug.LogWarning("[Inventory] Nothing was split out of slot " + _slotIndex +
                                 " because there is no free slot to put it in.");
                return null;
            }

            var taken = slot.Item.ShallowCopy();
            slot.Remove(_quantity);
            _slots[destination].Set(taken, _quantity);

            return taken;
        }

        public bool MoveSlot(int _from, int _to)
        {
            if (_from == _to) return false;
            if (_from < 0 || _from >= _slots.Count) return false;
            if (_to < 0 || _to >= _slots.Count) return false;

            var source = _slots[_from];
            var target = _slots[_to];

            if (source.IsEmpty) return false;

            if (target.IsEmpty)
            {
                target.Set(source.Item, source.Count);
                source.Clear();
                return true;
            }

            if (CanStack(source.Item, target.Item))
            {
                var maxStack = MaxStackFor(target.Item);
                var room = maxStack - target.Count;

                if (room > 0)
                {
                    var moved = Mathf.Min(room, source.Count);
                    target.Add(moved);
                    source.Remove(moved);
                    return true;
                }
            }

            source.SwapWith(target);
            return true;
        }

        /// <summary>Removes up to <paramref name="_quantity"/> of a definition.</summary>
        /// <returns>How many were removed.</returns>
        /// <summary>
        /// How many ORDINARY ones of something are held - ones carrying no per-object state.
        /// </summary>
        /// <remarks>
        /// <b>Anything that consumes items by kind must use this rather than the plain count.</b>
        /// A recipe asking for one iron sword means an ordinary iron sword. Counting by definition
        /// alone includes the enchanted one, the one at plus five, the one with a name the player
        /// gave it - and whatever consumes them next will take it.
        ///
        /// This is the same distinction the whole instance design rests on, arriving in the one place
        /// it is easiest to forget: a helper that takes a definition uid and looks like it is asking
        /// about a kind of thing, while quietly ranging over particular ones.
        /// </remarks>
        public int CountUnmodifiedOf(string _definitionUid)
        {
            if (string.IsNullOrEmpty(_definitionUid)) return 0;

            var total = 0;
            foreach (var slot in _slots)
            {
                if (slot.IsEmpty) continue;
                if (slot.Item.DefinitionUid != _definitionUid) continue;
                if (!slot.Item.IsPlain) continue;

                total += slot.Count;
            }
            return total;
        }

        /// <summary>
        /// Removes ordinary ones of something, never touching anything that carries state. Returns
        /// how many were actually removed.
        /// </summary>
        /// <remarks>
        /// The counterpart to <see cref="CountUnmodifiedOf"/>, and the two must always be used
        /// together: counting one way and consuming the other is how a player loses a weapon they
        /// spent an evening improving.
        /// </remarks>
        public int RemoveUnmodified(string _definitionUid, int _quantity = 1)
        {
            if (string.IsNullOrEmpty(_definitionUid) || _quantity <= 0) return 0;

            var remaining = _quantity;

            // Back to front, so partial stacks are consumed before full ones are broken into.
            for (int i = _slots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = _slots[i];
                if (slot.IsEmpty) continue;
                if (slot.Item.DefinitionUid != _definitionUid) continue;
                if (!slot.Item.IsPlain) continue;

                remaining -= slot.Remove(remaining);
            }

            return _quantity - remaining;
        }

        /// <summary>
        /// Whether this many of a definition would fit, counting room in existing stacks as well as
        /// empty slots.
        /// </summary>
        /// <remarks>
        /// <b>Asked before anything is consumed, never after.</b> Consuming materials and then
        /// discovering the result will not fit is the classic way a crafting system eats an evening
        /// of gathering, and it cannot be recovered from once the materials are gone.
        ///
        /// An unlimited inventory always has room, which is why capacity of zero or less means
        /// unlimited everywhere in this class.
        /// </remarks>
        public bool HasRoomFor(ItemDefinition _definition, int _quantity)
        {
            if (_definition == null || _quantity <= 0) return false;
            if (_capacity <= 0) return true;

            EnsureSize();

            var stackable = _definition.GetModule<StackableModule>();
            int maxStack = stackable != null ? stackable.MaxStack : 1;

            int room = 0;
            foreach (var slot in _slots)
            {
                if (slot.IsEmpty) { room += maxStack; continue; }

                if (maxStack <= 1) continue;
                if (slot.Item.DefinitionUid != _definition.Uid) continue;
                if (!slot.Item.IsPlain) continue;

                room += Mathf.Max(0, maxStack - slot.Count);
            }

            return room >= _quantity;
        }

        public int Remove(string _definitionUid, int _quantity = 1)
        {
            if (string.IsNullOrEmpty(_definitionUid) || _quantity <= 0) return 0;

            var remaining = _quantity;

            // Back to front, so partial stacks are consumed before full ones are broken into.
            for (int i = _slots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = _slots[i];
                if (slot.IsEmpty || slot.Item.DefinitionUid != _definitionUid) continue;
                remaining -= slot.Remove(remaining);
            }

            return _quantity - remaining;
        }

        /// <summary>Empties the slot holding a specific instance, leaving the slot in place.</summary>
        public bool RemoveInstance(string _instanceId)
        {
            var index = IndexOfInstance(_instanceId);
            if (index < 0) return false;

            _slots[index].Clear();
            return true;
        }

        /// <summary>Total number held of a definition, across every slot.</summary>
        public int CountOf(string _definitionUid)
        {
            var total = 0;
            foreach (var slot in _slots)
            {
                if (!slot.IsEmpty && slot.Item.DefinitionUid == _definitionUid) total += slot.Count;
            }
            return total;
        }

        /// <summary>Index of the slot holding a particular instance, or -1.</summary>
        public int IndexOfInstance(string _instanceId)
        {
            if (string.IsNullOrEmpty(_instanceId)) return -1;

            for (int i = 0; i < _slots.Count; i++)
            {
                if (!_slots[i].IsEmpty && _slots[i].Item.InstanceId == _instanceId) return i;
            }
            return -1;
        }

        /// <summary>The slot holding a particular instance, or null.</summary>
        public InventorySlot FindInstance(string _instanceId)
        {
            var index = IndexOfInstance(_instanceId);
            return index >= 0 ? _slots[index] : null;
        }

        /// <summary>
        /// Reports every problem with the bag's contents.
        /// </summary>
        /// <remarks>
        /// Reports, and does not remove. An item that stopped resolving is usually a definition
        /// deleted during development, and the previous project's hard lesson was that quietly
        /// dropping such things loses established saves. Making it visible is the fix; tidying it away
        /// is the bug.
        /// </remarks>
        public List<string> Validate()
        {
            var problems = new List<string>();

            if (IsOverCapacity)
            {
                problems.Add("Holds " + _slots.Count + " slots but capacity is " + _capacity +
                             ". Nothing was discarded to make it fit.");
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot == null)
                {
                    problems.Add("Slot " + i + " is null.");
                    continue;
                }

                if (slot.IsEmpty) continue;

                if (!slot.Item.IsValid)
                {
                    problems.Add("Slot " + i + " holds '" + slot.Item.DefinitionUid +
                                 "', which no longer resolves to any definition. Its definition was " +
                                 "deleted. The item is kept, not discarded.");
                }
            }

            return problems;
        }

        private int FirstEmptyIndex()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsEmpty) return i;
            }

            // Unlimited inventories grow on demand. A capped one does not.
            if (_capacity > 0) return -1;

            _slots.Add(new InventorySlot());
            return _slots.Count - 1;
        }

        /// <summary>
        /// Brings the slot list up to capacity, and never shrinks it below what is occupied.
        /// </summary>
        private void EnsureSize()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] == null) _slots[i] = new InventorySlot();
            }

            if (_capacity <= 0) return;

            while (_slots.Count < _capacity) _slots.Add(new InventorySlot());

            // Trim only trailing EMPTY slots. An occupied slot beyond capacity is reported by
            // Validate rather than destroyed.
            while (_slots.Count > _capacity && _slots[_slots.Count - 1].IsEmpty)
            {
                _slots.RemoveAt(_slots.Count - 1);
            }
        }
    }
}
