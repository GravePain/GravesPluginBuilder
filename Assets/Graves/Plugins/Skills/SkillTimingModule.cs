using Graves.Core;
using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Skills
{
    /// <summary>
    /// How often a skill may be used.
    /// </summary>
    /// <remarks>
    /// A skill with no timing module has no cooldown and may be used every frame, which is correct
    /// for a passive or a trivial one and is stated here so that its absence is a decision rather
    /// than an oversight.
    ///
    /// <b>There is deliberately no global cooldown.</b> A shared floor across every skill is a real
    /// design tool and it is also invisible until it silently blocks fast chaining, at which point it
    /// looks like input being dropped. If one is wanted it belongs here as an explicit setting, not
    /// as an unstated default.
    /// </remarks>
    [CreateAssetMenu(fileName = "SkillTimingModule", menuName = "Graves/Modules/Skill Timing")]
    public class SkillTimingModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("Seconds before this skill may be used again. The second field shortens it as the " +
                 "skill is levelled, so leave it at zero for a cooldown that never improves.")]
        private LevelValue _cooldown = new LevelValue { Base = 3f, PerLevel = -0.2f };

        /// <summary>Seconds between uses at a given skill level. Never negative.</summary>
        public float CooldownAt(int _level) => Mathf.Max(0f, _cooldown.At(_level));
    }
}
