using Graves.Core;
using Graves.Plugins.Vitals;
using UnityEngine;

namespace Graves.Plugins.Death
{
    /// <summary>
    /// Says that this entity comes back after it is killed.
    /// </summary>
    /// <remarks>
    /// Separate from the death module because the two answer different questions: what happens to
    /// the body, and whether the thing returns. A monster has the first and not the second; a player
    /// usually has both.
    /// </remarks>
    [CreateAssetMenu(fileName = "RespawnModule", menuName = "Graves/Modules/Respawn")]
    public class RespawnModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("Seconds spent dead before coming back.")]
        [Min(0f)] private float _delay = 3f;

        [SerializeField]
        [Tooltip("Return to where this entity started. Off, it comes back where it fell - which is " +
                 "usually surrounded by whatever killed it.")]
        private bool _returnToStart = true;

        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var health = _entity.GetComponent<Health>();
            if (health == null)
            {
                Debug.LogError("[Death] '" + _entity.name + "' is set to respawn but has no vitals, " +
                               "so it can never die in the first place.", _entity);
                return;
            }

            var respawn = _entity.GetComponent<Respawn>();
            if (respawn == null) respawn = _entity.gameObject.AddComponent<Respawn>();

            respawn.Initialise(health, _delay, _returnToStart);
        }
    }
}
