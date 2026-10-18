using System;
using System.Collections.Generic;
using Graves.Core;

namespace Graves.Plugins.Quests
{
    /// <summary>
    /// One quest as this particular player has experienced it: taken, how far along, and paid.
    /// </summary>
    /// <remarks>
    /// <b>It names the quest by uid, never by reference.</b> A serialised object reference to a
    /// ScriptableObject is the exact failure the core was built around - it resolves to something
    /// plausible and unrelated after a restart. The definition is looked up through the registry when
    /// it is needed and is allowed to come back null, which means the quest was deleted and is worth
    /// reporting rather than hiding.
    /// </remarks>
    [Serializable]
    public class QuestRecord
    {
        /// <summary>Which quest this is.</summary>
        public string QuestUid;

        /// <summary>How many of the target have been killed while this quest was held.</summary>
        public int Killed;

        /// <summary>True once the reward has been handed over.</summary>
        /// <remarks>
        /// Separate from "the count is high enough", because those are two different moments and a
        /// player can reach the count and close the game before anything is paid. Deriving claimed
        /// from the count would pay the reward again on every load.
        /// </remarks>
        public bool Claimed;
    }

    /// <summary>
    /// Every quest this entity has taken, and how far along each one is.
    /// </summary>
    /// <remarks>
    /// <b>Progress belongs to the instance, and this is the clearest case of it yet.</b> A quest
    /// definition says kill ten grunts; it is the same quest for every player and it is nine down for
    /// exactly one of them. That split is what the core's definition and instance separation is for,
    /// and the quest plugin needed no new persistence mechanism to use it - the entity's instance
    /// already goes into the save file and comes back.
    ///
    /// <b>Only what cannot be recomputed is stored.</b> Not the required count, not the target, not
    /// the description - all of those live on the definition and should move when the game is
    /// rebalanced. A quest raised from ten kills to fifteen after a patch should find the returning
    /// player on nine of fifteen, not silently complete.
    /// </remarks>
    [Serializable]
    public class QuestLogState : InstanceState
    {
        /// <summary>The quests taken, in the order they were taken.</summary>
        public List<QuestRecord> Quests = new List<QuestRecord>();

        /// <inheritdoc />
        public override string StateKey => "graves.quests.log";

        /// <summary>The record for a quest, or null when it has never been taken.</summary>
        public QuestRecord Find(string _questUid)
        {
            if (string.IsNullOrEmpty(_questUid)) return null;

            for (int i = 0; i < Quests.Count; i++)
            {
                var record = Quests[i];
                if (record != null && record.QuestUid == _questUid) return record;
            }
            return null;
        }

        /// <summary>The record for a quest, creating one if it has never been taken.</summary>
        public QuestRecord FindOrAdd(string _questUid)
        {
            if (string.IsNullOrEmpty(_questUid)) return null;

            var existing = Find(_questUid);
            if (existing != null) return existing;

            var created = new QuestRecord { QuestUid = _questUid };
            Quests.Add(created);
            return created;
        }
    }
}
