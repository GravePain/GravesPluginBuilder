using System;
using System.Collections.Generic;
using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Skills
{
    /// <summary>One skill an entity begins knowing, and how good it is at it.</summary>
    [Serializable]
    public class StartingSkill
    {
        [Tooltip("What it knows.")]
        public SkillDefinition Skill;

        [Tooltip("How practised it is. One-based.")]
        [Min(1)] public int Level = 1;
    }

    /// <summary>
    /// Gives an entity things it knows how to do.
    /// </summary>
    /// <remarks>
    /// Starting skills reference definitions and are learned into instance state at runtime, which
    /// is the same shape the inventory uses for starting items and for the same reason: authored
    /// content must never hold a live instance.
    ///
    /// <b>Learning is not guarded against repeating.</b> Unlike starting inventory, learning a skill
    /// twice is harmless - the state raises the level rather than adding a duplicate - so a load
    /// followed by an attach cannot produce a second copy of anything.
    /// </remarks>
    [CreateAssetMenu(fileName = "SkillsModule", menuName = "Graves/Modules/Skills")]
    public class SkillsModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("What this kind of entity knows from the start.")]
        private List<StartingSkill> _startingSkills = new List<StartingSkill>();

        /// <summary>What this kind of entity begins knowing.</summary>
        public IReadOnlyList<StartingSkill> StartingSkills => _startingSkills;

        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var host = _entity.gameObject;

            var skills = host.GetComponent<Skills>();
            if (skills == null) skills = host.AddComponent<Skills>();

            var instance = _entity.Instance;
            if (instance == null)
            {
                Debug.LogError("[Skills] '" + _entity.name + "' has no instance, so what it knows " +
                               "cannot be saved or restored.", _entity);
                return;
            }

            skills.Bind(instance.GetOrAddState<SkillsState>());

            for (int i = 0; i < _startingSkills.Count; i++)
            {
                var entry = _startingSkills[i];
                if (entry == null || entry.Skill == null)
                {
                    Debug.LogWarning("[Skills] '" + _entity.name + "' has an empty starting skill at " +
                                     "index " + i + ". Its definition was probably deleted.", _entity);
                    continue;
                }

                skills.Learn(entry.Skill, entry.Level);
            }
        }
    }
}
