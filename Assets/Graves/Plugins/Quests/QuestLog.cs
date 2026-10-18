using System;
using System.Collections.Generic;
using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Quests
{
    /// <summary>
    /// What one entity has been asked to do, and how far along it is.
    /// </summary>
    /// <remarks>
    /// <b>The log counts; it does not watch.</b> Nothing here knows what combat is or how a thing
    /// dies. Something else notices a kill and tells this, which is what keeps the quest plugin
    /// referencing the core and nothing else - and is why a game with a different idea of what
    /// counts can supply its own reporter without touching a line of this.
    ///
    /// <b>The state is the storage, not a copy of it.</b> The same lesson the health component had to
    /// learn the hard way: an earlier design here would have held its own list and written it into
    /// the state on change, and a load would have overwritten the state in place while this went on
    /// reading its own copy. Every read and every write below goes through the bound state.
    /// </remarks>
    [DisallowMultipleComponent]
    public class QuestLog : MonoBehaviour
    {
        private QuestLogState _state;
        private GameEntity _owner;

        /// <summary>Raised when a quest is taken.</summary>
        public event Action<QuestDefinition> Accepted;

        /// <summary>Raised on each kill that counts. Carries the quest, the new count and the target.</summary>
        public event Action<QuestDefinition, int, int> Advanced;

        /// <summary>Raised the moment the count is reached, before any reward is paid.</summary>
        public event Action<QuestDefinition> Completed;

        /// <summary>Raised when the reward has actually been handed over.</summary>
        public event Action<QuestDefinition> Claimed;

        /// <summary>Hands the log its storage. Everything it knows lives there from this point on.</summary>
        public void Bind(GameEntity _entity, QuestLogState _storage)
        {
            _owner = _entity;
            _state = _storage;
        }

        /// <summary>The quests currently held, completed or not.</summary>
        public IReadOnlyList<QuestRecord> Records =>
            _state != null ? (IReadOnlyList<QuestRecord>)_state.Quests : Array.Empty<QuestRecord>();

        /// <summary>True when this quest has been taken.</summary>
        public bool Holds(QuestDefinition _quest) =>
            _quest != null && _state != null && _state.Find(_quest.Uid) != null;

        /// <summary>How many of the target have been killed for a quest. Zero when it is not held.</summary>
        public int KilledFor(QuestDefinition _quest)
        {
            if (_quest == null || _state == null) return 0;
            var record = _state.Find(_quest.Uid);
            return record != null ? record.Killed : 0;
        }

        /// <summary>True when the count has been reached, whether or not the reward has been paid.</summary>
        public bool IsComplete(QuestDefinition _quest) =>
            _quest != null && KilledFor(_quest) >= _quest.Required;

        /// <summary>True when the reward for this quest has been handed over.</summary>
        public bool IsClaimed(QuestDefinition _quest)
        {
            if (_quest == null || _state == null) return false;
            var record = _state.Find(_quest.Uid);
            return record != null && record.Claimed;
        }

        /// <summary>
        /// Takes a quest on. Returns false when it was already held, or cannot be tracked.
        /// </summary>
        /// <remarks>
        /// <b>Accepting a quest twice must not reset it.</b> A returning player whose starting quests
        /// are handed out again on every load would be put back to nought every session, which is the
        /// obvious implementation and destroys exactly the thing this plugin exists to keep.
        ///
        /// A quest with no target is refused rather than accepted and left permanently at nought, so
        /// the mistake is visible at the moment it is made instead of appearing as a quest that never
        /// progresses.
        /// </remarks>
        public bool Accept(QuestDefinition _quest)
        {
            if (_quest == null) return false;

            if (_state == null)
            {
                Debug.LogError("[Quests] '" + name + "' was offered '" + _quest.DisplayName +
                               "' but has no quest log storage, so nothing could be recorded.", this);
                return false;
            }

            if (_quest.Target == null)
            {
                Debug.LogError("[Quests] '" + _quest.DisplayName + "' has no target, so nothing can " +
                               "ever count towards it. It was not accepted.", _quest);
                return false;
            }

            if (_state.Find(_quest.Uid) != null) return false;

            _state.FindOrAdd(_quest.Uid);
            Accepted?.Invoke(_quest);
            return true;
        }

        /// <summary>
        /// Reports a kill. Every held quest hunting that kind advances by one.
        /// </summary>
        /// <remarks>
        /// <b>By definition uid, never by object.</b> The thing that died is gone by the time anything
        /// wants to look at it again, and a save restores an entity through the registry rather than
        /// as the same object. The uid is the only identity that survives both.
        ///
        /// <b>One kill can advance several quests, and that is not a bug.</b> Two people can both want
        /// grunts dead, and a kill that only counted for whichever quest was taken first would be
        /// wrong in a way the player experiences as the counter sticking.
        /// </remarks>
        public int RecordKill(string _definitionUid)
        {
            if (string.IsNullOrEmpty(_definitionUid) || _state == null) return 0;

            int advanced = 0;

            // By index rather than foreach, because paying a reward can add items, raise events and
            // in principle accept another quest - and mutating a list while enumerating it throws.
            for (int i = 0; i < _state.Quests.Count; i++)
            {
                var record = _state.Quests[i];
                if (record == null || record.Claimed) continue;

                var quest = Resolve(record.QuestUid);
                if (quest == null) continue;
                if (quest.TargetUid != _definitionUid) continue;

                // Counting stops at the requirement. Letting it run on would make "twelve of ten"
                // possible in a save file, and every screen that draws a bar would have to guard it.
                if (record.Killed >= quest.Required) continue;

                record.Killed++;
                advanced++;
                Advanced?.Invoke(quest, record.Killed, quest.Required);

                if (record.Killed < quest.Required) continue;

                Completed?.Invoke(quest);
                if (quest.PayOnCompletion) Claim(quest);
            }

            return advanced;
        }

        /// <summary>
        /// Hands over the reward for a completed quest. Returns false when there is nothing to pay or
        /// the payment could not be made.
        /// </summary>
        /// <remarks>
        /// <b>The single payment path.</b> Completing a quest calls this, and a hand-in at an NPC will
        /// call the same method, so there is one place where a reward is given and one place that
        /// records it was.
        ///
        /// <b>A reward that cannot be given leaves the quest unclaimed.</b> A full bag is an ordinary
        /// situation and losing the reward to it would be a real loss the player never sees happen.
        /// Marking it claimed only after every reward reported success means coming back with room
        /// works.
        /// </remarks>
        public bool Claim(QuestDefinition _quest)
        {
            if (_quest == null || _state == null) return false;

            var record = _state.Find(_quest.Uid);
            if (record == null || record.Claimed) return false;
            if (record.Killed < _quest.Required) return false;

            bool allPaid = true;
            int rewards = 0;

            var modules = _quest.Modules;
            for (int i = 0; i < modules.Count; i++)
            {
                var reward = modules[i] as QuestRewardModule;
                if (reward == null) continue;

                rewards++;
                if (!reward.Pay(_owner)) allPaid = false;
            }

            if (rewards == 0)
            {
                Debug.LogWarning("[Quests] '" + _quest.DisplayName + "' was completed and pays " +
                                 "nothing. Add a reward module to it, or that is the design.", _quest);
            }

            if (!allPaid) return false;

            record.Claimed = true;
            Claimed?.Invoke(_quest);
            return true;
        }

        /// <summary>The definition behind a recorded uid, or null when the quest has been deleted.</summary>
        /// <remarks>
        /// A record naming a quest that no longer exists is kept in the state, because deleting it
        /// would destroy the progress of a player whose game is simply older than the current build.
        /// </remarks>
        private QuestDefinition Resolve(string _uid)
        {
            var settings = CoreSettings.Instance;
            var registry = settings != null ? settings.Registry : null;
            if (registry == null) return null;

            return registry.Resolve(_uid) as QuestDefinition;
        }
    }
}
