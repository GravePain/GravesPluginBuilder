using Graves.Core;
using Graves.Plugins.Vitals;
using UnityEngine;

namespace Graves.Plugins.Lifecycle.Vitals
{
    /// <summary>
    /// Says that the game is over when this entity dies.
    /// </summary>
    /// <remarks>
    /// Put this on the player for a game with one life, or on an escort target for a game where
    /// losing them is losing.
    /// </remarks>
    [CreateAssetMenu(fileName = "LoseOnDeathModule", menuName = "Graves/Modules/Lose On Death")]
    public class LoseOnDeathModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("Shown when the game ends this way.")]
        private string _reason = "You died.";

        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var health = _entity.GetComponent<Health>();
            if (health == null)
            {
                Debug.LogError("[Session] " + _entity.name + " is supposed to end the game by dying " +
                               "but has no vitals, so it never can.", _entity);
                return;
            }

            // Two answers to one exit. Respawn says death continues the run; this says it stops it.
            // Whichever won would be an accident of ordering, so it is reported instead.
            if (_entity.GetComponent<Death.Respawn>() != null)
            {
                Debug.LogError("[Session] " + _entity.name + " both respawns and ends the game when " +
                               "it dies. Those contradict, and which one takes effect would depend " +
                               "on module order. Remove one.", _entity);
            }

            var ending = _entity.GetComponent<EndWhenThisDies>();
            if (ending == null) ending = _entity.gameObject.AddComponent<EndWhenThisDies>();

            ending.Initialise(health, _reason);
        }
    }
}
