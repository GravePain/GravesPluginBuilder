using System;
using Graves.Plugins.Vitals;
using UnityEngine;
using Graves.Core;

namespace Graves.Plugins.Combat
{
    /// <summary>
    /// Anything that adds to the damage a thing deals: a weapon, a stat, a temporary rage.
    /// </summary>
    /// <remarks>
    /// The same pull-based mechanism health uses for its maximum. An attacker sums whatever it finds
    /// on its own object and never learns that equipment exists, so a weapon plugin can add damage
    /// without this file changing.
    /// </remarks>
    public interface ICombatBonus
    {
        /// <summary>Extra damage, given the base value before any bonuses.</summary>
        int GetDamageBonus(int _baseDamage);
    }

    /// <summary>
    /// Decides whether something found inside a swing is a legitimate victim.
    /// </summary>
    /// <remarks>
    /// <b>This exists because two monsters killed each other on the way to the player.</b> A swing
    /// finds everything with health inside its arc, which is correct for a sword and disastrous for
    /// a pack converging on the same spot - they arrived together, caught each other in the sweep,
    /// and were dead before they landed a blow on anyone who mattered.
    ///
    /// <b>It is a filter rather than a faction system, deliberately.</b> Teams, hostility tables and
    /// reputation are a real feature and a large one, and none of it is needed to stop this. A
    /// filter answers one question about one victim, so a faction system can be built later as an
    /// implementation of this interface without the attacker changing at all.
    ///
    /// <b>No filter means hit everything.</b> That keeps the player's sword indiscriminate, which is
    /// the honest default: a person swinging in a crowd should expect to hit what is in front of
    /// them.
    /// </remarks>
    public interface IDamageFilter
    {
        /// <summary>True when this victim may be hurt by the swing.</summary>
        bool MayDamage(Health _victim);
    }

    /// <summary>
    /// Swings at whatever is in front, and hurts it.
    /// </summary>
    /// <remarks>
    /// <b>This is the component that makes the player the subject of the loop rather than its
    /// object.</b> Vitals made a thing able to lose; this makes a thing able to take.
    ///
    /// <b>It knows nothing about input and nothing about animation.</b> Something else decides when
    /// to call <see cref="TryAttack"/>, and something else may listen for the swing and play a clip.
    /// Both of those live in their own assemblies, so combat works with neither installed - it just
    /// happens invisibly and in silence, which is the correct headless behaviour and is also exactly
    /// what a test wants.
    ///
    /// <b>Targets are found by overlap rather than by a raycast.</b> A ray through a single point
    /// misses a wide swing at a target standing slightly off-centre, and a melee attack that misses
    /// because the aim was two degrees out reads as broken rather than as demanding.
    ///
    /// <b>The layers it sweeps are authored, not assumed.</b> This used to test everything, written
    /// as <c>~0</c>, which is correct in a project with no special layers and a landmine in any
    /// project that grows one. Graves' own shipped game hit exactly this: a layer was added for
    /// ability hitboxes, and every <c>~0</c> mask in the project silently began treating projectiles
    /// as things a sword could hit. His note on it is blunt - a new mask written as <c>~0</c>
    /// re-opens the bug.
    ///
    /// So the mask is a field on the module. It still defaults to everything, because this framework
    /// ships no layers and inventing some would be deciding a game's physics for it. The difference
    /// is that the default is now visible in the inspector and can be narrowed the moment a game has
    /// something it should not be hitting.
    /// </remarks>
    [DisallowMultipleComponent]
    public class Attacker : MonoBehaviour, ILevelConsumer
    {
        private LevelValue _baseDamage = LevelValue.Flat(10f);
        private int _level = 1;
        private float _range = 2f;
        private float _halfAngle = 60f;
        private float _cooldown = 0.6f;
        private bool _hitEverythingInArc;
        private int _hitMask = ~0;

        private float _readyAt;
        private ICombatBonus[] _bonuses;
        private IDamageFilter[] _filters;
        private Health _own;
        private ILevelSource _levelSource;
        private bool _lookedForLevel;

        private readonly Collider[] _overlap = new Collider[32];

        /// <summary>Raised the moment a swing starts, before anything is hurt.</summary>
        /// <remarks>
        /// Separate from the hit so a clip or a sound can begin with the swing rather than with the
        /// impact. An attack that animates only when it connects looks like it never missed.
        /// </remarks>
        public event Action Swung;

