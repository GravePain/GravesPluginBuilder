using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Equipment.Stats
{
    /// <summary>
    /// Makes what this entity wears actually change its numbers.
    /// </summary>
    /// <remarks>
    /// Separate from the equipment module because wearing things and being changed by them are
    /// different features. A game where equipment is cosmetic, or where it only gates which
    /// abilities are usable, installs the equipment plugin and never this one - and by doing so
    /// avoids taking a dependency on vitals and combat at all.
    /// </remarks>
    [CreateAssetMenu(fileName = "EquipmentStatsModule", menuName = "Graves/Modules/Equipment Stats")]
    public class EquipmentStatsModule : DefinitionModule
    {
        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var host = _entity.gameObject;

            if (host.GetComponent<Equipment>() == null)
            {
                Debug.LogError("[Equipment] '" + _entity.name + "' is set to gain stats from what it " +
                               "wears but has no equipment module, so it can wear nothing.", _entity);
                return;
            }

            if (host.GetComponent<EquipmentBonuses>() == null) host.AddComponent<EquipmentBonuses>();

            // Health and the attacker cache which bonus sources they found. This one may have
            // arrived after they looked, and a silent zero from armour that is plainly on the
            // character is a miserable thing to chase.
            var health = host.GetComponent<Graves.Plugins.Vitals.Health>();
            if (health != null) health.RefreshSources();

            var attacker = host.GetComponent<Graves.Plugins.Combat.Attacker>();
            if (attacker != null) attacker.RefreshSources();
        }
    }
}
