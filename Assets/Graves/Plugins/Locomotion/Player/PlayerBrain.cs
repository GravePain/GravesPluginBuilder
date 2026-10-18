using Graves.Plugins.Input;
using UnityEngine;

namespace Graves.Plugins.Locomotion.Player
{
    /// <summary>
    /// Turns what a person is pressing into what a character wants to do. The player's half of the
    /// brain and motor split.
    /// </summary>
    /// <remarks>
    /// <b>This class is the only place that knows a human is involved.</b> It reads an input source,
    /// converts stick space into world space, and hands a <see cref="MoveIntent"/> to whatever motor
    /// the entity has. An AI brain does the same job from a path instead of a keyboard and produces
    /// the same intent, which is the entire justification for the split now that single-player has
    /// removed the networking one.
    ///
    /// <b>It holds no authored references.</b> Both the input source and the motor are found when
    /// the entity wakes. That is the rule the reference material earned the hard way: the reference framework
    /// abstracts movement properly and swapping it is still a manual re-wiring job, because every
    /// consumer serialises a reference to the component instance.
    /// </remarks>
    [DisallowMultipleComponent]
    public class PlayerBrain : MonoBehaviour
    {
        private InputSource _input;
        private Motor _motor;
        private Transform _view;
        private bool _warnedAboutView;

        /// <summary>
        /// Wires the brain to the parts it drives. Called by the module when the entity wakes.
        /// </summary>
        public void Initialise(InputSource _source, Motor _target)
        {
            _input = _source;
            _motor = _target;
        }

        private void Update()
        {
            if (_input == null || _motor == null) return;

            var state = _input.Read();
            var intent = MoveIntent.None;

            Vector3 forward = ViewForward();
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);

            intent.Move = right * state.Move.x + forward * state.Move.y;

            // Facing follows travel. A character that turns to look where it is going needs no
            // camera to do it, which keeps this working before a camera plugin exists and keeps the
            // brain from acquiring an opinion about framing.
            if (intent.Move.sqrMagnitude > 0.000001f) intent.Look = intent.Move;

            intent.Jump = state.Jump;

            _motor.SetIntent(intent);
        }

        /// <summary>
        /// The direction the player considers forward, flattened onto the ground.
        /// </summary>
        /// <remarks>
        /// Camera-relative when there is a camera, because pushing forward should move away from the
        /// viewer rather than along some world axis the player cannot see. Without one it falls back
        /// to world forward and says so once - a silent fallback here presents as controls that are
        /// rotated for no visible reason, which is a miserable thing to diagnose.
        /// </remarks>
        private Vector3 ViewForward()
        {
            if (_view == null)
            {
                var camera = Camera.main;
                if (camera != null) _view = camera.transform;
            }

            if (_view == null)
            {
                if (!_warnedAboutView)
                {
                    _warnedAboutView = true;
                    Debug.LogWarning("[PlayerBrain] '" + name + "' found no main camera, so movement " +
                                     "is relative to world forward rather than to the view.", this);
                }
                return Vector3.forward;
            }

            var flat = new Vector3(_view.forward.x, 0f, _view.forward.z);

            // Looking straight down or up leaves nothing to flatten. Falling back to the camera's up
            // vector recovers a usable heading instead of collapsing movement to zero.
            if (flat.sqrMagnitude < 0.000001f) flat = new Vector3(_view.up.x, 0f, _view.up.z);
            if (flat.sqrMagnitude < 0.000001f) return Vector3.forward;

            return flat.normalized;
        }
    }
}
