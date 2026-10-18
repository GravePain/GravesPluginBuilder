using UnityEngine;

namespace Graves.Core
{
    /// <summary>
    /// Something died. Announced on <see cref="GameEvents"/> for anyone who cares.
    /// </summary>
    /// <remarks>
    /// <b>Why this is in the core rather than in the plugin that raises it.</b> Health raises a local
    /// <c>Died</c> event too, and anything that already references the vitals plugin should use that
    /// one — it is direct, typed to the thing that died, and needs no bus.
    ///
    /// This exists for everyone else. A plugin that wants to react to a death without depending on
    /// vitals cannot subscribe to a type declared inside vitals: naming the type requires referencing
    /// the assembly, so the dependency survives the indirection. The event has to be declared
    /// somewhere everybody already looks, and that is here.
    ///
    /// <b>The same reason applies to a stranger.</b> Somebody extending this framework without
    /// editing it can subscribe to this and react to any death in the game, including deaths from a
    /// source that has nothing to do with health — a trap, a script, a harvested plant — because what
    /// raises it is <see cref="IMortal"/>, not a health bar.
    /// </remarks>
    public class EntityDied : IGameEvent
    {
        /// <summary>What died. Never null.</summary>
        public readonly GameObject Subject;

        /// <summary>What killed it, if anything said. Often null.</summary>
        public readonly GameObject Killer;

        /// <summary>Creates the announcement.</summary>
        public EntityDied(GameObject _subject, GameObject _killer)
        {
            Subject = _subject;
            Killer = _killer;
        }
    }
}
