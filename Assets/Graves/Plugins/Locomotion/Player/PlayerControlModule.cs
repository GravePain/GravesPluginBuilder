using Graves.Core;
using Graves.Plugins.Input;
using UnityEngine;

namespace Graves.Plugins.Locomotion.Player
{
    /// <summary>
    /// Marks a definition as the one a person drives. Installs an input source and a brain, and
    /// connects them to whatever motor is already there.
    /// </summary>
    /// <remarks>
    /// <b>This module is what makes something the player, and that is on purpose.</b> Being the
    /// player is not a property of data - it is the fact that a human is holding the controls. Two
    /// definitions that differ only in whether they carry this module are an ordinary NPC and a
    /// playable character, with no flag anywhere saying which is which.
    ///
    /// It is also why the entity kind enum was removed from the core on 2026-09-05. See
    /// <c>Agent Memory/decisions/D-001-remove-entity-kind.md</c>.
    ///
    /// <b>It installs no motor.</b> A locomotion module supplies that, and the two are separate
    /// because they answer different questions: what kind of legs, and who is steering. An entity
    /// with this and no motor is reported rather than given one, since guessing which motor a game
    /// wanted is exactly the kind of repair this framework refuses to make.
    /// </remarks>
    [CreateAssetMenu(fileName = "PlayerControlModule", menuName = "Graves/Modules/Player Control")]
    public class PlayerControlModule : DefinitionModule
    {
        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var host = _entity.gameObject;

            var motor = host.GetComponent<Motor>();
            if (motor == null)
            {
                Debug.LogError("[PlayerControl] '" + _entity.name + "' has a player control module " +
                               "but no motor, so nothing can move it. Add a locomotion module to '" +
                               (_entity.Definition != null ? _entity.Definition.DisplayName : "the definition") +
                               "'.", _entity);
                return;
            }

            var source = host.GetComponent<InputSource>();
            if (source == null) source = host.AddComponent<KeyboardMouseInput>();

            var brain = host.GetComponent<PlayerBrain>();
            if (brain == null) brain = host.AddComponent<PlayerBrain>();

            brain.Initialise(source, motor);
        }
    }
}
