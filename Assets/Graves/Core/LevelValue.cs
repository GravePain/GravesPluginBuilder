using System;
using UnityEngine;

namespace Graves.Core
{
    /// <summary>
    /// A number that grows with level, authored as two fields instead of a table.
    /// </summary>
    /// <remarks>
    /// <b>Extracted from the reference rather than invented.</b> The reference framework expresses health, damage,
    /// defence, cooldowns, cast range and level requirements with a handful of these structs, and
    /// they replace per-level arrays, lookup functions and stat tables entirely. Ninety-two lines
    /// carry the whole idea. See the second agent's extraction note.
    ///
    /// <b>Level is one-based and the formula subtracts one</b>, so level one always yields exactly
    /// <see cref="Base"/> with no bonus. That is deliberate in the original and it is the detail
    /// most likely to be lost and then rediscovered as an off-by-one in every stat at once.
    ///
    /// <b>This was written before a progression system existed, on the argument that the shape had
    /// to be right early or every stat in every plugin would need editing on the day levels arrived.
    /// The bet paid.</b> Progression exists now, adds a level source, and not one stat declaration
    /// changed - what changed was the value passed in. Recorded because the reverse bet, declaring
    /// stats as plain floats and widening them later, is the one that looks cheaper at the start.
    /// </remarks>
    [Serializable]
    public struct LevelValue
    {
        [Tooltip("The value at level one.")]
        public float Base;

        [Tooltip("Added for each level beyond the first. Zero means this value never grows.")]
        public float PerLevel;

        /// <summary>The value at a given level. Levels are one-based.</summary>
        public float At(int _level)
        {
            int level = _level < 1 ? 1 : _level;
            return PerLevel * (level - 1) + Base;
        }

        /// <summary>The value at a given level, rounded to a whole number.</summary>
        public int IntAt(int _level) => Mathf.RoundToInt(At(_level));

        /// <summary>A value that does not grow.</summary>
        public static LevelValue Flat(float _value) => new LevelValue { Base = _value, PerLevel = 0f };
    }
}
