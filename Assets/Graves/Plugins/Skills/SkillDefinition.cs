using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Skills
{
    /// <summary>
    /// A thing an entity can deliberately do: a swing, a shout, a heal.
    /// </summary>
    /// <remarks>
    /// <b>A skill carries no behaviour, and this is the sharpest disagreement with the
    /// reference.</b> The reference framework's skill asset is data and behaviour together - virtual methods for
    /// checking the caster, checking the target and applying the effect, subclassed per kind of
    /// skill. It is convenient, and it is also why their definitions cannot be extended without
    /// partial classes: new behaviour means editing the asset's class.
    ///
    /// Here a skill is a name with modules bolted on. One module says what it costs in time, another
    /// says what it does, another says what must be true before it is allowed. Each lives in
    /// whichever plugin owns that concern, so a second plugin can add a new kind of effect without
    /// touching this file or any existing skill.
    ///
    /// <b>The cost is an indirection.</b> Overriding a virtual method is shorter to write than
    /// declaring a module that implements an interface. That tax is real and was accepted
    /// deliberately - see the second agent's extraction note, where it
    /// is recorded as a prediction to be judged rather than a certainty.
    /// </remarks>
    [DefinitionCategory("Skills", Order = 40)]
    [CreateAssetMenu(fileName = "Skill", menuName = "Graves/Skill Definition")]
    public class SkillDefinition : GameDefinition
    {
        [SerializeField]
        [Tooltip("Shown to the player. Free text, and not an identity - the uid is.")]
        [TextArea(1, 4)] private string _description;

        [SerializeField]
        [Tooltip("Optional item this skill takes its numbers from. Leave empty and the skill uses " +
                 "the numbers authored on itself. Bind one and the item wins, so a better weapon " +
                 "makes the same skill hit harder with no edit here.")]
        private ItemDefinition _numbersFrom;

        /// <summary>What this does, in words, for a tooltip to show.</summary>
        public string Description => _description;

        /// <summary>
        /// The item this skill reads its numbers from, or null when it carries its own.
        /// </summary>
        /// <remarks>
        /// <b>The split this enables is the design half against the numbers half.</b> What a skill IS
        /// - its shape, its reach, what it animates, what it demands before it will fire - is
        /// authored here. What it COSTS and what it DEALS can live on an item instead, so an upgrade,
        /// an enchantment or a rolled affix changes a skill's damage through the item economy without
        /// anything touching this asset.
        ///
        /// Taken from a shipped game of Graves', where every skill is two assets and the manual puts
        /// it plainly: the ability is the design, the item is the numbers.
        ///
        /// <b>Where this deliberately differs from his.</b> In that project the binding is mandatory
        /// and a skill with no item is silently broken in three ways at once - no cooldown, every
        /// value resolving to zero, and the global cooldown floor never armed. His own manual spends
        /// half a page on the trap.
        ///
        /// Here the binding is optional and the failure is loud. A skill with no bound item falls
        /// back to the numbers authored on itself; a skill with neither refuses to fire and says
        /// which module is missing. Nothing resolves silently to zero, because a free, instant,
        /// unlimited version of a skill reads in play as "the cooldown system is broken" rather than
        /// as a missing asset.
        /// </remarks>
        public ItemDefinition NumbersFrom => _numbersFrom;
    }
}
