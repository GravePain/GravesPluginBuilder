using Graves.Core;
using Graves.Plugins.Vitals;
using UnityEngine;

namespace Graves.Plugins.Skills.Vitals
{
    /// <summary>
    /// Applies the rule that the dead cannot use skills.
    /// </summary>
    /// <remarks>
    /// A module rather than an automatic behaviour, because a game about necromancers might
    /// genuinely want the dead to act. It is the sort of default that looks obviously correct until
    /// somebody's whole design depends on the opposite.
    /// </remarks>
    [CreateAssetMenu(fileName = "DeadCannotUseSkillsModule", menuName = "Graves/Modules/Dead Cannot Use Skills")]
    public class DeadCannotUseSkillsModule : DefinitionModule
    {
        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var skills = _entity.GetComponent<Skills>();
            if (skills == null)
            {
                Debug.LogError("[Skills] '" + _entity.name + "' has the dead-cannot-act rule but " +
                               "knows no skills. Add a skills module.", _entity);
                return;
            }

            if (_entity.GetComponent<Health>() == null)
            {
                Debug.LogError("[Skills] '" + _entity.name + "' has the dead-cannot-act rule but no " +
                               "vitals, so it can never be dead.", _entity);
                return;
            }

            if (_entity.GetComponent<DeadCannotUseSkills>() == null)
                _entity.gameObject.AddComponent<DeadCannotUseSkills>();

            skills.RefreshBlockers();
        }
    }
}
