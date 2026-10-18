using Graves.Core;
using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Skills.Combat
{
    /// <summary>
    /// How hard a skill hits. Authored on the skill, or on an item the skill is bound to.
    /// </summary>
    /// <remarks>
    /// <b>The numbers half of the design-and-numbers split.</b> Put this on a skill and the skill has
    /// fixed damage. Put it on a weapon and bind that weapon to the skill, and the same technique
    /// scales with what is being held - which is what lets an upgrade, an enchantment or a rolled
    /// affix change a skill's damage without anything touching the skill.
    ///
    /// <b>Damage is a bonus over the user's own, not a replacement.</b> A skill that set its damage
    /// outright would ignore every weapon the user carries, so a heavy blow with a fine sword would
    /// hit for the same as with a rusty one. The multiplier scales what the user can already do; the
    /// flat amount is what the technique itself contributes regardless of what is held.
    ///
    /// <b>Nothing here uses a sentinel.</b> A field meaning "read this from somewhere else" resolves
    /// to a silent zero the moment the somewhere else is missing, which is the single sharpest trap
    /// in the design this was taken from. Absence is handled by the module being absent, which is a
    /// state that can be reported.
    /// </remarks>
    [CreateAssetMenu(fileName = "MeleeDamageModule", menuName = "Graves/Modules/Skill Melee Damage")]
    public class MeleeDamageModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("Multiplies the user's own damage, including whatever their equipment adds. One is " +
                 "a normal hit, two is double.")]
        private LevelValue _multiplier = new LevelValue { Base = 1.5f, PerLevel = 0.25f };

        [SerializeField]
        [Tooltip("Added after the multiplier. What the technique contributes on its own, whatever " +
                 "is being held.")]
        private LevelValue _flatBonus = new LevelValue { Base = 0f, PerLevel = 0f };

        /// <summary>The damage a strike should deal, given the user's own damage and the skill level.</summary>
        public int DamageFor(int _usersDamage, int _skillLevel)
        {
            float total = _usersDamage * _multiplier.At(_skillLevel) + _flatBonus.At(_skillLevel);
            return total < 0f ? 0 : Mathf.RoundToInt(total);
        }
    }
}
