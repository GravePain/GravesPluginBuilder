using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Locomotion
{
    /// <summary>
    /// Gives an entity the ability to move: how fast it walks, how sharply it turns, how high it
    /// jumps, and the size of the body doing it.
    /// </summary>
    /// <remarks>
    /// This module installs a motor and nothing else. It does not decide where the entity goes -
    /// that is a brain, it lives in another assembly, and an entity with this module and no brain
    /// simply stands there, which is correct for a thing that has legs and no reason to use them.
    ///
    /// <b>The body dimensions are here rather than on a prefab</b> because they are a property of
    /// what the thing is. A goblin is goblin-sized in every scene it appears in, and authoring that
    /// once on the definition beats setting it on each prefab and hoping they agree.
    /// </remarks>
    [CreateAssetMenu(fileName = "LocomotionModule", menuName = "Graves/Modules/Locomotion")]
    public class LocomotionModule : DefinitionModule
    {
        [Header("Movement")]
        [SerializeField]
        [Tooltip("Metres per second at full throttle. A brisk human walk is about 2, a jog about 4.")]
        [Min(0f)] private float _speed = 4f;

        [SerializeField]
        [Tooltip("Degrees per second of turning. High values snap to the wanted facing; low values " +
                 "read as a heavy thing that has to swing around.")]
        [Min(0f)] private float _turnSpeed = 720f;

        [SerializeField]
        [Tooltip("How quickly speed is reached and lost, in metres per second per second. Large " +
                 "values feel responsive and arcade-like; small values feel weighty and slippery.")]
        [Min(0.01f)] private float _acceleration = 40f;

        [Header("Falling")]
        [SerializeField]
        [Tooltip("Peak jump height in metres. Zero means this entity cannot jump.")]
        [Min(0f)] private float _jumpHeight = 1.2f;

        [SerializeField]
        [Tooltip("Downward acceleration. Earth is 9.81, and almost every game that felt right used " +
                 "more - this is a tuning value, not a physical constant.")]
        [Min(0f)] private float _gravity = 25f;

        [Header("Body")]
        [SerializeField]
        [Tooltip("Height of the collision capsule in metres.")]
        [Min(0.1f)] private float _height = 1.8f;

        [SerializeField]
        [Tooltip("Radius of the collision capsule in metres.")]
        [Min(0.01f)] private float _radius = 0.3f;

        [SerializeField]
        [Tooltip("How far the capsule may step up without jumping.")]
        [Min(0f)] private float _stepOffset = 0.3f;

        [SerializeField]
        [Tooltip("Steepest slope in degrees that can be walked up.")]
        [Range(0f, 90f)] private float _slopeLimit = 45f;

        /// <summary>Metres per second at full throttle.</summary>
        public float Speed => _speed;

        /// <summary>
        /// Installs a motor on the entity and configures it from the authored values.
        /// </summary>
        /// <remarks>
        /// The collision capsule is created and sized here, before the motor is added, so the motor
        /// never has to cope with a controller it did not expect. It is centred at half its own
        /// height so the capsule sits on the entity's origin rather than straddling it - a body
        /// buried to its waist in the floor is the usual result of forgetting this.
        /// </remarks>
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var host = _entity.gameObject;

            var controller = host.GetComponent<CharacterController>();
            if (controller == null) controller = host.AddComponent<CharacterController>();

            controller.height = Mathf.Max(_height, _radius * 2f);
            controller.radius = _radius;
            controller.center = new Vector3(0f, controller.height * 0.5f, 0f);
            controller.stepOffset = Mathf.Min(_stepOffset, controller.height);
            controller.slopeLimit = _slopeLimit;

            var existing = host.GetComponent<Motor>();
            if (existing != null && !(existing is CharacterControllerMotor))
            {
                // Two motors on one entity is a configuration error rather than a merge to resolve,
                // and picking one silently would leave the other quietly doing nothing.
                Debug.LogError("[Locomotion] '" + _entity.name + "' already has a " +
                               existing.GetType().Name + ". An entity may only have one motor, so " +
                               "the locomotion module did not install its own.", _entity);
                return;
            }

            var motor = existing as CharacterControllerMotor;
            if (motor == null) motor = host.AddComponent<CharacterControllerMotor>();

            motor.Configure(_speed, _turnSpeed, _acceleration, _jumpHeight, _gravity);
        }
    }
}
