using Graves.Core;
using Graves.Plugins.Animation;
using UnityEngine;

namespace Graves.Plugins.Skills.Animation
{
    /// <summary>
    /// A skill effect that plays a named clip on the user.
    /// </summary>
    /// <remarks>
    /// <b>It asks for a clip by name and knows nothing about how one is played.</b> The runner takes
    /// the Animator's output for the length of the clip and hands it back; whether that is a
    /// Playables graph, a state machine or something not written yet is not this module's business.
    /// That separation is the one the categorisation document draws between playback and bridge, and
    /// this is the bridge - it decides that a skill is worth a clip and which one.
    ///
    /// <b>An effect, not a special case.</b> Because it is simply another effect module, a skill can
    /// play a clip and deal damage by carrying two modules, and neither knows the other exists. A
    /// skill that only animates - a taunt, a bow - carries this one alone.
    ///
    /// <b>Root motion is deliberately not enabled by this.</b> Clips that carry root translation
    /// will not move the character, because the motor owns where the character goes. A skill wanting
    /// to travel should move the motor and play the clip, rather than letting the clip do both.
    /// </remarks>
    [CreateAssetMenu(fileName = "PlayClipModule", menuName = "Graves/Modules/Skill Play Clip")]
    public class PlayClipModule : DefinitionModule, ISkillEffect
    {
        [SerializeField]
        [Graves.Plugins.Animation.ClipName]
        [Tooltip("The clip this skill plays. Chosen from the names authored on animation modules " +
                 "rather than typed. The user needs a clip by this name; a skill naming one nobody " +
                 "has authored still fires, still costs its cooldown, and shows nothing.")]
        private string _clipName = "Attack";

        /// <summary>The clip this skill asks for.</summary>
        public string ClipName => _clipName;

        /// <inheritdoc />
        /// <remarks>
        /// Both failures are reported once rather than every use. A skill that animates nothing is
        /// worth knowing about, and a warning per swing would bury the console under a person
        /// playing normally.
        /// </remarks>
        public void Apply(SkillUse _use)
        {
            if (_use.User == null) return;

            var runner = _use.User.GetComponent<AnimationRunner>();
            if (runner == null)
            {
                Debug.LogWarning("[Skills] '" + _use.Skill.DisplayName + "' wants to play '" +
                                 _clipName + "' but '" + _use.User.name + "' has no animation " +
                                 "module. The skill still works, invisibly.", _use.User);
                return;
            }

            if (!runner.Play(_clipName))
            {
                Debug.LogWarning("[Skills] '" + _use.Skill.DisplayName + "' asked for a clip named '" +
                                 _clipName + "' and '" + _use.User.name + "' has no such clip.",
                                 _use.User);
            }
        }
    }
}
