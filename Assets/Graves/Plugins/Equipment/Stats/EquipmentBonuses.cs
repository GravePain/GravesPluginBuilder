using Graves.Core;
using Graves.Plugins.Combat;
using Graves.Plugins.Vitals;
using UnityEngine;

namespace Graves.Plugins.Equipment.Stats
{
    /// <summary>
    /// Adds up what the wearer is wearing, and reports it to whatever asks.
    /// </summary>
    /// <remarks>
    /// <b>This class is the whole reason the bonus interfaces exist, and it is the first thing to
    /// prove them.</b> Health and the attacker were written before equipment did, and neither
    /// mentions it. Putting this component on an entity makes armour count, with no edit to either
    /// and no message sent anywhere.
    ///
    /// <b>Nothing needs telling when equipment changes.</b> The component is present the whole time
    /// and only its answer changes, so a stat asking for its total simply gets a different number on
    /// the next read. There is no cache to invalidate, no event to miss, and no window in which the
    /// armour is on and the health has not noticed. That is worth stating because the obvious design
    /// - recompute a stored total whenever something is equipped - has all three of those problems.
    ///
    /// The cost is that both totals are summed on every read of a stat. Measured against the
    /// alternative, that is the right trade: the reference ran the same way commercially for years,
    /// and cached stats that silently disagree with their sources are far more expensive to find
    /// than a loop over a handful of worn items.
    /// </remarks>
    [DisallowMultipleComponent]
    public class EquipmentBonuses : MonoBehaviour, IHealthBonus, ICombatBonus
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

            int flat = 0;
            float percent = 0f;

            foreach (var entry in equipment.Worn)
            {
                var bonus = BonusOn(entry?.Item);
                if (bonus == null) continue;

                flat += bonus.Health;
                percent += bonus.HealthPercent;
            }

            // Proportional bonuses are taken against the base rather than against the running total,
            // so two items granting ten per cent each give twenty rather than twenty-one. Order of
            // equipping then cannot change the result, which it otherwise would.
            return flat + Mathf.RoundToInt(_baseHealth * (percent * 0.01f));
        }

        /// <inheritdoc />
        public int GetDamageBonus(int _baseDamage)
        {
            var equipment = Worn;
            if (equipment == null) return 0;

            int total = 0;
            foreach (var entry in equipment.Worn)
            {
                var bonus = BonusOn(entry?.Item);
                if (bonus != null) total += bonus.Damage;
            }
            return total;
        }

        /// <summary>
        /// The stat bonus carried by a worn instance, or null when it has none.
        /// </summary>
        /// <remarks>
        /// A worn item whose definition has been deleted contributes nothing and is not reported
        /// here. It is already reported by the equipment state's own validation, and logging it from
        /// inside a stat getter would print once per frame for the rest of the session.
        /// </remarks>
        private static StatBonusModule BonusOn(GameInstance _item)
        {
            var definition = _item?.DefinitionAs<ItemDefinition>();
            return definition == null ? null : definition.GetModule<StatBonusModule>();
        }
    }
}
