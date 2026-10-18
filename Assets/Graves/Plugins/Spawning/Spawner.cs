using System.Collections.Generic;
using Graves.Core;
using Graves.Plugins.AI;
using UnityEngine;

namespace Graves.Plugins.Spawning
{
    /// <summary>
    /// Keeps a patch of the world stocked with something, replacing what gets killed.
    /// </summary>
    /// <remarks>
    /// <b>A spawner is scene furniture, not an entity, and that is the one interesting decision
    /// here.</b> Everything else in this framework that matters is a definition with modules. A
    /// spawner is not: it has no identity worth saving, no body, and it is placed rather than
    /// authored. Making it an entity would have given it a permanent id it has no use for and a
    /// representation it does not want, because the core spawns a body for every entity and falls
    /// back to a capsule when none is given. An invisible marker would arrive with a visible capsule.
    ///
    /// That is worth recording rather than quietly working around. The rule that every entity has a
    /// body is right for anything a player interacts with and wrong for a marker, and the cheapest
    /// correct answer today is not to make markers into entities.
    ///
    /// <b>What it spawns are ordinary entities.</b> They get a definition, an instance, modules and
    /// everything else the framework provides. Only the thing doing the spawning sits outside.
    /// </remarks>
    [DisallowMultipleComponent]
    public class Spawner : MonoBehaviour
    {
        [Header("What")]
        [SerializeField]
        [Tooltip("The definition to spawn. Without one this does nothing, and says so once.")]
        private EntityDefinition _definition;

        [Header("How many")]
        [SerializeField]
        [Tooltip("How many should be alive at once.")]
        [Min(1)] private int _alive = 2;

        [SerializeField]
        [Tooltip("Seconds between one appearing and the next.")]
        [Min(0f)] private float _interval = 5f;

        [SerializeField]
        [Tooltip("Seconds before the first one appears.")]
        [Min(0f)] private float _startDelay;

        [Header("When")]
        [SerializeField]
        [Tooltip("Only spawn while something worth fighting is within this many metres. Zero means " +
                 "always spawn, which fills an empty world nobody is looking at.")]
        [Min(0f)] private float _activeRange = 30f;

        [Header("Where")]
        [SerializeField]
        [Tooltip("Metres from this point they may appear. Zero puts every one of them in exactly " +
                 "the same spot, stacked inside one another.")]
        [Min(0f)] private float _radius = 3f;

        private readonly List<GameEntity> _spawned = new List<GameEntity>();
        private float _nextAt;
        private bool _complained;

        /// <summary>How many of this spawner's occupants are currently alive.</summary>
        public int LivingCount
        {
            get
            {
                Prune();
                return _spawned.Count;
            }
        }

        private void Start() => _nextAt = Time.time + _startDelay;

        private void Update()
        {
            if (_definition == null)
            {
                if (_complained) return;
                _complained = true;
                Debug.LogError("[Spawner] " + name + " has no definition assigned, so it will never " +
                               "spawn anything.", this);
                return;
            }

            Prune();

            if (_spawned.Count >= _alive) return;

            // Asked before the timer, not after. Checking the clock first would let a spawner count
            // down while nobody is present and then empty its whole quota the instant someone walks
            // into range, which reads as an ambush that was not designed.
            if (!AnyoneToCare())
            {
                _nextAt = Time.time + _interval;
                return;
            }

            if (Time.time < _nextAt) return;

            Spawn();

            // Timed from the spawn rather than from the death, so a spawner that is several short
            // fills them one at a time instead of emptying its whole quota into a single frame.
            _nextAt = Time.time + _interval;
        }

        /// <summary>
        /// True when something worth fighting is close enough for this spawner to bother.
        /// </summary>
        /// <remarks>
        /// <b>Graves' requirement, and the one this spawner first went without.</b> A world that
        /// populates every corner nobody is standing in spends its frames on fights no one will see,
        /// and a player walking into a clearing should not find it already stocked from an hour ago.
        ///
        /// <b>It reuses the AI target marker rather than inventing a second one.</b> That marker
        /// already means "a thing the world reacts to", which is exactly the question being asked
        /// here. The cost is that this plugin now depends on the AI plugin, which is wrong in
        /// principle for a spawner that populates a world with deer.
        ///
        /// Recorded rather than pre-empted, the same way the level primitives were: when a second
        /// consumer wants the marker without wanting brains, the marker moves to its own assembly
        /// and both point at it. One consumer is not enough to justify that yet.
        /// </remarks>
        private bool AnyoneToCare()
        {
            if (_activeRange <= 0f) return true;

            float limit = _activeRange * _activeRange;
            var targets = AiTarget.All;

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null) continue;

                var offset = target.transform.position - transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude <= limit) return true;
            }

            return false;
        }

        /// <summary>
        /// Drops anything that has been destroyed or killed.
        /// </summary>
        /// <remarks>
        /// Both checks are needed, for different reasons. A body removed on death leaves a destroyed
        /// object, which reads as null. A body left to lie there does not, and a spawner counting
        /// corpses as occupants would stop replacing them, so the world would go quiet for no
        /// visible reason.
        /// </remarks>
        private void Prune()
        {
            for (int i = _spawned.Count - 1; i >= 0; i--)
            {
                var entity = _spawned[i];
                if (entity == null) { _spawned.RemoveAt(i); continue; }

                // Asked through the core's interface rather than of the vitals plugin. A
                // spawner wants to know whether its occupant is still standing; it has no business
                // knowing what health is, and requiring vitals to be installed to keep a clearing
                // stocked was a dependency nobody chose.
                var mortal = entity.GetComponent<IMortal>();
                if (mortal != null && mortal.IsDead) _spawned.RemoveAt(i);
            }
        }

        private void Spawn()
        {
            var offset = Random.insideUnitCircle * _radius;
            var where = transform.position + new Vector3(offset.x, 0f, offset.y);

            var entity = GameEntity.Create(_definition, where, transform.rotation);
            if (entity == null) return;

            _spawned.Add(entity);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, _radius);

            if (_activeRange <= 0f) return;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, _activeRange);
        }
    }
}
