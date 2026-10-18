using UnityEngine;

namespace Graves.Plugins.Locomotion
{
    /// <summary>
    /// A motor built on Unity's <c>CharacterController</c>: walks, turns, falls and jumps, and
    /// slides along whatever it hits.
    /// </summary>
    /// <remarks>
    /// The default because it is the least surprising - it collides with the world, it is affected
    /// by gravity, and it needs nothing baked. A navigation-mesh motor is a sibling of this class
    /// rather than a mode inside it, so neither one carries the other's concepts.
    ///
    /// <b>Vertical and horizontal motion are tracked separately</b> and combined only at the moment
    /// of moving. Keeping them apart is what allows a jump to survive a change of direction in the
    /// air, and it means a horizontal intent of zero does not cancel a fall.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public class CharacterControllerMotor : Motor
    {
        private CharacterController _controller;
        private MoveIntent _intent;

        private Vector3 _horizontal;
        private float _vertical;

        private float _speed = 4f;
        private float _turnDegreesPerSecond = 720f;
        private float _acceleration = 40f;
        private float _jumpHeight = 1.2f;
        private float _gravity = 25f;

        /// <inheritdoc />
        public override Vector3 Velocity => _horizontal + Vector3.up * _vertical;

        /// <inheritdoc />
        public override bool IsGrounded => _controller != null && _controller.isGrounded;

        /// <summary>
        /// Applies the authored settings. Called by the module when the entity wakes, so the motor
        /// never reads a definition and never learns what a module is.
        /// </summary>
        public void Configure(float _walkSpeed, float _turnSpeed, float _accelerationRate,
                              float _jumpHeightMetres, float _gravityStrength)
        {
            _speed = Mathf.Max(0f, _walkSpeed);
            _turnDegreesPerSecond = Mathf.Max(0f, _turnSpeed);
            _acceleration = Mathf.Max(0.01f, _accelerationRate);
            _jumpHeight = Mathf.Max(0f, _jumpHeightMetres);
            _gravity = Mathf.Max(0f, _gravityStrength);
        }

        /// <inheritdoc />
        public override void SetIntent(MoveIntent _newIntent)
        {
            // A jump is a single-frame request and the motor consumes it on its own schedule, so an
            // incoming false must never erase one that has not been acted on yet. Without this, a
            // brain setting intent more often than the motor runs can silently lose the jump.
            bool jumpStillPending = _intent.Jump && !_newIntent.Jump;
            _intent = _newIntent;
            if (jumpStillPending) _intent.Jump = true;
        }

        /// <inheritdoc />
        public override void SetSpeed(float _metresPerSecond) => _speed = Mathf.Max(0f, _metresPerSecond);

        /// <inheritdoc />
        public override void Warp(Vector3 _position)
        {
            // The controller writes the transform itself and will fight an external assignment, so
            // it is disabled across the move. Cheaper than it looks and the only reliable way.
            if (_controller == null) _controller = GetComponent<CharacterController>();

            bool wasEnabled = _controller.enabled;
            _controller.enabled = false;
            transform.position = _position;
            _controller.enabled = wasEnabled;

            _horizontal = Vector3.zero;
            _vertical = 0f;
        }

        /// <inheritdoc />
        public override void Stop()
        {
            _intent = MoveIntent.None;
            _horizontal = Vector3.zero;
            _vertical = 0f;
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            if (delta <= 0f) return;

            Turn(delta);
            Fall(delta);
            Travel(delta);

            _controller.Move((_horizontal + Vector3.up * _vertical) * delta);
        }

        /// <summary>Rotates toward the wanted facing, on the horizontal plane only.</summary>
        /// <remarks>
        /// Y axis only, deliberately. Facing a target that is above or below should never tilt a
        /// character over, and every movement system that has forgotten this has produced the same
        /// comical result.
        /// </remarks>
        private void Turn(float _delta)
        {
            if (!_intent.WantsToTurn) return;

            var flat = new Vector3(_intent.Look.x, 0f, _intent.Look.z);
            if (flat.sqrMagnitude < 0.000001f) return;

            var wanted = Quaternion.LookRotation(flat.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted,
                                                          _turnDegreesPerSecond * _delta);
        }

        /// <summary>Applies gravity, and consumes a pending jump when there is ground to push off.</summary>
        private void Fall(float _delta)
        {
            if (_controller.isGrounded)
            {
                // A small downward bias while grounded. Left at zero, the controller reports itself
                // ungrounded every other frame on flat ground and the character reads as jittery.
                if (_vertical < 0f) _vertical = -2f;

                if (_intent.Jump && _jumpHeight > 0f)
                {
                    _vertical = Mathf.Sqrt(2f * _gravity * _jumpHeight);
                }
            }
            else
            {
                _vertical -= _gravity * _delta;
            }

            // Consumed either way: a jump requested in mid-air is refused rather than remembered,
            // because a queued jump firing on landing is not what anybody pressed the key for.
            _intent.Jump = false;
        }

        /// <summary>Eases horizontal velocity toward what the intent asks for.</summary>
        /// <remarks>
        /// Accelerated rather than assigned, so that releasing a key decelerates instead of stopping
        /// dead. The magnitude of the intent carries throttle, so a half-pushed stick produces a
        /// half-speed walk with no separate field.
        /// </remarks>
        private void Travel(float _delta)
        {
            var flat = new Vector3(_intent.Move.x, 0f, _intent.Move.z);
            float throttle = Mathf.Clamp01(flat.magnitude);
            Vector3 wanted = throttle > 0.000001f ? flat.normalized * (_speed * throttle) : Vector3.zero;

            _horizontal = Vector3.MoveTowards(_horizontal, wanted, _acceleration * _delta);
        }
    }
}
