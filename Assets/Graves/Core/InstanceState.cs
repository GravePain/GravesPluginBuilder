using System;

namespace Graves.Core
{
    /// <summary>
    /// Per-object state added by a plugin: durability on this sword, charges on this wand, health on
    /// this particular goblin. The instance-side counterpart to <see cref="DefinitionModule"/>.
    /// </summary>
    /// <remarks>
    /// <b>Why this is a plain class and not a ScriptableObject.</b> A module is authored once and
    /// lives in an asset, so it is a ScriptableObject. State belongs to one object at runtime, is
    /// created and destroyed constantly, and has to travel into a save file. An asset per sword would
    /// be absurd. The core holds these in a <c>[SerializeReference]</c> list, which is what lets it
    /// store a plugin's type without naming it — the same trick as modules, by a different mechanism
    /// because the storage is different.
    ///
    /// <b>Keep it data.</b> No behaviour, no references to scene objects, no references to assets
    /// that a save cannot survive. If state needs to name a definition, it names the uid. Anything
    /// stored here should still make sense six months later in a save file written by an older build,
    /// which is the situation this whole design exists for.
    ///
    /// <b>A note for whoever writes a save plugin.</b> Unity's <c>JsonUtility</c> DOES handle
    /// <c>[SerializeReference]</c>: it writes a <c>references.RefIds</c> block recording each
    /// concrete type's class, namespace and assembly, and reads it back. Verified 2026-09-05 — an
    /// earlier version of this comment claimed the opposite and was wrong.
    ///
    /// It is still worth serialising each state separately rather than relying on that, for one
    /// reason: a type that cannot be resolved on load is silently dropped by the built-in mechanism,
    /// and the next save then writes it out of existence. Handling each state as an opaque blob keeps
    /// the bytes of an uninstalled plugin's state recoverable. The format remains a plugin's decision
    /// and deliberately not the core's — see <see cref="ISaveable"/>.
    /// </remarks>
    [Serializable]
    public abstract class InstanceState
    {
        /// <summary>
        /// Identifies this state within its instance. Must be stable across versions — it is how a
        /// save file names this piece of state, and changing it orphans existing saves.
        /// </summary>
        /// <remarks>
        /// Defaults to the type name, which is right until a plugin is renamed. Override and return a
        /// literal string the moment state is being persisted, so a refactor cannot silently break
        /// every save that has ever been written.
        ///
        /// <b>A save addresses state by this key, not by the class.</b> That is what makes the
        /// sentence above worth acting on: rename the class and the saves still load, because the key
        /// did not move. Leave it as the default and a rename is exactly the silent break it warns
        /// about - the class name WAS the key, so the key changed too.
        ///
        /// <b>Two types must never claim the same key.</b> One plugin's data would be restored into
        /// the other plugin's structure. The editor's catalogue checks for it, and a load that meets
        /// it reports and restores nothing rather than writing the wrong thing.
        /// </remarks>
        public virtual string StateKey => GetType().Name;
    }
}
