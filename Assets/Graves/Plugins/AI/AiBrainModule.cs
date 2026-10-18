using Graves.Core;
using Graves.Plugins.Locomotion;
using UnityEngine;

namespace Graves.Plugins.AI
{
    /// <summary>
    /// Puts a brain in an entity so it chases things by itself.
    /// </summary>
    /// <remarks>
    /// The counterpart to the player control module, and deliberately interchangeable with it. Swap
    /// one for the other on a definition and the same body is driven by a person instead of by a
    /// machine, with no other change anywhere.
    /// </remarks>
    [CreateAssetMenu(fileName = "AiBrainModule", menuName = "Graves/Modules/AI Brain")]
    public class AiBrainModule : DefinitionModule
    {
        [Header("Senses")]
        [SerializeField]
        [Tooltip("How far away a target can be noticed, in metres.")]
        [Min(0f)] private float _sightRange = 12f;

        [SerializeField]
        [Tooltip("How far away a target must get before the chase is abandoned. Never less than " +
                 "sight range, or the chase flickers on and off.")]
        [Min(0f)] private float _giveUpRange = 20f;

        [SerializeField]
        [Tooltip("How close to get before stopping. Set this a little inside attack reach, or the " +
                 "chaser stands just too far away to land a blow.")]
        [Min(0f)] private float _stopDistance = 1.6f;

        /// <inheritdoc />
        /// <remarks>
        /// A missing motor is reported rather than installed. Guessing which kind of legs a game
        /// wanted is precisely the repair this framework refuses to make, and an AI silently given a
        /// physics motor when the game meant to use navigation would be a very quiet wrong answer.
        /// </remarks>
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var host = _entity.gameObject;

            var motor = host.GetComponent<Motor>();
            if (motor == null)
            {
                Debug.LogError("[AI] '" + _entity.name + "' has a brain but no motor, so it can " +
                               "decide where to go and never get there. Add a locomotion module.",
                               _entity);
                return;
            }

            // Optional. Something without vitals cannot die, so it simply never stops chasing, which
            // is correct for a thing that cannot be killed.
            var mortal = host.GetComponent<IMortal>();

            var brain = host.GetComponent<ChaseBrain>();
            if (brain == null) brain = host.AddComponent<ChaseBrain>();

            brain.Initialise(motor, mortal, _sightRange, _stopDistance, _giveUpRange);

            // What happens to the body afterwards is not this plugin's business. Death is its own
            // plugin because it depends on vitals and on locomotion while neither depends on the
            // other, and because a game with no AI still wants its player to die.
        }
    }
}
