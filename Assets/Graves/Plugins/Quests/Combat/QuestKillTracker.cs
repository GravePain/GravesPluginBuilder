using Graves.Core;
using Graves.Plugins.Combat;
using Graves.Plugins.Quests;
using Graves.Plugins.Vitals;
using UnityEngine;

namespace Graves.Plugins.Quests.Combat
{
    /// <summary>
    /// Watches what this entity kills and tells its quest log.
    /// </summary>
    /// <remarks>
    /// <b>The killer counts, not the victim.</b> This was the one real design decision in the quest
    /// plugin and it could have gone the other way. The framework already had the opposite pattern in
    /// front of it: an experience reward sits on the thing that dies, listens to its own death, and
    /// reaches into whoever killed it. Copying that would have meant a module on every kind of thing
    /// a quest might ever ask for - so adding a quest to hunt wolves would mean editing the wolf, and
    /// forgetting to would produce a quest that silently never progresses.
    ///
    /// The direction here is the one that matches how the quest is authored. The quest names its
    /// target; the target does not know it has been named, and never needs to.
    ///
    /// <b>What that costs, stated rather than hidden.</b> Only a killing blow delivered through an
    /// attacker counts. Something that drowns, burns down over time, falls, or is removed by a script
    /// is not a kill anybody claims - and when a game gains any of those, this is the component that
    /// has to learn about them.
    ///
    /// <b>It listens to the hit, not to the death.</b> Subscribing to the victim's death event would
    /// mean subscribing to something new on every swing and unsubscribing from a corpse. The attacker
    /// already reports every blow it lands, and a victim that is dead immediately after being struck
    /// was killed by that blow - the attacker refuses to strike anything already dead, so there is no
    /// second claimant.
    /// </remarks>
    [DisallowMultipleComponent]
    public class QuestKillTracker : MonoBehaviour
    {
        private Attacker _attacker;
        private QuestLog _log;

        /// <summary>Wires the tracker to the attacker whose kills it counts and the log it reports to.</summary>
        public void Initialise(Attacker _source, QuestLog _destination)
        {
            if (_attacker != null) _attacker.Hit -= OnHit;

            _attacker = _source;
            _log = _destination;

            if (_attacker != null) _attacker.Hit += OnHit;
        }

        private void OnHit(Health _victim, int _dealt)
        {
            if (_log == null || _victim == null || !_victim.IsDead) return;

            var entity = _victim.GetComponentInParent<GameEntity>();
            if (entity == null || entity.Definition == null) return;

            _log.RecordKill(entity.Definition.Uid);
        }

        private void OnDestroy()
        {
            if (_attacker != null) _attacker.Hit -= OnHit;
        }
    }
}
