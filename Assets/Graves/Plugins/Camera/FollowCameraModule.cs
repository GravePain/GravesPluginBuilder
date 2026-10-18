using Graves.Core;
using Graves.Plugins.Input;
using UnityEngine;

namespace Graves.Plugins.Camera
{
    /// <summary>
    /// Says that the camera should watch this entity, and how closely.
    /// </summary>
    /// <remarks>
    /// <b>The camera is claimed rather than created.</b> A scene already has a camera, and spawning
    /// a second one produces two views, an audio listener warning, and a black screen if the wrong
    /// one wins. Taking over the existing main camera is the behaviour that surprises nobody.
    ///
    /// <b>Nothing is dragged into a field to make this work.</b> The entity that carries this module
    /// is the subject, found at the moment it wakes.
    /// </remarks>
    [CreateAssetMenu(fileName = "FollowCameraModule", menuName = "Graves/Modules/Follow Camera")]
    public class FollowCameraModule : DefinitionModule
    {
        [Header("Framing")]
        [SerializeField]
        [Tooltip("Metres behind the subject.")]
        [Min(0.1f)] private float _distance = 5f;

        [SerializeField]
        [Tooltip("Height above the subject's origin that the camera looks at. Roughly head height " +
                 "reads better than the feet.")]
        private float _height = 1.6f;

        [SerializeField]
        [Tooltip("How quickly the camera catches up. Higher is tighter; zero pins it rigidly to " +
                 "the subject.")]
        [Min(0f)] private float _damping = 12f;

        [Header("Orbit")]
        [SerializeField]
        [Tooltip("Degrees of horizontal swing per unit of look input.")]
        [Min(0f)] private float _yawSensitivity = 0.2f;

        [SerializeField]
        [Tooltip("Degrees of vertical swing per unit of look input.")]
        [Min(0f)] private float _pitchSensitivity = 0.15f;

        [SerializeField]
        [Tooltip("How far below the horizon the camera may rise to look down.")]
        [Range(-89f, 0f)] private float _minPitch = -30f;

        [SerializeField]
        [Tooltip("How far above the horizon the camera may drop to look up.")]
        [Range(0f, 89f)] private float _maxPitch = 70f;

        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var camera = UnityEngine.Camera.main;
            if (camera == null)
            {
                Debug.LogError("[FollowCamera] '" + _entity.name + "' asks to be followed but the " +
                               "scene has no camera tagged MainCamera, so nothing is watching it.",
                               _entity);
                return;
            }

            var follow = camera.GetComponent<FollowCamera>();
            if (follow == null) follow = camera.gameObject.AddComponent<FollowCamera>();

            if (follow.Target != null && follow.Target != _entity.transform)
            {
                // Two entities asking for the same camera is a configuration error, and the last one
                // to wake would silently win. Reported, and the first claim stands.
                Debug.LogWarning("[FollowCamera] '" + _entity.name + "' asked for the main camera, " +
                                 "which is already following '" + follow.Target.name + "'. The " +
                                 "first claim was kept.", _entity);
                return;
            }

            // Optional on purpose: an entity with no input source is followed but cannot be orbited.
            var source = _entity.GetComponent<InputSource>();

            follow.Follow(_entity.transform, source, _distance, _height, _damping,
                          _yawSensitivity, _pitchSensitivity, _minPitch, _maxPitch);
        }
    }
}
