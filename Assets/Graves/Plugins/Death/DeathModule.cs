using Graves.Core;
using Graves.Plugins.Vitals;
using UnityEngine;

namespace Graves.Plugins.Death
{
    /// <summary>
    /// Says what happens to this entity's body once it is killed.
    /// </summary>
    /// <remarks>
    /// Applies to anything with health, which now includes the player. That is the point of moving
    /// it out of the AI plugin: a person dying and a monster dying are the same event, and only one
    /// of them was being handled while this lived with the brains.
    /// </remarks>
    [CreateAssetMenu(fileName = "DeathModule", menuName = "Graves/Modules/Death")]
    public class DeathModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("Remove the body after it dies. Turn this off for something that should lie where " +
                 "it fell.")]
        private bool _removeBody = true;

        [SerializeField]
        [Tooltip("Seconds the body lies still before it starts sinking.")]
        [Min(0f)] private float _linger = 1.5f;

        [SerializeField]
        [Tooltip("Metres per second the body sinks once it starts.")]
        [Min(0f)] private float _sinkSpeed = 0.8f;

        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var host = _entity.gameObject;

            var health = host.GetComponent<Health>();
            if (health == null)
            {
                Debug.LogError("[Death] '" + _entity.name + "' says what to do when it dies but has " +
                               "no vitals, so it can never die. Add a vitals module.", _entity);
                return;
            }

            var sink = host.GetComponent<DeathSink>();
            if (sink == null) sink = host.AddComponent<DeathSink>();

            sink.Initialise(health, _linger, _sinkSpeed, _removeBody);
        }
    }
}
