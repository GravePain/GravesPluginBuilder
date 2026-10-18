using System;
using System.Collections.Generic;
using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Animation
{
    /// <summary>
    /// One authored animation: a name to ask for it by, the clip, and how it blends.
    /// </summary>
    [Serializable]
    public class NamedClip
    {
        [Tooltip("What gameplay asks for. Free text - the runner matches on this, not on the clip's " +
                 "asset name, so renaming the clip file cannot break a call.")]
        public string Name = "New Animation";

        [Tooltip("The clip to play.")]
        public AnimationClip Clip;

        [Tooltip("Playback speed multiplier.")]
        [Min(0.01f)] public float Speed = 1f;

        [Tooltip("Seconds to cross-fade in from whatever the Animator is currently playing. Zero " +
                 "snaps.")]
        [Min(0f)] public float BlendIn = 0.15f;

        [Tooltip("Seconds to cross-fade back to the Animator as the clip ends.")]
        [Min(0f)] public float BlendOut = 0.15f;

        [Tooltip("Optional mask. With one set, the clip plays only on the masked bones and the " +
                 "Animator keeps driving the rest - an upper-body attack over a running lower body.")]
        public AvatarMask Mask;
    }

    /// <summary>
    /// Gives an entity a set of named animation clips that can be played on demand, without any of
    /// them existing as states in an Animator Controller.
    /// </summary>
    /// <remarks>
    /// This is a plugin. The core stores it, shows it in the authoring window, and knows nothing else
    /// about it - the dependency runs one way, enforced by the assembly definition.
    ///
    /// The problem being solved: Mecanim can only play a clip that is a state in a controller, so a
    /// game with a hundred abilities needs a controller graph nobody can maintain. The runner attached
    /// here bypasses the controller for one-off clips and hands control straight back afterwards.
    /// </remarks>
    [CreateAssetMenu(fileName = "AnimationModule", menuName = "Graves/Modules/Animation")]
    public class AnimationModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("The clips this entity can be asked to play, by name.")]
        private List<NamedClip> _clips = new List<NamedClip>();

        /// <summary>The authored clips.</summary>
        public IReadOnlyList<NamedClip> Clips => _clips;

        /// <summary>
        /// Finds a clip by name, case-insensitively. Returns null when there is no such name, which
        /// the runner reports rather than silently doing nothing.
        /// </summary>
        public NamedClip Find(string _name)
        {
            if (string.IsNullOrEmpty(_name)) return null;

            for (int i = 0; i < _clips.Count; i++)
            {
                var entry = _clips[i];
                if (entry != null && string.Equals(entry.Name, _name, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// Attaches a runner to the live entity. This is the whole of the core's involvement - it
        /// calls this once and then leaves the plugin to it.
        /// </summary>
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var runner = _entity.gameObject.GetComponent<AnimationRunner>();
            if (runner == null) runner = _entity.gameObject.AddComponent<AnimationRunner>();

            runner.Initialise(this, _entity);
        }
    }
}
