using Graves.Core;
using Graves.Plugins.Input;
using UnityEngine;

namespace Graves.Plugins.Skills.Player
{
    /// <summary>
    /// Lets a person trigger this entity's skills.
    /// </summary>
    /// <remarks>
    /// The same shape as the player control and player attack modules, and separate for the same
    /// reason: knowing a skill and choosing to use one are different questions, and only the second
    /// involves a human. An AI that uses skills is a sibling of this and shares everything below it.
    /// </remarks>
    [CreateAssetMenu(fileName = "PlayerSkillModule", menuName = "Graves/Modules/Player Skills")]
    public class PlayerSkillModule : DefinitionModule
    {
        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var host = _entity.gameObject;

            var skills = host.GetComponent<Skills>();
            if (skills == null)
            {
                Debug.LogError("[Skills] '" + _entity.name + "' can be told to use skills but knows " +
                               "none. Add a skills module.", _entity);
                return;
            }

            var source = host.GetComponent<InputSource>();
            if (source == null) source = host.AddComponent<KeyboardMouseInput>();

            var driver = host.GetComponent<PlayerSkillDriver>();
            if (driver == null) driver = host.AddComponent<PlayerSkillDriver>();

            driver.Initialise(source, skills);
        }
    }
}
