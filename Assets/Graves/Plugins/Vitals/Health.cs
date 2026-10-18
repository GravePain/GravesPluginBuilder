using System;
using UnityEngine;
using Graves.Core;

namespace Graves.Plugins.Vitals
{
    /// <summary>
    /// Anything that can add to a maximum health total: equipment, a stat, a temporary blessing.
    /// </summary>
    /// <remarks>
    /// <b>This is the mechanism that keeps health ignorant of everything that modifies it.</b>
    /// Health sums whatever it finds on the same object and never learns that equipment exists.
    /// Adding a new source of bonuses requires no edit here and no edit to health.
    ///
    /// Taken from the reference, where it is the central extension mechanism and predates any
    /// messaging. It is worth being explicit about why it is not events: aggregation and
    /// notification are different problems. This answers "what is my maximum", which is a question
    /// with an answer right now, and pulling it is simpler and more correct than maintaining a
    /// running total that something must remember to update.
    /// </remarks>
    public interface IHealthBonus
    {
        /// <summary>Extra maximum health, given the base value before any bonuses.</summary>
        /// <remarks>
        /// The base is passed in so a bonus can be proportional - "ten per cent more health" is a
        /// thing games want and cannot express against an unknown total.
        /// </remarks>
        int GetHealthBonus(int _baseHealth);
    }

    /// <summary>
    /// How much damage a thing can take before it stops being alive.
    /// </summary>
    /// <remarks>
    /// <b>Maximum is computed on every read rather than cached.</b> No dirty flags, no invalidation,
    /// no recomputing when equipment changes - because there is nothing to recompute. The reference
    /// ran this way commercially for years, and cached stats with invalidation are a well known
    /// source of bugs where a total silently disagrees with the things that produced it.
    ///
    /// <b>Notification is a plain C# event, not a UnityEvent.</b> A UnityEvent is wired in the
    /// inspector, and inspector wiring is the exact defect this project took from the reference's
    /// movement swap: a serialised reference that has to be re-dragged whenever the thing it points
    /// at is replaced. Subscribing in code has no such problem and cannot be silently unwired.
    /// </remarks>
    [DisallowMultipleComponent]
    public class Health : MonoBehaviour, ILevelConsumer, IMortal
    {
        private LevelValue _baseMax = LevelValue.Flat(100f);
        private int _level = 1;
        private int _loose;
        private bool _started;

        private HealthState _state;
        private IHealthBonus[] _bonuses;
        private ILevelSource _levelSource;
        private bool _lookedForLevel;

        /// <summary>
        /// Where the number actually lives: the bound state when there is one, a private field
        /// otherwise.
        /// </summary>
        /// <remarks>
        /// <b>This indirection exists because of a bug that reported success.</b> An earlier version
        /// kept its own integer and copied it into the state whenever health changed. Saving worked
        /// perfectly. Loading also reported perfect success - six entities restored, nothing missing,
        /// no problems - and changed nothing, because the save system overwrites the state object in
        /// place and the component was reading a copy it had made at startup.
        ///
        /// The save plugin was right and this was wrong. Its own comment says it overwrites in place
        /// so that a plugin holding its state does not end up writing to an orphan, which is a plain
        /// statement that <b>the state is the storage</b>. The inventory plugin already worked that
        /// way. This did not, and the mismatch was invisible from either side.
        ///
        /// With the state as the storage, a load needs no notification, no callback and no polling.
        /// The bytes land in the object the component is already reading.
        /// </remarks>
        private int Stored
        {
            get => _state != null ? _state.Current : _loose;
            set
            {
                if (_state != null)
                {
                    _state.Current = value;
                    _state.Initialised = true;
                }
                else
                {
                    _loose = value;
                }
            }
        }

        /// <summary>Raised when damage is taken. Carries the amount actually lost and the dealer, which may be null.</summary>
        public event Action<int, GameObject> Damaged;

        /// <summary>Raised when healed. Carries the amount actually restored.</summary>
        public event Action<int> Healed;

        /// <summary>Raised once, the moment health reaches zero. Carries the killer, which may be null.</summary>
        public event Action<GameObject> Died;

        /// <summary>Current health. Never below zero, never above <see cref="Max"/>.</summary>
        public int Current => Mathf.Clamp(Stored, 0, Max);

