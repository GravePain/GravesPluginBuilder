using Graves.Core;
using Graves.Plugins.Combat;
using Graves.Plugins.Quests;
using UnityEngine;

namespace Graves.Plugins.Quests.Combat
{
    /// <summary>
    /// Makes this entity's kills count towards its quests.
    /// </summary>
    /// <remarks>
    /// <b>Separate from the quest log module on purpose.</b> Holding quests and being able to kill
    /// things are two different capabilities. A quest giver has a log full of nothing and no weapon;
    /// a summoned creature swings and has no quests of its own. Only something that does both carries
    /// both, and forcing them together would mean the quest plugin could not be installed without the
    /// combat plugin.
    ///
    /// <b>Order matters, and it is reported rather than assumed.</b> This needs the log and the
    /// attacker to exist already, both of which are added by other modules. Put it after them.
    /// </remarks>
    [CreateAssetMenu(fileName = "CountsKillsModule", menuName = "Graves/Modules/Counts Kills For Quests")]
    public class CountsKillsModule : DefinitionModule
    {
        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var host = _entity.gameObject;

            var log = host.GetComponent<QuestLog>();
            if (log == null)
            {
                Debug.LogError("[Quests] '" + _entity.name + "' counts kills but has no quest log, " +
                               "so there is nothing to count towards. Add a Quest Log module above " +
                               "this one.", _entity);
                return;
            }

            var attacker = host.GetComponent<Attacker>();
            if (attacker == null)
            {
                Debug.LogError("[Quests] '" + _entity.name + "' counts kills but cannot attack, so " +
                               "it will never make one. Add a Combat module above this one.", _entity);
                return;
            }

            var tracker = host.GetComponent<QuestKillTracker>();
            if (tracker == null) tracker = host.AddComponent<QuestKillTracker>();

            tracker.Initialise(attacker, log);
        }
    }
}
