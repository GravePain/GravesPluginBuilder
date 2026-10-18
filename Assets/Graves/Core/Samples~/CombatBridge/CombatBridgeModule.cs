using Graves.Core;
using Graves.Plugins.Vitals;
using UnityEngine;

namespace Graves.Plugins.CombatBridge
{
    /// <summary>
    /// Says that this kind of entity takes its damage from the external combat framework.
    /// </summary>
    /// <remarks>
    /// <b>The module is the entire authored surface of an adopted tool.</b> Adding this to a
    /// definition is how a designer says "this one fights"; there is nothing else to wire, and no
    /// field here for tuning, because every number that decides a hit already lives in the combat
    /// framework's own assets - the ability, the hitbox, the character definition. Duplicating any
    /// of them here would create a second place to tune damage and a first-class argument about
    /// which one is real.
    ///
    /// <b>This is the shape to copy when adopting anything proven.</b> The tool stays exactly as it
    /// is, in its own assembly, knowing nothing about this framework. One assembly references both
    /// and holds one module and one component. If the tool is ever replaced, the blast radius is
    /// this folder.
    /// </remarks>
    [CreateAssetMenu(fileName = "CombatBridgeModule", menuName = "Graves/Modules/Combat Bridge")]
    public class CombatBridgeModule : DefinitionModule
    {
        /// <summary>
        /// Attaches the bridge, after checking the entity can actually receive damage.
        /// </summary>
        /// <remarks>
        /// The Vitals check happens here rather than being left to the component, because a
        /// definition missing its Vitals module is an authoring mistake and this is the moment it can
        /// be named. Module order is not relied on: Health is added if it is absent, exactly as
        /// VitalsModule would, and VitalsModule configures it whenever it runs.
        /// </remarks>
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var host = _entity.gameObject;

            if (host.GetComponent<Health>() == null)
            {
                Debug.LogWarning("[CombatBridge] '" + _entity.name + "' has a combat bridge but no " +
                                 "Vitals module, so it has no health to lose. Add Vitals to its " +
                                 "definition.", _entity);
            }

            if (host.GetComponent<CombatBridge>() == null) host.AddComponent<CombatBridge>();
        }
    }
}