        /// <summary>Raised for each thing hurt by a swing. Carries the victim and the damage dealt.</summary>
        public event Action<Health, int> Hit;

        /// <summary>Damage per hit at the current level, including every bonus present.</summary>
        public int Damage
        {
            get
            {
                int baseline = _baseDamage.IntAt(CurrentLevel);
                if (baseline < 0) baseline = 0;

                int bonus = 0;
                var sources = Bonuses();
                for (int i = 0; i < sources.Length; i++)
                {
                    if (sources[i] != null) bonus += sources[i].GetDamageBonus(baseline);
                }

                int total = baseline + bonus;
                return total < 0 ? 0 : total;
            }
        }

        /// <summary>True when the cooldown has elapsed and another swing is allowed.</summary>
        public bool IsReady => Time.time >= _readyAt;

        /// <summary>Seconds until the next swing is allowed. Zero when ready.</summary>
        public float CooldownRemaining => Mathf.Max(0f, _readyAt - Time.time);

        /// <summary>Applies the authored settings. Called by the module when the entity wakes.</summary>
        public void Configure(LevelValue _damage, float _reach, float _arcDegrees, float _secondsBetween,
                              bool _hitAll, int _layers)
        {
            _hitMask = _layers;
            _baseDamage = _damage;
            _range = Mathf.Max(0f, _reach);
            _halfAngle = Mathf.Clamp(_arcDegrees, 0f, 360f) * 0.5f;
            _cooldown = Mathf.Max(0f, _secondsBetween);
            _hitEverythingInArc = _hitAll;
            _bonuses = null;
        }

        /// <summary>The level damage is evaluated at: a level source on this object, or one.</summary>
        public int CurrentLevel
        {
            get
            {
                if (!_lookedForLevel)
                {
                    _lookedForLevel = true;
                    _levelSource = GetComponent<ILevelSource>();
                }

                if (_levelSource == null) return _level;
                int reported = _levelSource.Level;
                return reported < 1 ? 1 : reported;
            }
        }

        /// <summary>Sets the level directly. Ignored when a level source is present.</summary>
        public void SetLevel(int _newLevel) => _level = _newLevel < 1 ? 1 : _newLevel;

        /// <summary>
        /// Swings if the cooldown allows. Returns how many things were hurt, which is zero for a
        /// miss and also zero when the swing did not happen at all.
        /// </summary>
        /// <remarks>
        /// <b>The cooldown starts on the swing, not on the hit.</b> Otherwise missing costs nothing
        /// and the correct tactic is to flail constantly.
        ///
        /// <b>The return value deliberately does not distinguish a miss from a refusal.</b> Anything
        /// that needs to know asks <see cref="IsReady"/> first. Encoding both into one integer
        /// produces the kind of caller that treats minus one as a number.
        /// </remarks>
        public int TryAttack()
        {
            if (!IsReady) return 0;

            // The dead do not swing. Checked here rather than in each thing that asks, because
            // otherwise every driver - a person's mouse, a monster's brain, a trap - has to remember
            // it, and the one that forgets produces a corpse that keeps fighting. An entity with no
            // vitals at all can always attack, which is correct for a turret.
            if (_own == null) _own = GetComponent<Health>();
            if (_own != null && _own.IsDead) return 0;

            _readyAt = Time.time + _cooldown;
            Swung?.Invoke();

            return Strike(Damage, _range, _halfAngle * 2f, _hitEverythingInArc);
        }

