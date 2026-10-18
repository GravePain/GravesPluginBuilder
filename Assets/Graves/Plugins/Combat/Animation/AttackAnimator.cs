using Graves.Core;
using Graves.Plugins.Animation;
using UnityEngine;

namespace Graves.Plugins.Combat.Animation
{
    /// <summary>
    /// Plays a clip when a swing starts, so an attack can be seen rather than only measured.
    /// </summary>
    /// <remarks>
    /// <b>This is the bridge half of animation, and it is the reason it lives in its own
    /// assembly.</b> The clip runner is playback: it takes the Animator's output for the length of
    /// one clip and hands it back. This decides that a swing is worth a clip and which one. Your
    /// categorisation document separates those two jobs, and keeping them apart means the playback
    /// mechanism could be replaced without combat noticing.
    ///
    /// <b>Combat does not depend on this.</b> With this assembly deleted, attacks still land and
    /// still hurt - silently and invisibly. That is the test of whether the split is real.
    /// </remarks>
    [DisallowMultipleComponent]
    public class AttackAnimator : MonoBehaviour
    {
        private Attacker _attacker;
        private AnimationRunner _runner;
        private string _clipName;
        private bool _interrupts;
        private bool _warned;

        /// <summary>Wires the swing to the clip that shows it.</summary>
        public void Initialise(Attacker _source, AnimationRunner _target, string _clip,
                               bool _mayInterruptItself)
        {
            Unsubscribe();

            _attacker = _source;
            _runner = _target;
            _clipName = _clip;
            _interrupts = _mayInterruptItself;

            if (_attacker != null) _attacker.Swung += OnSwung;
        }

        private void OnSwung()
        {
            if (_runner == null || string.IsNullOrWhiteSpace(_clipName)) return;

            // Whether a swing may cut short the swing before it is authored, not assumed. Only the
            // SAME clip is protected: a skill's clip still takes over from a basic swing, because
            // refusing that would make a deliberate ability lose to a held mouse button.
            //
            // No guard against re-entry beyond this. The reference this runner came from carried a
            // timing guard that existed to cover churn the runner no longer has, and it made the one
            // case worth testing - swinging repeatedly mid-stride - unreachable.
            if (!_interrupts && _runner.IsPlaying &&
                string.Equals(_runner.Playing, _clipName, System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!_runner.Play(_clipName) && !_warned)
            {
                _warned = true;
                Debug.LogWarning("[AttackAnimation] '" + name + "' has no clip named '" + _clipName +
                                 "'. Attacks will still land, invisibly.", this);
            }
        }

        private void OnDestroy() => Unsubscribe();

        private void Unsubscribe()
        {
            if (_attacker == null) return;
            _attacker.Swung -= OnSwung;
        }
    }
}
