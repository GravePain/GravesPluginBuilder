using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Equipment
{
    /// <summary>
    /// Marks an item as wearable, and says where it goes.
    /// </summary>
    /// <remarks>
    /// Attached to an item definition. An item without one can sit in an inventory and never be
    /// equipped, which is correct for a potion, a key or a lump of ore.
    ///
    /// <b>The slot is referenced as an asset, and the uid is what is stored.</b> The reference here
    /// is an ordinary authored link, which Unity keeps by file identity so renaming the slot cannot
    /// break it. What goes into a save is <see cref="SlotUid"/>, never the reference, because a save
    /// that points at an asset is the failure this whole framework was built in reaction to.
    /// </remarks>
    [CreateAssetMenu(fileName = "EquippableModule", menuName = "Graves/Modules/Equippable")]
    public class EquippableModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("Where this item is worn. An item with no slot assigned cannot be equipped, and " +
                 "the attempt is reported rather than guessed at.")]
        private EquipmentSlotDefinition _slot;

        [SerializeField]
        [Tooltip("Optional prefab attached to the wearer when this is equipped - a sword model, a " +
                 "hat. Left empty, equipping changes numbers and nothing visible.")]
        private GameObject _worn;

        /// <summary>The slot this item occupies. Null when unassigned, which is a configuration error.</summary>
        public EquipmentSlotDefinition Slot => _slot;

        /// <summary>The permanent id of the slot this item occupies, or null.</summary>
        public string SlotUid => _slot != null ? _slot.Uid : null;

        /// <summary>Optional prefab shown on the wearer. May be null.</summary>
        public GameObject Worn => _worn;
    }
}
