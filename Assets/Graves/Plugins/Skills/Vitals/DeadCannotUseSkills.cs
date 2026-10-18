using Graves.Plugins.Vitals;
using UnityEngine;

namespace Graves.Plugins.Skills.Vitals
{
    /// <summary>
    /// Stops a dead thing using skills.
    /// </summary>
    /// <remarks>
    /// The other half of a rule the attacker already had. A dead entity was refused a basic swing
    /// and allowed to cast, because the check lived in one place and not the other. Found by a
    /// player who died, kept pressing keys, and reported that the only thing still working was the
    /// skill.
    ///
    /// It lives in its own assembly so the skills plugin does not have to know what health is. A
    /// game with skills and no vitals installs neither this nor a reason to want it.
    /// </remarks>
    [DisallowMultipleComponent]
    public class DeadCannotUseSkills : MonoBehaviour, ISkillBlocker
    {
        private Health _health;
        private bool _looked;

        /// <inheritdoc />
        public bool BlocksSkills(out string _reason)
        {
            if (!_looked)
            {
                _looked = true;
                _health = GetComponent<Health>();
            }

            if (_health != null && _health.IsDead)
            {
                _reason = "You are dead.";
                return true;
            }

            _reason = null;
            return false;
        }
    }
}
