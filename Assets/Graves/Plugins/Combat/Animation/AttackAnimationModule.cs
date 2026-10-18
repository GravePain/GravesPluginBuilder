using Graves.Core;
using Graves.Plugins.Animation;
using UnityEngine;

namespace Graves.Plugins.Combat.Animation
{
    /// <summary>
    /// Says which authored clip an attack should play, and whether a new swing may cut it short.
    /// </summary>
    /// <remarks>
    /// <b>Cancelling is a play style, not a defect.</b> Graves' point, 2026-09-05, and it changed
    /// where this decision lives. A game whose attacks can be cancelled into one another feels
    /// fast and demanding; a game whose swings commit feels heavy and readable. Both are real, and
    /// the framework has no business choosing.
    ///
    /// So the runner still restarts whenever it is asked to play - that is playback doing as it is
    /// told - and the decision about whether to ask sits here, on a module a game authors.
    ///
    /// <b>The lever that matters most is not this flag.</b> It is the relationship between the
    /// attack cooldown and the clip's length. A cooldown shorter than the clip means every swing is
    /// cut short whatever this says; a cooldown longer than it means the character stands finished
    /// and waiting. Set those two against each other first.
    /// </remarks>
    [CreateAssetMenu(fileName = "AttackAnimationModule", menuName = "Graves/Modules/Attack Animation")]
    public class AttackAnimationModule : DefinitionModule
    {
        [SerializeField]
        [Graves.Plugins.Animation.ClipName]
        [Tooltip("The clip this swing plays. Chosen from the names authored on animation modules " +
                 "rather than typed, because a typo here is a swing that animates nothing and says " +
                 "so only once, in a warning nobody is watching for.")]
        private string _clipName = "Attack";

        [SerializeField]
        [Tooltip("Let a new swing cut the current one short and start again. Off, a swing already " +
                 "playing is left to finish and the new one is silent. This is a feel decision, " +
                 "not a correctness one - see the remarks.")]
        private bool _interruptsItself;

        /// <inheritdoc />
        /// <remarks>
        /// Both halves are reported by name when missing rather than repaired. An attack that
        /// silently plays nothing is the placeholder problem in miniature: it works well enough that
        /// nobody investigates until it ships.
        /// </remarks>
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var host = _entity.gameObject;

            var attacker = host.GetComponent<Attacker>();
            if (attacker == null)
            {
                Debug.LogError("[AttackAnimation] '" + _entity.name + "' has an attack animation " +
                               "but no combat module, so there is no swing to show.", _entity);
                return;
            }

            var runner = host.GetComponent<AnimationRunner>();
            if (runner == null)
            {
                Debug.LogError("[AttackAnimation] '" + _entity.name + "' has an attack animation " +
                               "but no animation module, so there is nothing to play it with.",
                               _entity);
                return;
            }

            var animator = host.GetComponent<AttackAnimator>();
            if (animator == null) animator = host.AddComponent<AttackAnimator>();

            animator.Initialise(attacker, runner, _clipName, _interruptsItself);
        }
    }
}
