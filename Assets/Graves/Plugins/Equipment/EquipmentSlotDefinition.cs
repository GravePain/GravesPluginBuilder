using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Equipment
{
    /// <summary>
    /// A place on a body where something can be worn or held: a head, a main hand, a ring finger.
    /// </summary>
    /// <remarks>
    /// <b>A slot is authored content, not a value in an enum.</b> An enum would mean the framework
    /// deciding that games have heads and hands, and a game wanting a tail, four arms or three ring
    /// fingers would have to edit a plugin to get one. Authoring them means a game invents whatever
    /// slots it has.
    ///
    /// <b>It is also not a string.</b> The reference gates abilities on a weapon category by prefix
    /// matching a string, which is one of the weakest things in it: a typo produces an item that
    /// fits nowhere and reports nothing. A definition has a permanent uid, so a slot can be renamed
    /// from "Main Hand" to "Weapon Hand" without invalidating a single item or a single save.
    ///
    /// <b>This is the first plugin here to add a whole new kind of definition.</b> The contract says
    /// a category attribute is all that is required and nothing had tested it. It appears in the
    /// authoring window beside entities and items, with no core change - which is the claim the
    /// framework has been making since before any of this was built.
    /// </remarks>
    [DefinitionCategory("Equipment Slots", Order = 30)]
    [CreateAssetMenu(fileName = "EquipmentSlot", menuName = "Graves/Equipment Slot Definition")]
    public class EquipmentSlotDefinition : GameDefinition
    {
        [SerializeField]
        [Tooltip("How many things can be worn here at once. Two rings on one hand is a capacity of " +
                 "two, not two separate slots, unless the game wants to tell them apart.")]
        [Min(1)] private int _capacity = 1;

        /// <summary>How many items may occupy this slot at once.</summary>
        public int Capacity => Mathf.Max(1, _capacity);
    }
}
