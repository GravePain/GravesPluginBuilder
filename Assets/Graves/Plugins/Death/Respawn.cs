using Graves.Plugins.Locomotion;
using Graves.Plugins.Vitals;
using UnityEngine;

namespace Graves.Plugins.Death
{
    /// <summary>
    /// Brings a killed thing back: heals it, stands it up, and puts it somewhere safe.
    /// </summary>
    /// <remarks>
    /// <b>This exists because dying was indistinguishable from a bug.</b> A killed entity has its
    /// motor and collider switched off and simply stops answering the controls. Watching that with
    /// no health bar on screen, the honest reading is "I am stuck", and that is exactly how it was
    /// first reported. The framework was doing what it was told; the experience was unreadable.
    ///
    /// <b>Respawning is a game's decision, which is why it is its own module.</b> Plenty of games
    /// end the run instead, or leave a corpse to be revived by someone else. What is not optional is
    /// that death be legible, and this is the cheapest legible answer.
    ///
    /// <b>It re-enables what death disabled, rather than death knowing about it.</b> The sink turns
    /// the motor and controller off; this turns them back on. Neither knows the other exists, and an
    /// entity with a sink and no respawn simply stays down.
    /// </remarks>
    [DisallowMultipleComponent]
    public class Respawn : MonoBehaviour
    {
        private Health _health;
        private float _delay = 3f;
        private bool _returnToStart = true;

        private Vector3 _origin;
        private Quaternion _facing;
        private bool _waiting;
        private float _reviveAt;

        /// <summary>True while dead and waiting to come back.</summary>
        public bool IsWaiting => _waiting;

        /// <summary>Seconds until this thing returns. Zero when it is not waiting.</summary>
        public float Remaining => _waiting ? Mathf.Max(0f, _reviveAt - Time.time) : 0f;

        /// <summary>Wires the component to the health whose death it answers.</summary>
        public void Initialise(Health _target, float _afterSeconds, bool _goHome)
        {
            if (_health != null) _health.Died -= OnDied;

            _health = _target;
            _delay = Mathf.Max(0f, _afterSeconds);
            _returnToStart = _goHome;

            // Remembered here rather than at the moment of death, because by then the body may have
            // been moved, sunk into the ground, or pushed somewhere it should not return to.
            _origin = transform.position;
            _facing = transform.rotation;

            if (_health != null) _health.Died += OnDied;
        }

        private void OnDied(GameObject _killer)
        {
            if (_waiting) return;
            _waiting = true;
            _reviveAt = Time.time + _delay;

            Debug.Log("[Death] '" + name + "' was killed" +
                      (_killer != null ? " by '" + _killer.name + "'" : "") +
                      ". Back in " + _delay.ToString("0.0") + "s.", this);
        }

        private void Update()
        {
            if (!_waiting || Time.time < _reviveAt) return;
            _waiting = false;

            if (_returnToStart)
            {
                // Through the motor, not the transform. A character controller writes the transform
                // itself and will drag the body back; an agent on a navigation mesh needs its own
                // warp or it snaps to where it was. The motor knows which it is and this does not.
                var motor = GetComponent<Motor>();
                if (motor != null)
                {
                    motor.enabled = true;
                    var controller = GetComponent<CharacterController>();
                    if (controller != null) controller.enabled = true;

                    motor.Warp(_origin);
                    motor.Stop();
                }
                transform.rotation = _facing;
            }
            else
            {
                var motor = GetComponent<Motor>();
                if (motor != null) motor.enabled = true;
                var controller = GetComponent<CharacterController>();
                if (controller != null) controller.enabled = true;
            }

            // Healed last. Restoring health first would leave a live entity standing in the place it
            // died for as long as the rest of this takes, which is a frame in which monsters can
            // reach it and kill it again before it has moved.
            if (_health != null) _health.RestoreTo(_health.Max);

            Debug.Log("[Death] '" + name + "' is back.", this);
        }

        private void OnDestroy()
        {
            if (_health != null) _health.Died -= OnDied;
        }
    }
}
