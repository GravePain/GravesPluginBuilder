using Graves.Core;
using Graves.Plugins.Input;
using UnityEngine;

namespace Graves.Plugins.Combat.Player
{
    /// <summary>
    /// Lets a person attack with this entity.
    /// </summary>
    /// <remarks>
    /// Separate from the combat module for the same reason the player brain is separate from the
    /// motor: what can attack and what decides to attack are different questions, and only one of
    /// them involves a human. It is also what keeps the input system out of the combat assembly.
    /// </remarks>
    [CreateAssetMenu(fileName = "PlayerAttackModule", menuName = "Graves/Modules/Player Attack")]
    public class PlayerAttackModule : DefinitionModule
    {
        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var host = _entity.gameObject;

            var attacker = host.GetComponent<Attacker>();
            if (attacker == null)
            {
                Debug.LogError("[PlayerAttack] '" + _entity.name + "' can be told to attack but has " +
                               "no combat module, so there is nothing to swing.", _entity);
                return;
            }

            var source = host.GetComponent<InputSource>();
            if (source == null) source = host.AddComponent<KeyboardMouseInput>();

            var driver = host.GetComponent<PlayerAttackDriver>();
            if (driver == null) driver = host.AddComponent<PlayerAttackDriver>();

            driver.Initialise(source, attacker);
        }
    }
}
