using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Gathering
{
    /// <summary>
    /// Somebody took something out of a node. Announced on <see cref="GameEvents"/>.
    /// </summary>
    /// <remarks>
    /// <b>Announced rather than only returned, because the interesting listeners are not here.</b>
    /// A quest counting logs, a tutorial waiting for a first pickup, a sound, a floating number — none
    /// of those belong to gathering, and none of them should have to be referenced by it.
    ///
    /// This one lives in the plugin rather than in the core, and the distinction is worth keeping
    /// straight: a listener must reference this assembly to name the type. That is right here, because
    /// anything interested in gathering is by definition already interested in gathering. Compare
    /// <see cref="EntityDied"/>, which is in the core precisely because things that care about death
    /// should not have to care about health.
    /// </remarks>
    public class ItemGathered : IGameEvent
    {
        /// <summary>Who gathered it.</summary>
        public readonly GameObject Gatherer;

        /// <summary>What was taken.</summary>
        public readonly ItemDefinition Item;

        /// <summary>How many, after any capacity limit was applied.</summary>
        public readonly int Count;

        /// <summary>True when that harvest emptied the node.</summary>
        public readonly bool Depleted;

        /// <summary>Creates the announcement.</summary>
        public ItemGathered(GameObject _gatherer, ItemDefinition _item, int _count, bool _depleted)
        {
            Gatherer = _gatherer;
            Item = _item;
            Count = _count;
            Depleted = _depleted;
        }
    }
}