        /// <summary>Maximum health at the current level, including every bonus present.</summary>
        public int Max
        {
            get
            {
                int baseline = _baseMax.IntAt(CurrentLevel);
                if (baseline < 1) baseline = 1;

                // Summed by hand rather than with Linq. The reference measured its equivalent at
                // 190 KB of garbage per call and hand-rolled every one of them for that reason, and
                // this is read far more often than it is written.
                int bonus = 0;
                var sources = Bonuses();
                for (int i = 0; i < sources.Length; i++)
                {
                    if (sources[i] != null) bonus += sources[i].GetHealthBonus(baseline);
                }

                int total = baseline + bonus;
                return total < 1 ? 1 : total;
            }
        }

        /// <summary>True once health has reached zero.</summary>
        public bool IsDead => Current <= 0;

        /// <summary>
        /// True when the state this health was bound to already held a value - it came from a save
        /// rather than from a spawn.
        /// </summary>
        /// <remarks>
        /// <b>This exists because the question cannot be asked afterwards.</b> Binding an
        /// uninitialised state writes through <c>Stored</c>, which marks it initialised, so by the
        /// time any other module looks, every state says it has been initialised and a fresh spawn is
        /// indistinguishable from a loaded one. <c>Bind</c> is the only moment the difference is
        /// visible, so it is recorded there.
        ///
        /// The distinction matters to anything that wants to top an entity up on arrival: a genuinely
        /// wounded thing must stay wounded across a load, and a fresh one must not spawn hurt.
        /// </remarks>
        public bool RestoredFromSave { get; private set; }

        /// <summary>Current health as a fraction of maximum, for a bar to draw.</summary>
        public float Fraction
        {
            get
            {
                int max = Max;
                return max <= 0 ? 0f : (float)Current / max;
            }
        }

        /// <summary>
        /// Applies the authored settings and fills health. Called by the module when the entity wakes.
        /// </summary>
        public void Configure(LevelValue _maximum, bool _startFull)
        {
            _baseMax = _maximum;
            _bonuses = null;

            if (!_started || _startFull) Stored = Max;
            _started = true;
        }

        /// <summary>
        /// Hands health its storage, so that what it holds is the same object a save reads and
        /// writes.
        /// </summary>
        /// <remarks>
        /// <b>Call this after <see cref="Configure"/>, not before.</b> Binding first would clamp a
        /// saved value against a maximum that has not been set yet, which is a maximum of one
        /// hundred by default and quietly wrong rather than obviously broken.
        ///
        /// An uninitialised state adopts whatever health currently holds, so a newly spawned thing
        /// starts full. An initialised one is left exactly as it is, because it came from a save and
        /// is the truth. Getting that the wrong way round means every load either revives the dead
        /// or kills the living, depending on which way the ambiguity is guessed.
        ///
        /// Passing null unbinds, and health falls back to its own field. That keeps this component
        /// usable with no framework, no instance and no save present at all.
        /// </remarks>
        public void Bind(HealthState _storage)
        {
            if (_storage == null)
            {
                _state = null;
                RestoredFromSave = false;
                return;
            }

            bool wasInitialised = _storage.Initialised;
            int carried = Stored;

            _state = _storage;
            RestoredFromSave = wasInitialised;

            if (!wasInitialised) Stored = Mathf.Clamp(carried, 0, Max);
        }

        /// <summary>
        /// The level this entity's maximum is evaluated at: whatever a level source on the same
        /// object reports, or one when there is none.
        /// </summary>
        /// <remarks>
        /// Looked up once and then held. The source is a component on this object and components do
        /// not come and go, so re-searching every read would cost something for nothing.
        /// </remarks>
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

        /// <summary>
        /// Sets the level directly, for an entity with no level source of its own.
        /// </summary>
        /// <remarks>
        /// Ignored when a level source is present, because two things deciding one number is how
        /// they come to disagree. Kept for tests and for a game that wants a fixed level without a
        /// progression system.
        /// </remarks>
        public void SetLevel(int _newLevel)
        {
            _level = _newLevel < 1 ? 1 : _newLevel;
            Stored = Mathf.Min(Stored, Max);
        }

