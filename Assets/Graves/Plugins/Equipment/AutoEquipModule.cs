using System.Collections.Generic;
using Graves.Core;
using Graves.Plugins.Inventory;
using UnityEngine;

namespace Graves.Plugins.Equipment
{
    /// <summary>
    /// Wears whatever is already in the bag and fits an empty slot, once, when the entity wakes.
    /// </summary>
    /// <remarks>
    /// <b>A game's decision, and that is why it is a separate module rather than a default.</b>
    /// Spawning with your sword already drawn is right for a returning hero and wrong for a
    /// prisoner who starts with their gear confiscated in a chest. Neither is the framework's call.
    ///
    /// <b>It only fills empty slots.</b> Anything already worn - restored from a save, or equipped
    /// by something that attached earlier - is left exactly as it is. Without that, every load would
    /// re-equip whatever the definition happened to start with, quietly overwriting what the player
    /// had chosen.
    /// </remarks>
    [CreateAssetMenu(fileName = "AutoEquipModule", menuName = "Graves/Modules/Auto Equip")]
    public class AutoEquipModule : DefinitionModule
    {
        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var equipment = _entity.GetComponent<Equipment>();
            if (equipment == null)
            {
                Debug.LogError("[Equipment] '" + _entity.name + "' is set to equip what it carries " +
                               "but has no equipment module, so it can wear nothing.", _entity);
                return;
            }

            var instance = _entity.Instance;

            // Asked for by key, not by type. A purse is an InventoryState by inheritance, so the
            // typed lookup matches one - and which of the two it hands back depends on the order the
            // modules happen to sit in. Auto-equip would then scan the purse, find no equipment in
            // it, and do nothing at all, with no error anywhere to say why.
            var inventory = instance == null ? null
                                             : instance.GetState<InventoryState>(InventoryState.Key);
            if (inventory == null) return;

            // Collected before equipping, because equipping removes items from the inventory and
            // mutating a collection while walking it is a bug that only shows with two items.
            var candidates = new List<GameInstance>();
            foreach (var slot in inventory.Slots)
            {
                if (slot == null || slot.IsEmpty) continue;

                var definition = slot.Definition;
                if (definition == null) continue;
                if (definition.GetModule<EquippableModule>() == null) continue;

                candidates.Add(slot.Item);
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                var item = candidates[i];
                var definition = item.DefinitionAs<ItemDefinition>();
                var equippable = definition == null ? null : definition.GetModule<EquippableModule>();
                var slot = equippable == null ? null : equippable.Slot;

                if (slot == null) continue;
                if (!equipment.HasSlot(slot)) continue;
                if (equipment.IsOccupied(slot)) continue;

                equipment.Equip(item, inventory);
            }
        }
    }
}
