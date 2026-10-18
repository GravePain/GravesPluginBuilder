using Graves.Core;
using Graves.Plugins.Combat;
using Graves.Plugins.Vitals;
using UnityEngine;

namespace Graves.Plugins.Equipment.Enhancement
{
    /// <summary>
    /// Adds what improved equipment is worth, on top of what the equipment itself is worth.
    /// </summary>
    /// <remarks>
    /// <b>A second bonus source sitting beside the first, rather than a change to it.</b> The
    /// equipment bonuses component knows what an item grants. This one knows what improving it
    /// grants. Neither had to learn about the other, and health and the attacker learned about
    /// neither of them - they sum whatever declares itself, which is what the pull mechanism was for.
    ///
    /// <b>This assembly is what pays for the split.</b> Delete it and equipment still grants its
    /// bonuses and enhancement still records its levels; the levels simply stop being worth anything.
    /// A game wanting equipment and no upgrade economy never installs it, and never inherits either
    /// dependency.
    ///
    /// <b>Levels are read from the instance, never from the definition.</b> A plus-three sword is one
    /// particular sword. Reading enhancement off the definition would improve every copy of it in the
    /// world at once, and that distinction is the reason instances exist at all.
    /// </remarks>
    [DisallowMultipleComponent]
    public class EnhancementBonuses : MonoBehaviour, IHealthBonus, ICombatBonus
    {
        private Equipment _equipment;

        private Equipment Worn
        {
            get
            {
                if (_equipment == null) _equipment = GetComponent<Equipment>();
                return _equipment;
            }
        }

        /// <inheritdoc />
        public int GetHealthBonus(int _baseHealth)
        {
            var equipment = Worn;
            if (equipment == null) return 0;

            int total = 0;
            foreach (var entry in equipment.Worn)
            {
                var scaling = ScalingOn(entry?.Item);
                if (scaling == null) continue;

                total += scaling.HealthPerLevel * Plugins.Enhancement.Enhancer.LevelOf(entry.Item);
            }
            return total;
        }

        /// <inheritdoc />
        public int GetDamageBonus(int _baseDamage)
        {
            var equipment = Worn;
            if (equipment == null) return 0;

            int total = 0;
            foreach (var entry in equipment.Worn)
            {
                var scaling = ScalingOn(entry?.Item);
                if (scaling == null) continue;

                total += scaling.DamagePerLevel * Plugins.Enhancement.Enhancer.LevelOf(entry.Item);
            }
            return total;
        }

        /// <summary>
        /// What improving this worn item is worth, or null when improving it is worth nothing.
        /// </summary>
        private static EnhancementScalingModule ScalingOn(GameInstance _item)
        {
            var definition = _item?.DefinitionAs<ItemDefinition>();
            return definition == null ? null : definition.GetModule<EnhancementScalingModule>();
        }
    }
}
