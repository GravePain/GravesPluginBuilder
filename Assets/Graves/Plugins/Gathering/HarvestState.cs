using System;
using Graves.Core;

namespace Graves.Plugins.Gathering
{
    /// <summary>
    /// How much is left in one particular node, and when it comes back.
    /// </summary>
    /// <remarks>
    /// <b>This is the storage, not a copy of it.</b> A harvestable reads and writes through this
    /// object rather than keeping its own counter, which is what makes a half-stripped bush still
    /// half-stripped after a save and a restart. A component holding its own number saves correctly
    /// and restores nothing.
    /// </remarks>
    [Serializable]
    public class HarvestState : InstanceState
    {
        /// <summary>The key this state is addressed by in a save file.</summary>
        public const string Key = "graves.gathering.harvest";

        /// <summary>Harvests left before it is stripped bare.</summary>
        public int Remaining;

        /// <summary>True once the node has been set up, so a load is not overwritten by seeding.</summary>
        public bool Seeded;

        /// <summary>
        /// Seconds of play remaining before a stripped node regrows. Zero when it is not regrowing.
        /// </summary>
        /// <remarks>
        /// A countdown rather than a timestamp. <c>Time.time</c> restarts at zero every session, so a
        /// stored timestamp compared against it after a load either regrows everything at once or
        /// nothing ever - and which one depends on how long the previous session ran, which is the
        /// worst kind of bug to reproduce.
        /// </remarks>
        public float RegrowIn;

        /// <inheritdoc />
        public override string StateKey => Key;
    }
}
