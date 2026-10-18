using Graves.Core;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Graves.Plugins.Animation
{
    /// <summary>
    /// Plays a named clip on an entity by taking over the Animator's output for the duration, then
    /// handing it straight back - so an ability animation never has to exist as a state in an
    /// Animator Controller.
    /// </summary>
    /// <remarks>
    /// <b>The graph.</b> One <see cref="AnimationMixerPlayable"/> with two inputs, cross-faded by
    /// weight:
    /// <code>
    /// AnimationPlayableOutput(animator)
    ///   └── AnimationMixerPlayable
    ///         ├── 0: the clip the Animator is currently playing   (blend FROM)
    ///         └── 1: the requested clip, optionally behind a mask (blend TO)
    /// </code>
    ///
    /// <b>Input 0 is the whole trick.</b> To cross-fade smoothly OUT of Mecanim you have to know what
    /// Mecanim is doing, so the current clip is read back off the Animator and rebuilt as this graph's
    /// own playable, at the same speed and at the same point in its cycle. Without that, taking over
    /// the output snaps the character to the new clip's first frame.
    ///
    /// <b>One graph, built once.</b> The source technique created a fresh PlayableGraph per call and
    /// kept a list of graphs it had failed to clean up. Here the graph is built in
    /// <see cref="Initialise"/> and only the clip playables are swapped, which removes that entire
    /// class of leak, along with the re-entrancy delay that existed to paper over it. Blending is
    /// weight over a real duration in <c>Update</c>, not a coroutine ticking a fixed wait.
    /// </remarks>
    [DisallowMultipleComponent]
    public class AnimationRunner : MonoBehaviour
    {
        private enum Phase
        {
            Idle,
            BlendingIn,
            Holding,
            BlendingOut
        }

        private AnimationModule _module;
        private Animator _animator;

        private PlayableGraph _graph;
        private AnimationPlayableOutput _output;
        private AnimationMixerPlayable _mixer;

        private AnimationClipPlayable _fromPlayable;
        private AnimationClipPlayable _toPlayable;
        private AnimationLayerMixerPlayable _maskMixer;
        private AnimationClipPlayable _maskBase;

        private Phase _phase = Phase.Idle;
        private NamedClip _current;
        private float _weight;
        private float _holdUntil;
        private bool _ready;

        // Where the Animator's clip was when it was captured. Needed twice: once for the blend-from
        // input, and again for the mask's base layer, which is a separate playable of the same clip.
        private float _capturedTime;
        private float _capturedSpeed = 1f;

        /// <summary>The clip currently playing, or null when the Animator has control.</summary>
        public string Playing => _phase == Phase.Idle || _current == null ? null : _current.Name;

        /// <summary>True while this runner is driving the Animator's output.</summary>
        public bool IsPlaying => _phase != Phase.Idle;

        /// <summary>
        /// Binds the runner to its module and the entity's body, and builds the graph.
        /// </summary>
        /// <remarks>
        /// The Animator is looked for on the spawned representation rather than on the entity, because
        /// the body is a child the core instantiated. One is added if the body has none: Playables
        /// output requires an Animator, but it does NOT require a controller, which is what lets this
        /// drive a bare placeholder with no Mecanim setup at all.
        /// </remarks>
        public void Initialise(AnimationModule _animationModule, GameEntity _entity)
        {
            _module = _animationModule;

            var body = _entity != null && _entity.Representation != null
                ? _entity.Representation
                : gameObject;

            _animator = body.GetComponentInChildren<Animator>();
            if (_animator == null)
            {
                _animator = body.AddComponent<Animator>();
                Debug.Log("[AnimationRunner] '" + name + "' had no Animator on its body - one was " +
                          "added. Playables output needs an Animator, but not a controller.", this);
            }

            BuildGraph();
        }

        /// <summary>
        /// Plays a named clip. Returns false, loudly, when the name is not authored.
        /// </summary>
        /// <remarks>
        /// A missing name is reported rather than ignored. A runner that silently does nothing is
        /// indistinguishable from one that is broken, and the difference matters at 2am.
        /// </remarks>
        public bool Play(string _name)
        {
            if (!_ready)
            {
                Debug.LogError("[AnimationRunner] '" + name + "' was asked for '" + _name +
                               "' before it was initialised.", this);
                return false;
            }

            var entry = _module != null ? _module.Find(_name) : null;
            if (entry == null)
            {
                Debug.LogWarning("[AnimationRunner] '" + name + "' has no animation named '" + _name +
                                 "'. Nothing played.", this);
                return false;
            }

            if (entry.Clip == null)
            {
                Debug.LogWarning("[AnimationRunner] '" + _name + "' is authored but has no clip " +
                                 "assigned. Nothing played.", this);
                return false;
            }

            _current = entry;
            ReleaseClipPlayables();

            bool blendable = CaptureAnimatorClip();
            BuildTargetPlayable(entry);

            // With nothing to blend from - no controller, or the Animator is mid-transition and
            // reports no clip - a cross-fade would fade in from an empty input and show the rest
            // pose. Go straight to full weight instead and accept the snap; it is the honest
            // result of there being no pose to leave.
            _weight = blendable && entry.BlendIn > 0f ? 0f : 1f;
            ApplyWeight();

            _graph.Play();

            float duration = entry.Clip.length / Mathf.Max(entry.Speed, 0.01f);
            _holdUntil = Time.time + Mathf.Max(0f, duration - entry.BlendOut);
            _phase = _weight >= 1f ? Phase.Holding : Phase.BlendingIn;

            return true;
        }

        /// <summary>Blends back to the Animator early, using the current clip's blend-out time.</summary>
        public void Stop()
        {
            if (_phase == Phase.Idle || _phase == Phase.BlendingOut) return;
            _phase = Phase.BlendingOut;
        }

        private void Update()
        {
            if (!_ready || _phase == Phase.Idle) return;

            switch (_phase)
            {
                case Phase.BlendingIn:
                    _weight += StepFor(_current.BlendIn);
                    if (_weight >= 1f)
                    {
                        _weight = 1f;
                        _phase = Phase.Holding;
                    }
                    ApplyWeight();
                    break;

                case Phase.Holding:
                    if (Time.time >= _holdUntil) _phase = Phase.BlendingOut;
                    break;

                case Phase.BlendingOut:
                    _weight -= StepFor(_current.BlendOut);
                    if (_weight <= 0f)
                    {
                        _weight = 0f;
                        ApplyWeight();
                        ReturnToAnimator();
                        return;
                    }
                    ApplyWeight();
                    break;
            }
        }

        private float StepFor(float _seconds)
            => _seconds <= 0f ? 1f : Time.deltaTime / _seconds;

        private void ApplyWeight()
        {
            if (!_mixer.IsValid()) return;
            _mixer.SetInputWeight(0, 1f - _weight);
            _mixer.SetInputWeight(1, _weight);
        }

        /// <summary>Stops driving the output so the Animator Controller resumes.</summary>
        private void ReturnToAnimator()
        {
            _phase = Phase.Idle;
            _current = null;
            if (_graph.IsValid()) _graph.Stop();
        }

        private void BuildGraph()
        {
            _graph = PlayableGraph.Create(name + " Animation Runner");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            _mixer = AnimationMixerPlayable.Create(_graph, 2);

            _output = AnimationPlayableOutput.Create(_graph, "Graves Animation", _animator);
            _output.SetSourcePlayable(_mixer);

            _ready = true;
        }

        /// <summary>
        /// Rebuilds whatever the Animator is currently playing as this graph's input 0.
        /// </summary>
        /// <remarks>
        /// Matching <c>normalizedTime</c> as well as speed matters: a walk cycle picked up at the
        /// wrong point in its stride cross-fades through a pose the character never actually holds.
        ///
        /// Returns false when there is nothing to sample - no controller at all, or the Animator is
        /// mid-transition, where <c>GetCurrentAnimatorClipInfo</c> is empty. The source technique
        /// waited out the transition in a coroutine; the caller here treats it as "no pose to leave"
        /// instead, which keeps <see cref="Play"/> synchronous and predictable.
        /// </remarks>
        private bool CaptureAnimatorClip()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null) return false;

            var info = _animator.GetCurrentAnimatorClipInfo(0);
            if (info == null || info.Length == 0 || info[0].clip == null) return false;

            var state = _animator.GetCurrentAnimatorStateInfo(0);
            var clip = info[0].clip;

            _capturedSpeed = state.speed;
            _capturedTime = Mathf.Repeat(state.normalizedTime, 1f) * clip.length;

            _fromPlayable = AnimationClipPlayable.Create(_graph, clip);
            _fromPlayable.SetSpeed(_capturedSpeed);
            _fromPlayable.SetTime(_capturedTime);

            _graph.Connect(_fromPlayable, 0, _mixer, 0);
            return true;
        }

        /// <summary>
        /// Builds input 1: the requested clip, wrapped in a layer mixer when a mask is authored.
        /// </summary>
        /// <remarks>
        /// The masked form needs the Animator's own clip on layer 0 underneath, so the unmasked bones
        /// keep doing what Mecanim was doing while the masked ones play the requested clip - the
        /// upper-body attack over a running lower body.
        /// </remarks>
        private void BuildTargetPlayable(NamedClip _entry)
        {
            _toPlayable = AnimationClipPlayable.Create(_graph, _entry.Clip);
            _toPlayable.SetSpeed(_entry.Speed);
            _toPlayable.SetApplyFootIK(true);

            if (_entry.Mask == null)
            {
                _graph.Connect(_toPlayable, 0, _mixer, 1);
                return;
            }

            _maskMixer = AnimationLayerMixerPlayable.Create(_graph, 2);

            if (_fromPlayable.IsValid() && _fromPlayable.GetAnimationClip() != null)
            {
                // Same clip AND the same point in it. A graph input can only feed one destination, so
                // the base layer has to be a second playable of that clip - and creating it at time
                // zero restarts the cycle underneath the mask. On a walk, that reads as the legs
                // snapping back and taking the same step twice: invisible if the mask happens to be
                // triggered near the start of the cycle, obvious anywhere else.
                _maskBase = AnimationClipPlayable.Create(_graph, _fromPlayable.GetAnimationClip());
                _maskBase.SetSpeed(_capturedSpeed);
                _maskBase.SetTime(_capturedTime);
                _maskMixer.ConnectInput(0, _maskBase, 0, 1f);
            }

            _maskMixer.ConnectInput(1, _toPlayable, 0, 1f);
            _maskMixer.SetLayerMaskFromAvatarMask(1, _entry.Mask);
            _maskMixer.SetLayerAdditive(1, false);

            _graph.Connect(_maskMixer, 0, _mixer, 1);
        }

        private void ReleaseClipPlayables()
        {
            if (!_graph.IsValid()) return;

            _mixer.DisconnectInput(0);
            _mixer.DisconnectInput(1);

            if (_maskMixer.IsValid()) _graph.DestroyPlayable(_maskMixer);
            if (_maskBase.IsValid()) _graph.DestroyPlayable(_maskBase);
            if (_fromPlayable.IsValid()) _graph.DestroyPlayable(_fromPlayable);
            if (_toPlayable.IsValid()) _graph.DestroyPlayable(_toPlayable);
        }

        private void OnDestroy()
        {
            if (_graph.IsValid()) _graph.Destroy();
            _ready = false;
        }
    }
}
