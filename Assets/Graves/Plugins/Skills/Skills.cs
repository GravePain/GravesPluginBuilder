using System.Collections.Generic;
using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Skills
{
    /// <summary>
    /// What an entity knows how to do, and the act of doing it.
    /// </summary>
    /// <remarks>
    /// <b>This class settles permission and never decides consequence.</b> It checks that a skill is
    /// known, off cooldown, and that every requirement it carries is satisfied - and then hands the
    /// skill's own effect modules the job of doing something. It has no idea what any of them do,
    /// which is what lets a plugin add a new kind of effect without this file changing.
    ///
    /// <b>Cooldowns are held here, not in saved state.</b> A cooldown is a moment measured against a
    /// clock that restarts every session. Everything is ready on load, which is a decision rather
    /// than an omission - see <see cref="SkillsState"/>.
    /// </remarks>
    [DisallowMultipleComponent]
    public class Skills : MonoBehaviour
    {
        private SkillsState _state;
        private readonly SkillsState _loose = new SkillsState();
        private readonly Dictionary<string, float> _readyAt = new Dictionary<string, float>();

        private DefinitionRegistry _registry;
        private bool _lookedForRegistry;
        private ISkillBlocker[] _blockers;

        /// <summary>Everything this entity knows.</summary>
        public IReadOnlyList<KnownSkill> Known => Storage.Known;

        private SkillsState Storage => _state ?? _loose;

        /// <summary>Hands this component its storage, so a save reads and writes what it reads.</summary>
        public void Bind(SkillsState _storage) => _state = _storage;

        /// <summary>Forgets the cached blockers, so the next attempt finds whatever is there now.</summary>
        public void RefreshBlockers() => _blockers = null;

        /// <summary>Learns a skill, or raises its level if already known.</summary>
        public void Learn(SkillDefinition _skill, int _atLevel = 1)
        {
            if (_skill == null) return;
            Storage.Learn(_skill.Uid, _atLevel);
        }

        /// <summary>True when this entity knows the skill at all.</summary>
        public bool Knows(SkillDefinition _skill) => _skill != null && Storage.Find(_skill.Uid) != null;

        /// <summary>What level this entity has in a skill, or zero when it does not know it.</summary>
        public int LevelOf(SkillDefinition _skill)
        {
            var entry = _skill == null ? null : Storage.Find(_skill.Uid);
            return entry == null ? 0 : Mathf.Max(1, entry.Level);
        }

        /// <summary>Seconds until a skill may be used again. Zero when it is ready.</summary>
        public float CooldownRemaining(SkillDefinition _skill)
        {
            if (_skill == null) return 0f;
            return _readyAt.TryGetValue(_skill.Uid, out var ready)
                 ? Mathf.Max(0f, ready - Time.time)
                 : 0f;
        }

        /// <summary>True when the skill is known, off cooldown, and every requirement is satisfied.</summary>
        /// <remarks>
        /// The reason a skill cannot be used is returned rather than logged, so a caller can show it.
        /// A button that greys itself out and says why is a different experience from one that does
        /// nothing when pressed.
        /// </remarks>
        public bool CanUse(SkillDefinition _skill, out string _reason)
        {
            _reason = null;
            if (_skill == null) { _reason = "No skill."; return false; }

            if (!Knows(_skill)) { _reason = "Not learned."; return false; }

            // Asked before anything else about the skill itself. Whether the user is in a state to
            // act is a question about the user, and a corpse should be refused before the cooldown
            // on a particular ability is even consulted.
            if (_blockers == null) _blockers = GetComponents<ISkillBlocker>();
            for (int i = 0; i < _blockers.Length; i++)
            {
                if (_blockers[i] == null) continue;
                if (!_blockers[i].BlocksSkills(out var blocked)) continue;

                _reason = string.IsNullOrEmpty(blocked) ? "Cannot act." : blocked;
                return false;
            }

            float remaining = CooldownRemaining(_skill);
            if (remaining > 0f)
            {
                _reason = "Ready in " + remaining.ToString("0.0") + "s.";
                return false;
            }

            var use = new SkillUse { User = gameObject, Skill = _skill, Level = LevelOf(_skill) };

            var modules = _skill.Modules;
            for (int i = 0; i < modules.Count; i++)
            {
                if (!(modules[i] is ISkillRequirement requirement)) continue;
                if (requirement.IsMet(use, out var why)) continue;

                _reason = string.IsNullOrEmpty(why) ? "Requirement not met." : why;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Uses a skill if it is allowed, and returns whether it happened.
        /// </summary>
        /// <remarks>
        /// <b>The cooldown starts before the effects run.</b> An effect that used the skill again -
        /// a chain, a rebound - would otherwise recurse without limit, and the stack overflow is a
        /// long way from the module that caused it.
        ///
        /// <b>A skill with no effects still succeeds and still goes on cooldown.</b> That is the
        /// correct reading of a skill nobody has finished authoring, and it is reported so the
        /// half-finished skill is visible rather than merely inert.
        /// </remarks>
        public bool TryUse(SkillDefinition _skill)
        {
            if (!CanUse(_skill, out var reason))
            {
                if (!string.IsNullOrEmpty(reason) && reason != "Not learned.")
                {
                    // Not logged for an unknown skill: something asking for a skill it does not have
                    // is normal for a hotbar with empty slots.
                }
                return false;
            }

            int level = LevelOf(_skill);
            var use = new SkillUse { User = gameObject, Skill = _skill, Level = level };

            // The bound item first, then the skill. A skill with neither is not a skill with a zero
            // cooldown - it is a skill nobody finished, and treating those the same is how a free,
            // instant, unlimited ability ships looking like a broken cooldown system.
            var timing = SkillNumbers.For<SkillTimingModule>(_skill);
            if (timing == null)
            {
                Debug.LogError("[Skills] '" + _skill.DisplayName + "' has no timing module on " +
                               SkillNumbers.SourceOf<SkillTimingModule>(_skill) +
                               ", so it would be usable every frame. It was not used.", this);
                return false;
            }

            float cooldown = timing.CooldownAt(level);
            if (cooldown > 0f) _readyAt[_skill.Uid] = Time.time + cooldown;

            int applied = 0;
            var modules = _skill.Modules;
            for (int i = 0; i < modules.Count; i++)
            {
                if (!(modules[i] is ISkillEffect effect)) continue;
                effect.Apply(use);
                applied++;
            }

            if (applied == 0)
            {
                Debug.LogWarning("[Skills] '" + _skill.DisplayName + "' was used and did nothing - it " +
                                 "carries no effect module.", this);
            }

            return true;
        }

        /// <summary>Resolves a known skill's uid back to its definition, or null.</summary>
        public SkillDefinition Resolve(string _skillUid)
        {
            if (string.IsNullOrEmpty(_skillUid)) return null;

            if (!_lookedForRegistry)
            {
                _lookedForRegistry = true;
                var settings = CoreSettings.Instance;
                _registry = settings != null ? settings.Registry : null;
            }

            return _registry == null ? null : _registry.Resolve<SkillDefinition>(_skillUid);
        }
    }
}
