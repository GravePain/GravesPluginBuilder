using Graves.Plugins.Input;
using UnityEngine;

namespace Graves.Plugins.Camera
{
    /// <summary>
    /// Keeps the camera behind a target and lets the viewer swing it around.
    /// </summary>
    /// <remarks>
    /// <b>Built as an instrument before it is a feature.</b> Locomotion is judged by eye and by
    /// nothing else - it passed every measurement on a previous animation path and was still visibly
    /// wrong. Without a camera that follows and can be turned, nobody can say whether movement is
    /// right, and the camera-relative half of the player brain cannot be exercised at all.
    ///
    /// <b>Orbit is here for that reason rather than for polish.</b> A camera fixed on one axis never
    /// tests whether pushing forward means away from the viewer, which is the one thing that
    /// conversion exists to do.
    ///
    /// Modes, blending, target lock and cinematic framing are deliberately absent. They are a camera
    /// system; this is a camera.
    /// </remarks>
    [DisallowMultipleComponent]
    public class FollowCamera : MonoBehaviour
    {
        private Transform _target;
        private InputSource _input;

        private float _distance = 5f;
        private float _height = 1.6f;
        private float _damping = 12f;
        private float _yawSpeed = 0.2f;
        private float _pitchSpeed = 0.15f;
        private float _minPitch = -30f;
        private float _maxPitch = 70f;

        private float _yaw;
        private float _pitch = 15f;

        /// <summary>Who is being followed. Null means the camera holds still.</summary>
        public Transform Target => _target;

        /// <summary>
        /// Points the camera at something and applies the authored settings.
        /// </summary>
        /// <remarks>
        /// The input source is optional. Given one the camera can be swung; without one it simply
        /// trails, which is the right behaviour for a camera watching something that is not being
        /// driven by a person.
        /// </remarks>
        public void Follow(Transform _subject, InputSource _source, float _followDistance,
                           float _eyeHeight, float _followDamping, float _yawSensitivity,
                           float _pitchSensitivity, float _pitchFloor, float _pitchCeiling)
        {
            _target = _subject;
            _input = _source;
            _distance = Mathf.Max(0.1f, _followDistance);
            _height = _eyeHeight;
            _damping = Mathf.Max(0f, _followDamping);
            _yawSpeed = Mathf.Max(0f, _yawSensitivity);
            _pitchSpeed = Mathf.Max(0f, _pitchSensitivity);
            _minPitch = _pitchFloor;
            _maxPitch = _pitchCeiling;

            if (_target != null) _yaw = _target.eulerAngles.y;

            // Placed immediately rather than eased in from wherever the camera happened to sit, so
            // the first frame of a session does not open on a swoop across the level.
            Place(1f);
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            if (_input != null)
            {
                var look = _input.Read().Look;
                _yaw += look.x * _yawSpeed;

                // Inverted deliberately: pushing the stick or mouse forward lowers the camera to
                // look down, which is what almost every third-person game does and what hands
                // expect.
                _pitch = Mathf.Clamp(_pitch - look.y * _pitchSpeed, _minPitch, _maxPitch);
            }

            // Framed in LateUpdate so the subject has already moved this frame. Doing it in Update
            // means always framing where the target was, which reads as a camera lagging behind by
            // exactly one frame and is very hard to see and very easy to feel.
            Place(_damping <= 0f ? 1f : 1f - Mathf.Exp(-_damping * Time.deltaTime));
        }

        /// <summary>Moves the camera toward where it should be, by a fraction of the way.</summary>
        /// <remarks>
        /// The caller passes a fraction already corrected for frame time, using an exponential decay
        /// rather than a raw multiplication. A plain lerp by <c>damping * deltaTime</c> changes how
        /// tight the camera feels when the frame rate changes, which is the kind of thing that gets
        /// blamed on the movement code.
        /// </remarks>
        private void Place(float _fraction)
        {
            var pivot = _target.position + Vector3.up * _height;
            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            var wanted = pivot - rotation * Vector3.forward * _distance;

            transform.position = Vector3.Lerp(transform.position, wanted, Mathf.Clamp01(_fraction));
            transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        }
    }
}
