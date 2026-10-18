using Graves.Plugins.Input;
using UnityEngine;

namespace Graves.Plugins.Skills.Player
{
    /// <summary>
    /// Uses a skill when the skill key is pressed.
    /// </summary>
    /// <remarks>
    /// <b>It uses the first skill the entity knows, and that is a placeholder.</b> Choosing between
    /// skills needs a hotbar - a row of slots, each naming one - and a hotbar needs a user interface
    /// to be worth anything. Until that exists, one key and one skill is enough for a person to
    /// press the thing and watch what happens, which is the only way the result can be judged.
    ///
    /// <b>The refusal reason is logged rather than swallowed.</b> A skill that will not fire and says
    /// nothing is indistinguishable from a key that is not bound, and the whole point of requirements
    /// explaining themselves is lost if nobody shows the explanation.
    /// </remarks>
    [DisallowMultipleComponent]
    public class PlayerSkillDriver : MonoBehaviour
    {
        private InputSource _input;
        private Skills _skills;

        /// <summary>Wires the driver to what it reads and what it drives.</summary>
        public void Initialise(InputSource _source, Skills _target)
        {
            _input = _source;
            _skills = _target;
        }

        private void Update()
        {
            if (_input == null || _skills == null) return;
            if (!_input.Read().Skill) return;

            var known = _skills.Known;
            if (known.Count == 0) return;

            var skill = _skills.Resolve(known[0].SkillUid);
            if (skill == null) return;

            if (_skills.TryUse(skill)) return;

            if (_skills.CanUse(skill, out var reason)) return;
            Debug.Log("[Skills] " + skill.DisplayName + ": " + reason, this);
        }
    }
}
