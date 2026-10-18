using UnityEngine;

namespace Graves.Plugins.Skills
{
    /// <summary>
    /// One attempt to use one skill: who, which, and at what level.
    /// </summary>
    /// <remarks>
    /// Passed to every requirement and every effect, so neither has to go looking for context. It is
    /// a struct rather than a set of arguments because the list will grow - a target, a direction, a
    /// charge time - and adding a field here does not change the signature of every effect ever
    /// written.
    /// </remarks>
    public struct SkillUse
    {
        /// <summary>The entity using the skill.</summary>
        public GameObject User;

        /// <summary>Which skill.</summary>
        public SkillDefinition Skill;

        /// <summary>What level the user has in it. One-based.</summary>
        public int Level;
    }
}
