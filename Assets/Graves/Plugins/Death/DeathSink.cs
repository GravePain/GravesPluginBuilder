using Graves.Plugins.Locomotion;
using Graves.Plugins.Vitals;
using UnityEngine;

namespace Graves.Plugins.Death
{
    /// <summary>
    /// Makes a killed thing visibly stop being a thing: it halts, stops blocking the way, sinks into
    /// the ground, and is removed.
    /// </summary>
    /// <remarks>
    /// <b>This plugin exists because of a rule that was written and then not applied to its
    /// author.</b> Death first lived inside the AI plugin, as a compromise between two bad options:
    /// put it there, or give vitals a dependency on locomotion and physics in order to stop a body.
    /// Both were wrong, and the rule for choosing was already written down - a part carrying a
    /// dependency the rest should not inherit gets its own assembly.
    ///
    /// Death depends on vitals, because it needs to know something died, and on locomotion, because
    /// it needs to stop a body moving. **Neither of those should depend on the other**, and a game
    /// with no AI at all still wants its player to die. That is a cross-cutting concern, and the
    /// standard answer to a cross-cutting concern is its own assembly. Caught by the lead agent
    /// during a retrospective review; see <c>Agent Memory/journal.md</c>.
    ///
    /// <b>It knows nothing about brains.</b> An earlier version reached into the AI plugin to switch
    /// a chase brain off, which is what forced it to live there. It does not need to: a brain that
    /// checks whether it is dead stops on its own, and a motor that has been stopped and disabled
    /// goes nowhere regardless of what is still writing intent at it.
    ///
    /// <b>Still placeholder presentation.</b> A ragdoll, a death animation and a loot drop are all
    /// better answers and all decisions a particular game makes. This exists because the loop is
    /// killing things, and a loop whose last beat is an enemy standing perfectly still with no
    /// health left does not read as a kill.
    /// </remarks>
    [DisallowMultipleComponent]
    public class DeathSink : MonoBehaviour
    {
        private Health _health;
        private float _linger = 1.5f;
        private float _sinkSpeed = 0.8f;
        private bool _removeBody = true;

        private bool _dying;
        private float _sinkAt;
        private float _removeAt;

        /// <summary>True once this thing has died and the sequence has begun.</summary>
        public bool IsDying => _dying;

        /// <summary>Wires the component to the health it is watching.</summary>
        public void Initialise(Health _target, float _lingerSeconds, float _sinkMetresPerSecond,
                               bool _removeAfterwards)
        {
            if (_health != null) _health.Died -= OnDied;

            _health = _target;
            _linger = Mathf.Max(0f, _lingerSeconds);
            _sinkSpeed = Mathf.Max(0f, _sinkMetresPerSecond);
            _removeBody = _removeAfterwards;

            if (_health != null) _health.Died += OnDied;
        }

        private void OnDied(GameObject _killer)
        {
            if (_dying) return;
            _dying = true;

            _sinkAt = Time.time + _linger;

            // Two seconds of sinking is enough at any sane speed, and a fixed budget means a body
            // cannot survive forever because someone authored a sink speed of zero.
            _removeAt = _sinkAt + 2f;

            var motor = GetComponent<Motor>();
            if (motor != null)
            {
                motor.Stop();
                motor.enabled = false;
            }

            // Disabled rather than destroyed. A corpse that still blocks a doorway is worse than no
            // corpse at all, and destroying the controller outright would fight anything else still
            // holding a reference to it this frame.
            var controller = GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
        }

        private void Update()
        {
            if (!_dying || !_removeBody) return;
            if (Time.time < _sinkAt) return;

            transform.position += Vector3.down * (_sinkSpeed * Time.deltaTime);

            if (Time.time >= _removeAt) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_health != null) _health.Died -= OnDied;
        }
    }
}