        /// <summary>
        /// Takes damage. Returns how much was actually lost, which is less than asked for when there
        /// was not that much health left.
        /// </summary>
        /// <remarks>
        /// Death fires exactly once. A second killing blow on something already dead does nothing
        /// and says nothing, because two death events for one corpse is how a thing drops its loot
        /// twice.
        /// </remarks>
        public int TakeDamage(int _amount, GameObject _dealer = null)
        {
            if (_amount <= 0 || IsDead) return 0;

            int before = Current;
            Stored = Mathf.Max(0, before - _amount);
            int lost = before - Stored;
            if (lost <= 0) return 0;

            Damaged?.Invoke(lost, _dealer);

            if (Stored <= 0)
            {
                Died?.Invoke(_dealer);

                // Announced as well as raised. The local event is for anything that already depends
                // on this plugin; the bus is for everything that does not and should not have to -
                // including code written by somebody who cannot edit this framework at all.
                GameEvents.Publish(new EntityDied(gameObject, _dealer));
            }

            return lost;
        }

        /// <summary>Restores health. Returns how much was actually restored.</summary>
        /// <remarks>
        /// The dead are not healed. Reviving is a different act with different consequences - loot,
        /// aggro, quest state - and letting a heal quietly do it is how something comes back to life
        /// halfway through dying.
        /// </remarks>
        public int Heal(int _amount)
        {
            if (_amount <= 0 || IsDead) return 0;

            int before = Current;
            Stored = Mathf.Min(Max, before + _amount);
            int gained = Stored - before;
            if (gained > 0) Healed?.Invoke(gained);

            return gained;
        }

        /// <summary>Sets current health directly, without raising damage or death.</summary>
        /// <remarks>
        /// For a save restoring what was there. Deliberately silent: replaying a death event on load
        /// would drop loot again and play a death animation for something that died last session.
        /// </remarks>
        public void RestoreTo(int _value)
        {
            Stored = Mathf.Clamp(_value, 0, Max);
            _started = true;
        }

        private IHealthBonus[] Bonuses()
        {
            // Gathered on first use rather than in Awake, so a bonus source added by a module that
            // attaches after this one is still found. Module order is author order and should not
            // decide whether armour counts.
            if (_bonuses == null) _bonuses = GetComponents<IHealthBonus>();
            return _bonuses;
        }

        /// <summary>
        /// Forgets every cached lookup, so the next read finds whatever is on the object now.
        /// </summary>
        /// <remarks>
        /// <b>Call this after adding a component this one reads.</b> That means a bonus source, which
        /// equipment will add and remove at runtime, and a level source, which a progression module
        /// adds while the entity is waking.
        ///
        /// <b>The level case was a live bug and is worth stating.</b> Modules attach in author order,
        /// and vitals attaches before progression. Configuring health reads its maximum, which looks
        /// for a level source, finds none because progression has not run yet, and caches that
        /// answer. The entity then levels up and its maximum health never moves - while damage,
        /// whose component was cached later, scales correctly. Half the stats respond to levelling
        /// and half do not, which reads as a balance problem rather than a bug.
        ///
        /// Re-scanning on every read would avoid it and costs a component lookup in the hottest path
        /// in the plugin. An explicit call is the same shape the bonus cache already used.
        /// </remarks>
        public void RefreshSources()
        {
            _bonuses = null;
            RefreshLevelSource();
        }

        /// <summary>
        /// Forgets the level source, then fills a fresh entity up to whatever its maximum turned out
        /// to be.
        /// </summary>
        /// <remarks>
        /// <b>The top-up lives here rather than in the progression module, and that is the whole
        /// point.</b> Health fills itself while attaching, against a maximum evaluated at level one
        /// because no level source exists yet. When progression arrives and the real level becomes
        /// visible, that maximum may be higher and the entity is standing there wounded through no
        /// fault of its own.
        ///
        /// Progression cannot fix it from its own side without depending on module order: if it
        /// attaches first there is no health to top up, and if it attaches second the state has
        /// already been marked initialised. Doing it here removes the ordering question entirely -
        /// whoever adds a level source calls this, and health answers with what it alone knows.
        ///
        /// <b>Only a fresh entity is filled.</b> One restored from a save is left exactly as it was,
        /// because a wounded thing that heals itself on every load is a save that quietly does not
        /// work.
        /// </remarks>
        public void RefreshLevelSource()
        {
            _levelSource = null;
            _lookedForLevel = false;

            if (RestoredFromSave || IsDead) return;

            int room = Max - Current;
            if (room > 0) Heal(room);
        }
    }
}
