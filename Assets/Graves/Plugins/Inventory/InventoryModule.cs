using System;
using System.Collections.Generic;
using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Inventory
{
    /// <summary>One authored line of starting contents.</summary>
    [Serializable]
    public class StartingItem
    {
        [Tooltip("What to grant.")]
        public ItemDefinition Item;

        [Tooltip("How many.")]
        [Min(1)] public int Count = 1;
    }

    /// <summary>
    /// Gives an entity a bag: how much it holds, and what it starts with.
    /// </summary>
    /// <remarks>
    /// Attached to an <c>EntityDefinition</c>. The definition says what a kind of thing carries; the
    /// instance holds what this particular one actually has.
    ///
    /// <b>Starting items reference definitions, and instances are made from them at runtime.</b> The
    /// alternative — authoring item instances directly into a definition — is precisely what broke the
    /// previous project: authored inventory content persisted object references that resolved after a
    /// restart to a boot texture, a bag mesh and an arm bone, and reported nothing. A definition
    /// reference is an asset reference, resolved by Unity, and safe. An instance is made fresh, here,
    /// every time.
    /// </remarks>
    [CreateAssetMenu(fileName = "InventoryModule", menuName = "Graves/Modules/Inventory")]
    public class InventoryModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("How many slots. Zero or less means unlimited.")]
        private int _capacity = 20;

        [SerializeField]
        [Tooltip("Granted once, the first time this entity comes into existence. Never on a load.")]
        private List<StartingItem> _startingItems = new List<StartingItem>();

        /// <summary>Slot limit. Zero or less means unlimited.</summary>
        public int Capacity => _capacity;

        /// <summary>What this kind of entity begins with.</summary>
        public IReadOnlyList<StartingItem> StartingItems => _startingItems;

        /// <summary>
        /// Ensures the entity has an inventory, and grants starting contents exactly once.
        /// </summary>
        /// <remarks>
        /// The seeding guard is not defensive coding. A loaded save already contains the inventory
        /// state, and without the guard this would grant the starting gear again on top of it, on
        /// every load. The symptom looks like item duplication rather than a save fault, so it gets
        /// investigated in the wrong place.
        /// </remarks>
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null || _entity.Instance == null) return;

            // Asked for by key rather than by type, so a second container of the same kind - a
            // purse, a bank, a stash - can never be handed back as the bag. Which one the typed
            // lookup returns depends on the order modules happen to sit in, which is not a thing
            // anybody should have to reason about while authoring.
            var inventory = _entity.Instance.GetOrAddState<InventoryState>(InventoryState.Key);
            inventory.Capacity = _capacity;

            if (inventory.Seeded) return;
            inventory.Seeded = true;

            foreach (var starting in _startingItems)
            {
                if (starting == null || starting.Item == null) continue;

                var stored = inventory.Add(new GameInstance(starting.Item), starting.Count);
                if (stored < starting.Count)
                {
                    Debug.LogWarning("[Inventory] '" + _entity.name + "' could only take " + stored +
                                     " of " + starting.Count + " '" + starting.Item.DisplayName +
                                     "'. Its capacity is " + _capacity + ".", _entity);
                }
            }
        }
    }
}
