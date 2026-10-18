using System;
using Graves.Core;

namespace Graves.Plugins.Progression
{
    /// <summary>
    /// How far along this particular thing is: what level it reached and how much experience it has
    /// banked toward the next one.
    /// </summary>
    /// <remarks>
    /// <b>This is the storage, not a copy of it.</b> The level component reads and writes through
    /// this object rather than keeping its own numbers, which is what makes a load work with nothing
    /// being notified. That rule was learned the hard way one plugin earlier - see
    /// <c>Agent Memory/decisions/F-002-state-is-the-storage.md</c> - and applied here from the first
    /// line rather than after a load silently did nothing.
    ///
    /// <b>Experience is banked toward the next level, not accumulated forever.</b> A running total
    /// since the beginning of time is the other common shape, and it forces every level threshold to
    /// be recomputed from the start whenever the curve is retuned. Storing progress toward the next
    /// step means a rebalance changes what the next level costs and never invalidates a level
    /// already earned - which matters, because the player already saw that level happen.
    /// </remarks>
    [Serializable]
    public class LevelState : InstanceState
    {
        /// <summary>The level reached. One-based; zero means never initialised.</summary>
        public int Level;

        /// <summary>Experience banked toward the next level.</summary>
        public int Experience;

        /// <summary>
        /// A stable name for this state in a save file, written as a literal so that renaming this
        /// class or moving it between assemblies cannot orphan an existing save.
        /// </summary>
        public override string StateKey => "graves.progression.level";
    }
}
