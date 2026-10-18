using System;
using Graves.Core;

namespace Graves.Plugins.Vitals
{
    /// <summary>
    /// How much health this particular thing has left. Survives a save.
    /// </summary>
    /// <remarks>
    /// <b>Current health belongs to the instance, not the definition.</b> Every goblin of a kind
    /// shares a maximum and each one has taken its own beating, which is the exact distinction the
    /// core's definition and instance split exists to carry.
    ///
    /// <b>Only the current value is stored.</b> Maximum is derived from the definition, the level
    /// and whatever bonuses are present, so writing it into a save would freeze a number that should
    /// move when the game is rebalanced. A player who returns after a patch that raised goblin
    /// health should meet the new goblins, and the previous project's lesson is that the safest
    /// thing to persist is the smallest thing that cannot be recomputed.
    /// </remarks>
    [Serializable]
    public class HealthState : InstanceState
    {
        /// <summary>Health remaining. Meaningless without the definition that says what the maximum is.</summary>
        public int Current;

        /// <summary>
        /// False until something has actually set health, so a brand new instance can be told apart
        /// from one that was saved on nought.
        /// </summary>
        /// <remarks>
        /// Without this, a fresh instance and a corpse are both zero, and restoring a save would
        /// either revive the dead or kill the newborn depending on which way the ambiguity was
        /// guessed. A flag is cheaper than either mistake.
        /// </remarks>
        public bool Initialised;

        /// <summary>
        /// A stable name for this state in a save file.
        /// </summary>
        /// <remarks>
        /// Written out as a literal rather than left to default to the type name, so renaming this
        /// class or moving it between assemblies cannot orphan a save that already exists.
        /// </remarks>
        public override string StateKey => "graves.vitals.health";
    }
}
