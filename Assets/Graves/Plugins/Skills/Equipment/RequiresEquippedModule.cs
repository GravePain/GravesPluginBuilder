using Graves.Core;
using Graves.Plugins.Equipment;
using UnityEngine;

namespace Graves.Plugins.Skills.Equipment
{
    /// <summary>
    /// A requirement that something must be worn in a given slot before the skill may be used.
    /// </summary>
    /// <remarks>
    /// <b>This is where the equipment work pays for itself.</b> The reference gates skills on a
    /// weapon by prefix-matching a string - a broad term matches anything, a narrower one restricts
    /// it - and a typo produces a skill nobody can ever use, silently. Here the slot is a definition
    /// with a permanent id, so it can be renamed freely and cannot be misspelled at all.
    ///
    /// <b>It checks occupancy, not what kind of thing is there.</b> "Something in the main hand" is
    /// the common case and is all the current model can express. Gating on a kind of weapon - a
    /// sword rather than a bow - wants item categories, which do not exist yet and should be built
    /// when a skill genuinely needs them rather than guessed at now.
    /// </remarks>
    [CreateAssetMenu(fileName = "RequiresEquippedModule", menuName = "Graves/Modules/Skill Requires Equipped")]
    public class RequiresEquippedModule : DefinitionModule, ISkillRequirement
    {
        [SerializeField]
        [Tooltip("The slot that must be occupied. With none assigned the requirement is treated as " +
                 "unsatisfiable rather than as always true, so an unfinished skill fails loudly.")]
        private EquipmentSlotDefinition _slot;

        [SerializeField]
        [Tooltip("Shown to the player when the requirement is not met. Left blank, a plain sentence " +
                 "is generated from the slot's name.")]
        private string _message;

        /// <inheritdoc />
        public bool IsMet(SkillUse _use, out string _reason)
        {
            if (_slot == null)
            {
                _reason = "This skill names no equipment slot, so it can never be used.";
                return false;
            }

            var equipment = _use.User == null ? null : _use.User.GetComponent<Graves.Plugins.Equipment.Equipment>();
            if (equipment == null)
            {
                _reason = "Nothing can be equipped.";
                return false;
            }

            if (equipment.IsOccupied(_slot))
            {
                _reason = null;
                return true;
            }

            _reason = string.IsNullOrWhiteSpace(_message)
                    ? "Requires something in the " + _slot.DisplayName + "."
                    : _message;
            return false;
        }
    }
}
