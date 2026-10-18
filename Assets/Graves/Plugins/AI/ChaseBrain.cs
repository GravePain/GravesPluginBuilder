using Graves.Plugins.Locomotion;
using UnityEngine;
using Graves.Core;

namespace Graves.Plugins.AI
{
    /// <summary>
    /// Watches for a target, walks at it, and stops when it is close enough.
    /// </summary>
    /// <remarks>
    /// <b>This class is the proof that the brain and motor split was worth building.</b> It produces
    /// exactly the same <see cref="MoveIntent"/> a person produces with a keyboard, and hands it to
    /// exactly the same motor. Neither the motor nor the intent knows a human is absent. Single
    /// player removed the networking argument for that split, leaving only this one, so if this
    /// class had needed anything the player brain did not, the split would have been wrong.
    ///
    /// <b>It is a straight line, on purpose.</b> No pathfinding, no obstacle avoidance, no
    /// wandering, no memory of where the target went. Those are real and they are also a navigation
    /// motor's job and a behaviour tree's job, and neither exists. What this does is enough to close
    /// the loop, and being visibly stupid is better than being complicated and subtly wrong.
    /// </remarks>
    [DisallowMultipleComponent]
    public class ChaseBrain : MonoBehaviour
    {
        private Motor _motor;
        private IMortal _mortal;

        private float _sightRange = 12f;
        private float _stopDistance = 1.6f;
        private float _giveUpRange = 20f;

        private AiTarget _quarry;

        /// <summary>What this brain is currently going after, or null when nothing is worth chasing.</summary>
        public AiTarget Quarry => _quarry;

        /// <summary>True while a target is being pursued.</summary>
        public bool IsChasing => _quarry != null;

        /// <summary>Applies the authored settings and wires the brain to its legs.</summary>
        public void Initialise(Motor _target, IMortal _own, float _sight, float _stop, float _giveUp)
        {
            _motor = _target;
            _mortal = _own;
            _sightRange = Mathf.Max(0f, _sight);
            _stopDistance = Mathf.Max(0f, _stop);

            // Never smaller than sight range. A brain that gives up closer than it can see is one
            // that spots a target, walks two steps, forgets, spots it again, and jitters on the spot
            // forever. Clamped rather than trusted, because that bug is very hard to read as a
            // tuning mistake.
            _giveUpRange = Mathf.Max(_giveUp, _sightRange);
        }

        private void Update()
        {
            if (_motor == null) return;

            // The dead do not give chase. Checked first so a corpse stops immediately rather than
            // finishing the step it was taking.
            if (_mortal != null && _mortal.IsDead)
            {
                _motor.SetIntent(MoveIntent.None);
                _quarry = null;
                return;
            }

            _quarry = ChooseQuarry();

            if (_quarry == null)
            {
                _motor.SetIntent(MoveIntent.None);
                return;
            }

            var toQuarry = _quarry.transform.position - transform.position;
            toQuarry.y = 0f;

            float distance = toQuarry.magnitude;
            var direction = distance > 0.0001f ? toQuarry / distance : Vector3.zero;

            var intent = MoveIntent.None;

            // Facing is held even at a standstill, so something that has closed the distance keeps
            // looking at what it is about to hit rather than staring past it.
            intent.Look = direction;

            if (distance > _stopDistance) intent.Move = direction;

            _motor.SetIntent(intent);
        }

        /// <summary>
        /// Picks the nearest worthwhile target, keeping the current one until it is genuinely out of
        /// reach.
        /// </summary>
        /// <remarks>
        /// Two ranges rather than one. Acquiring uses sight range and losing uses a larger give-up
        /// range, so a target hovering exactly at the edge does not get picked up and dropped on
        /// alternate frames. The gap between the two numbers is the whole anti-flicker mechanism.
        /// </remarks>
        private AiTarget ChooseQuarry()
        {
            var targets = AiTarget.All;

            float keepRange = _giveUpRange * _giveUpRange;
            float findRange = _sightRange * _sightRange;

            AiTarget best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < targets.Count; i++)
            {
                var candidate = targets[i];
                if (candidate == null) continue;
                if (candidate.gameObject == gameObject) continue;

                // A corpse is not worth chasing. Targets without vitals are fair game, since a thing
                // that cannot be hurt can still be something to follow.
                var theirs = candidate.GetComponent<IMortal>();
                if (theirs != null && theirs.IsDead) continue;

                var offset = candidate.transform.position - transform.position;
                offset.y = 0f;
                float distance = offset.sqrMagnitude;

                float limit = candidate == _quarry ? keepRange : findRange;
                if (distance > limit) continue;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }

        /// <summary>
        /// Forgets what it was chasing when switched off.
        /// </summary>
        /// <remarks>
        /// Found by watching the diagnostic output: a killed grunt reported it was still chasing,
        /// because death disables this component and a disabled component's <c>Update</c> never runs
        /// to clear the quarry. Harmless in itself and actively misleading to anyone reading state,
        /// which on this project is how most real bugs have been found.
        /// </remarks>
        private void OnDisable() => _quarry = null;

        /// <summary>Draws sight and stopping distances when selected, so they can be seen rather than imagined.</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, _sightRange);

            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, _stopDistance);
        }
    }
}
