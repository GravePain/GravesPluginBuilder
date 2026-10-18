using System;
using UnityEngine;
using Graves.Core;

namespace Graves.Plugins.Progression
{
    /// <summary>
    /// What level a thing is, and how close it is to the next one.
    /// </summary>
    /// <remarks>
    /// <b>Everything level-scaled in the game reads this without knowing it exists.</b> Health and
    /// damage ask for a level source on their own object; this is one. Nothing pushes a level into
    /// them, so nothing can forget to.
    ///
    /// <b>It is seventeen lines of concept and no more.</b> The reference's level component holds a
    /// current value and a maximum and nothing else, which was a surprise worth taking seriously: in
    /// a commercial RPG, level is not a system. The curve, the rewards and the consequences all live
    /// elsewhere and merely consult it.
    /// </remarks>
    [DisallowMultipleComponent]
    public class Level : MonoBehaviour, ILevelSource
    {
        private LevelState _state;
        private int _looseLevel = 1;
        private int _looseExperience;

        private LevelValue _cost = new LevelValue { Base = 100f, PerLevel = 50f };
        private int _maxLevel = 20;

        /// <summary>Raised on each level gained, carrying the new level.</summary>
        /// <remarks>
        /// Once per level, not once per grant. A single large reward that crosses three thresholds
        /// raises this three times, because anything reacting to a level - a stat recalculation, a
        /// sound, an unlocked ability - cares about each step rather than about the arithmetic.
        /// </remarks>
        public event Action<int> LevelledUp;

        /// <summary>Raised when experience is gained, carrying the amount.</summary>
        public event Action<int> ExperienceGained;

        /// <summary>The level reached. Never below one, never above the maximum.</summary>
        public int CurrentLevel
        {
            get
            {
                int stored = _state != null ? _state.Level : _looseLevel;
                if (stored < 1) stored = 1;
                return stored > _maxLevel ? _maxLevel : stored;
            }
            private set
            {
                int clamped = Mathf.Clamp(value, 1, _maxLevel);
                if (_state != null) _state.Level = clamped; else _looseLevel = clamped;
            }
        }

        /// <inheritdoc />
        int ILevelSource.Level => CurrentLevel;

        /// <summary>Experience banked toward the next level.</summary>
        public int Experience
        {
            get => _state != null ? _state.Experience : _looseExperience;
            private set
            {
                int clamped = value < 0 ? 0 : value;
                if (_state != null) _state.Experience = clamped; else _looseExperience = clamped;
            }
        }

        /// <summary>The highest level this thing can reach.</summary>
        public int MaxLevel => _maxLevel;

        /// <summary>True once the maximum level has been reached.</summary>
        public bool IsMaxLevel => CurrentLevel >= _maxLevel;

        /// <summary>Experience needed to reach the next level, or zero at maximum.</summary>
        public int ExperienceToNext => IsMaxLevel ? 0 : Mathf.Max(1, _cost.IntAt(CurrentLevel));

        /// <summary>Progress toward the next level, from zero to one. One at maximum level.</summary>
        public float Fraction
        {
            get
            {
                if (IsMaxLevel) return 1f;
                int needed = ExperienceToNext;
                return needed <= 0 ? 1f : Mathf.Clamp01((float)Experience / needed);
            }
        }

        /// <summary>Applies the authored settings.</summary>
        public void Configure(LevelValue _costPerLevel, int _highestLevel, int _startingLevel)
        {
            _cost = _costPerLevel;
            _maxLevel = Mathf.Max(1, _highestLevel);

            if (_state == null && _looseLevel < 1) _looseLevel = Mathf.Max(1, _startingLevel);
        }

        /// <summary>
        /// Hands this component its storage, so a save reads and writes the same object it does.
        /// </summary>
        /// <remarks>
        /// An uninitialised state adopts the starting level rather than zero. A level of zero is not
        /// a valid level, and treating a blank save as "level nought" produces an entity whose every
        /// stat evaluates below its base.
        /// </remarks>
        public void Bind(LevelState _storage, int _startingLevel)
        {
            if (_storage == null)
            {
                _state = null;
                return;
            }

            _state = _storage;

            if (_state.Level < 1)
            {
                _state.Level = Mathf.Clamp(Mathf.Max(1, _startingLevel), 1, _maxLevel);
                _state.Experience = 0;
            }
        }

        /// <summary>
        /// Adds experience and returns how many levels were gained, which is usually zero.
        /// </summary>
        /// <remarks>
        /// Loops rather than granting one level per call, so a single large reward can cross several
        /// thresholds at once. The loop is bounded by the maximum level, so a reward larger than the
        /// entire curve terminates rather than spinning.
        ///
        /// Leftover experience carries into the next level rather than being discarded. Discarding it
        /// is a quiet tax on killing something worth more than the remaining bar, and players notice.
        /// </remarks>
        public int Grant(int _amount)
        {
            if (_amount <= 0 || IsMaxLevel) return 0;

            Experience += _amount;
            ExperienceGained?.Invoke(_amount);

            int gained = 0;

            while (!IsMaxLevel)
            {
                int needed = ExperienceToNext;
                if (needed <= 0 || Experience < needed) break;

                Experience -= needed;
                CurrentLevel += 1;
                gained++;

                LevelledUp?.Invoke(CurrentLevel);
            }

            // At maximum, banked progress is meaningless and a bar drawn from it would sit part
            // filled forever. Cleared so the state says plainly that there is nothing left to earn.
            if (IsMaxLevel) Experience = 0;

            return gained;
        }
    }
}
