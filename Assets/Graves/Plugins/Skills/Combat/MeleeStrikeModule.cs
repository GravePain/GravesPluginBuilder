using Graves.Core;
using Graves.Plugins.Combat;
using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Skills.Combat
{
    /// <summary>
    /// A skill effect that swings at whatever is in front of the user.
    /// </summary>
    /// <remarks>
    /// <b>It does not do its own hit detection.</b> The attacker owns what a sweep in front of an
    /// entity hits - the overlap, the self check, the dead check, the damage filters - and this asks
    /// it to do that with different numbers. Two owners of one fact drift the first time either is
    /// fixed, and a skill that quietly ignored the filter stopping monsters killing each other would
    /// be a very confusing bug.
    ///
    /// <b>This module owns the shape and not the numbers.</b> Reach, arc and whether it cleaves are
    /// what the technique IS. How hard it hits lives in a <see cref="MeleeDamageModule"/>, which can
    /// sit on this skill or on an item bound to it - so the same swing scales with the sword in your
    /// hand rather than being fixed by the ability.
    /// </remarks>
    [CreateAssetMenu(fileName = "MeleeStrikeModule", menuName = "Graves/Modules/Skill Melee Strike")]
    public class MeleeStrikeModule : DefinitionModule, ISkillEffect
    {
        [Header("Reach")]
        [SerializeField]
        [Tooltip("How far the swing reaches, in metres.")]
        [Min(0f)] private float _range = 2.5f;

        [SerializeField]
        [Tooltip("Total width of the swing in degrees. A wide arc is forgiving; a narrow one asks " +
                 "for aim and should pay more for it.")]
        [Range(0f, 360f)] private float _arc = 90f;

        [SerializeField]
        [Tooltip("Hit everything inside the arc rather than only the first thing found.")]
        private bool _hitEverythingInArc = true;

        /// <inheritdoc />
        /// <remarks>
        /// A user with no attacker is reported rather than ignored. It means a skill that hits
        /// things was given to something with no ability to hit, which is an authoring mistake and
        /// will otherwise present as a skill that fires, goes on cooldown, and does nothing.
        /// </remarks>
        public void Apply(SkillUse _use)
        {
            if (_use.User == null) return;

            var attacker = _use.User.GetComponent<Attacker>();
            if (attacker == null)
            {
                Debug.LogError("[Skills] '" + _use.Skill.DisplayName + "' strikes, but '" +
                               _use.User.name + "' has no combat module to strike with.", _use.User);
                return;
            }

            // Shape is authored here; the numbers are not. They come from the bound item when there
            // is one, so a better weapon makes the same technique hit harder without this asset
            // changing, and from the skill itself when there is not.
            var numbers = SkillNumbers.For<MeleeDamageModule>(_use.Skill);
            if (numbers == null)
            {
                Debug.LogError("[Skills] '" + _use.Skill.DisplayName + "' strikes, but no damage " +
                               "module was found on " +
                               SkillNumbers.SourceOf<MeleeDamageModule>(_use.Skill) +
                               ". Nothing was hit - a skill with no numbers is unfinished, not free.",
                               _use.User);
                return;
            }

            int damage = numbers.DamageFor(attacker.Damage, _use.Level);

            attacker.Strike(damage, _range, _arc, _hitEverythingInArc);
        }
    }
}