        /// <summary>
        /// Performs one hit sweep with the numbers given, ignoring the configured ones and ignoring
        /// the cooldown. Returns how many things were hurt.
        /// </summary>
        /// <remarks>
        /// <b>Public so that a skill can swing without inventing its own hit detection.</b> There is
        /// one owner of "what does a sweep in front of me hit", and it is this method. A skill plugin
        /// that duplicated the overlap, the self check, the dead check and the damage filters would
        /// be a second owner of that fact, and the two would drift the first time either was fixed.
        ///
        /// It deliberately does not touch the cooldown or raise <see cref="Swung"/>. A skill has its
        /// own cooldown and its own reason to announce itself, and firing the basic attack's events
        /// would make every skill look like a basic attack to anything listening.
        /// </remarks>
        public int Strike(int _damage, float _reach, float _arcDegrees, bool _hitAll)
        {
            // Centred ahead of the attacker rather than on it, so the sphere covers the space being
            // swung into instead of the space already occupied.
            var origin = transform.position + Vector3.up * 1f;
            float reach = Mathf.Max(0f, _reach);
            var centre = origin + transform.forward * (reach * 0.5f);
            float radius = reach;
            float halfAngle = Mathf.Clamp(_arcDegrees, 0f, 360f) * 0.5f;

            int found = Physics.OverlapSphereNonAlloc(centre, radius, _overlap, _hitMask,
                                                      QueryTriggerInteraction.Ignore);

            // A fixed buffer, so a crowded room truncates rather than allocating every swing. Worth
            // saying out loud because silent truncation in a combat system is a bug that presents as
            // "sometimes the last enemy takes no damage".
            if (found >= _overlap.Length)
            {
                Debug.LogWarning("[Combat] '" + name + "' found more colliders than its buffer holds, " +
                                 "so some were not considered. Raise the buffer if this is common.",
                                 this);
            }

            int hits = 0;

            for (int i = 0; i < found; i++)
            {
                var collider = _overlap[i];
                if (collider == null) continue;

                var health = collider.GetComponentInParent<Health>();
                if (health == null) continue;
                if (health.gameObject == gameObject) continue;
                if (health.IsDead) continue;
                if (!MayDamage(health)) continue;

                var toTarget = health.transform.position - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.000001f)
                {
                    float angle = Vector3.Angle(transform.forward, toTarget.normalized);
                    if (angle > halfAngle) continue;
                }

                int dealt = health.TakeDamage(_damage, gameObject);
                if (dealt <= 0) continue;

                hits++;
                Hit?.Invoke(health, dealt);

                if (!_hitAll) break;
            }

            return hits;
        }

        private ICombatBonus[] Bonuses()
        {
            if (_bonuses == null) _bonuses = GetComponents<ICombatBonus>();
            return _bonuses;
        }

        /// <summary>
        /// Asks every filter present whether this victim is fair game. All must agree.
        /// </summary>
        /// <remarks>
        /// Unanimous rather than majority, because each filter states a reason something must not be
        /// hit, and a veto is the only sensible way to combine prohibitions. With no filters
        /// installed the loop does not run and everything is a valid victim.
        /// </remarks>
        private bool MayDamage(Health _victim)
        {
            if (_filters == null) _filters = GetComponents<IDamageFilter>();

            for (int i = 0; i < _filters.Length; i++)
            {
                if (_filters[i] != null && !_filters[i].MayDamage(_victim)) return false;
            }

            return true;
        }

        /// <summary>
        /// Forgets every cached lookup, so the next swing finds whatever is on the object now.
        /// </summary>
        /// <remarks>
        /// Call after adding a component this one reads: a damage bonus, a damage filter, or a level
        /// source. Modules attach in author order, and one that arrives after this component has
        /// already been read would otherwise never be seen.
        /// </remarks>
        public void RefreshSources()
        {
            _bonuses = null;
            _filters = null;
            RefreshLevelSource();
        }

        /// <inheritdoc />
        public void RefreshLevelSource()
        {
            _levelSource = null;
            _lookedForLevel = false;
        }

        /// <summary>
        /// Draws the reach and the edges of the arc when selected, so it can be judged rather than
        /// guessed at.
        /// </summary>
        /// <remarks>
        /// Drawn with <c>Gizmos</c> rather than editor handles on purpose. Handles live in
        /// UnityEditor, and naming that type puts an editor reference into a runtime assembly - the
        /// same mistake the core was cleaned of, where one line mentioning a type in order to remove
        /// it made the dependency report read wrongly. Gizmos are part of the engine and cost
        /// nothing in a build.
        /// </remarks>
        private void OnDrawGizmosSelected()
        {
            var origin = transform.position + Vector3.up * 1f;

            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.9f);
            Gizmos.DrawRay(origin, Quaternion.Euler(0f, -_halfAngle, 0f) * transform.forward * _range);
            Gizmos.DrawRay(origin, Quaternion.Euler(0f, _halfAngle, 0f) * transform.forward * _range);

            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.25f);
            Gizmos.DrawWireSphere(origin + transform.forward * (_range * 0.5f), _range);
        }
    }
}
