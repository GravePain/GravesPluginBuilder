using System;
using System.Collections.Generic;
using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Skills
{
    /// <summary>One skill this entity knows, and how good it is at it.</summary>
    [Serializable]
    public class KnownSkill
    {
        [Tooltip("The permanent id of the skill definition.")]
        public string SkillUid;

        [Tooltip("How practised this entity is. One-based.")]
        public int Level = 1;
    }

    /// <summary>
    /// What this particular thing knows how to do.
    /// </summary>
    /// <remarks>
    /// <b>Skills are stored by uid, never by reference.</b> A save that pointed at an asset is the
    /// failure this framework was founded on. A uid resolves through the registry, and a skill whose
    /// definition has been deleted comes back unresolvable and is reported rather than silently
    /// becoming a different skill.
    ///
    /// <b>Cooldowns are not stored here, and that is a decision.</b> A cooldown is a moment in a
    /// running session measured against the clock, and the clock restarts. Persisting one would mean
    /// storing a wall-clock timestamp and reasoning about a player who quit for a week, which is a
    /// design question no game has asked yet. On load, everything is ready.
    /// </remarks>
    [Serializable]
    public class SkillsState : InstanceState
    {
        [SerializeField]
        [Tooltip("Everything this entity has learned.")]
        private List<KnownSkill> _known = new List<KnownSkill>();

        /// <summary>Everything this entity knows.</summary>
        public IReadOnlyList<KnownSkill> Known => _known;

        /// <summary>A stable name for this state in a save file.</summary>
        public override string StateKey => "graves.skills";

        /// <summary>The entry for a skill, or null when it is not known.</summary>
        public KnownSkill Find(string _skillUid)
        {
            if (string.IsNullOrEmpty(_skillUid)) return null;

            for (int i = 0; i < _known.Count; i++)
            {
                if (_known[i] != null && _known[i].SkillUid == _skillUid) return _known[i];
            }
            return null;
        }

        /// <summary>
        /// Records a skill as known, or raises its level if it already is. Returns the entry.
        /// </summary>
        /// <remarks>
        /// Learning something twice raises the level rather than adding a duplicate. Two entries for
        /// one skill would give it two cooldowns and two levels, and which one applied would depend
        /// on lookup order.
        /// </remarks>
        public KnownSkill Learn(string _skillUid, int _atLevel = 1)
        {
            if (string.IsNullOrEmpty(_skillUid)) return null;

            var existing = Find(_skillUid);
            if (existing != null)
            {
                if (_atLevel > existing.Level) existing.Level = _atLevel;
                return existing;
            }

            var added = new KnownSkill { SkillUid = _skillUid, Level = Mathf.Max(1, _atLevel) };
            _known.Add(added);
            return added;
        }

        /// <summary>Forgets a skill. Returns true when it was known.</summary>
        public bool Forget(string _skillUid)
        {
            var existing = Find(_skillUid);
            return existing != null && _known.Remove(existing);
        }

        /// <summary>Reports any known skill whose definition no longer exists.</summary>
        public List<string> Validate(DefinitionRegistry _registry)
        {
            var problems = new List<string>();
            if (_registry == null) return problems;

            for (int i = 0; i < _known.Count; i++)
            {
                var entry = _known[i];
                if (entry == null) { problems.Add("Entry " + i + " is null."); continue; }
                if (_registry.Resolve(entry.SkillUid) == null)
                    problems.Add("Knows a skill whose definition no longer exists (" + entry.SkillUid + ").");
            }
            return problems;
        }
    }
}
