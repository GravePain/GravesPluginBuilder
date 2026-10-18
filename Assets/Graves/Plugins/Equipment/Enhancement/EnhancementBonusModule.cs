using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Equipment.Enhancement
{
    /// <summary>
    /// Makes an entity feel the difference between a plain sword and an improved one.
    /// </summary>
    /// <remarks>
    /// Separate from the equipment stats module because the two are independent features. Equipment
    /// that grants bonuses is one thing; an economy in which those bonuses can be improved is
    /// another, and plenty of games have the first without the second.
    /// </remarks>
    [CreateAssetMenu(fileName = "EnhancementBonusModule", menuName = "Graves/Modules/Enhancement Bonuses")]
    public class EnhancementBonusModule : DefinitionModule
    {
        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var host = _entity.gameObject;

            if (host.GetComponent<Equipment>() == null)
            {
                Debug.LogError("[Enhancement] '" + _entity.name + "' is set to gain from improved " +
                               "equipment but has no equipment module, so it can wear nothing.",
                               _entity);
                return;
            }

            if (host.GetComponent<EnhancementBonuses>() == null) host.AddComponent<EnhancementBonuses>();

            // Health and the attacker cache which bonus sources they found. This one may have arrived
            // after they looked, and a plus-five sword quietly counting for nothing is a miserable
            // thing to chase.
            var health = host.GetComponent<Plugins.Vitals.Health>();
            if (health != null) health.RefreshSources();

            var attacker = host.GetComponent<Plugins.Combat.Attacker>();
            if (attacker != null) attacker.RefreshSources();
        }
    }
}
