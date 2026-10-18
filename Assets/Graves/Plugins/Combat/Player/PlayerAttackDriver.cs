using Graves.Core;
using Graves.Plugins.Input;
using UnityEngine;

namespace Graves.Plugins.Combat.Player
{
    /// <summary>
    /// Reads the attack button and swings when it is pressed.
    /// </summary>
    /// <remarks>
    /// <b>Deliberately trivial.</b> Every decision about whether an attack is allowed lives in the
    /// attacker: the cooldown, the reach, the arc. This asks once per frame and passes the answer
    /// on, which is what keeps a monster's brain and a person's finger interchangeable.
    /// </remarks>
    [DisallowMultipleComponent]
    public class PlayerAttackDriver : MonoBehaviour
    {
        private InputSource _input;
        private Attacker _attacker;

        /// <summary>Wires the driver to the parts it reads and drives.</summary>
        public void Initialise(InputSource _source, Attacker _target)
        {
            _input = _source;
            _attacker = _target;
        }

        private void Update()
        {
            if (_input == null || _attacker == null) return;
            if (!_input.Read().Attack) return;

            // Nothing is done with the result. A refused swing is not an error and a missed one is
            // not either, and a driver that logged both would fill the console with a person
            // playing the game.
            _attacker.TryAttack();
        }
    }
}
