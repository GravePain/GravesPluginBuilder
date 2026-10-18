using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Graves.Plugins.Animation;

namespace Graves.TestBed
{
    /// <summary>
    /// Presses a key, plays a named animation. Test scaffolding, not a controller.
    /// </summary>
    /// <remarks>
    /// <b>It lives in the test bed, and used to live in its own assembly inside the animation
    /// plugin.</b> The approved build list said that assembly should be retired; it was not, and a
    /// reviewer found it still present and still referenced a build later. Retiring it here rather
    /// than folding it into the input plugin, because that would have made scaffolding into a
    /// shipped feature - what this is for is pressing a key in a test scene to see whether a clip
    /// plays, and nothing outside the test bed should be able to reach it.
    ///
    /// Its cost while it existed was the one D-002 named: two things reading the keyboard in two
    /// assemblies. That is now one.
    /// </remarks>
    [RequireComponent(typeof(AnimationRunner))]
    public class AnimationKeyTrigger : MonoBehaviour
    {
        /// <summary>One key bound to one animation name.</summary>
        [Serializable]
        public class Binding
        {
            [Tooltip("Key to press.")]
            public Key Key = Key.Digit1;

            [Tooltip("The animation name to ask the runner for. Must match a name on the module.")]
            public string Animation = string.Empty;
        }

        [SerializeField]
        [Tooltip("Key bindings. Names must match those authored on the entity's animation module.")]
        private List<Binding> _bindings = new List<Binding>();

        private AnimationRunner _runner;

        private void Awake() => _runner = GetComponent<AnimationRunner>();

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _runner == null) return;

            for (int i = 0; i < _bindings.Count; i++)
            {
                var binding = _bindings[i];
                if (binding == null || string.IsNullOrEmpty(binding.Animation)) continue;

                // Key.None is not a real key and indexing the keyboard with it THROWS, every frame.
                // A new element added to a serialized list does not run field initialisers, so the
                // default a binding actually arrives with is None, not the one declared on the field.
                if (binding.Key == Key.None) continue;

                if (keyboard[binding.Key].wasPressedThisFrame) _runner.Play(binding.Animation);
            }
        }
    }
}
