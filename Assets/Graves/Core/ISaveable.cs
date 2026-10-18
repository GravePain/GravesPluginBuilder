namespace Graves.Core
{
    /// <summary>
    /// Implemented by anything with state worth keeping across sessions. The core defines the contract
    /// and now also ships one implementation of it.
    /// </summary>
    /// <remarks>
    /// <b>The core supplies a persister, and this was a reversal.</b> This said the format was a
    /// plugin's decision and the core would supply none, on the grounds that designing one with no
    /// plugin written would be designing against imagination. That reasoning was sound at the time and
    /// the conclusion stopped being right once the framework was something people install.
    ///
    /// A core that defines content, puts it in a world, and cannot make any of it survive a restart is
    /// not a thing anybody can use on its own. That is not a taste argument: the same arrangement is
    /// visible in a shipped framework whose core rates 3.3 while every one of its paid plugins rates
    /// 5.0 - buyers reached the socket, found it did nothing alone, and said so. Persistence is the
    /// one capability with no opinion about what kind of game this is, so it is the one that can be
    /// added here without deciding anything on a game's behalf.
    ///
    /// <b>The escape hatch stays.</b> <see cref="Saving.SaveSystem"/> is a default, not a mandate -
    /// its format is a JSON file on disk, and a game that wants player prefs, a cloud service or a
    /// binary format writes its own against this same interface. Nothing in the core requires the
    /// supplied one to be the one that runs.
    ///
    /// What the core does own is the part a plugin cannot supply for itself: identity. Saved state is
    /// addressed by a stable id, never by a name, an index or an object reference, so a save survives
    /// renaming, reordering and re-authoring. See <see cref="DefinitionRegistry"/> for why.
    ///
    /// <b>Two different keying schemes, and this interface uses the simpler one.</b> State belonging
    /// to an entity is addressed by that entity's instance id plus the state's own key. State reached
    /// through this interface belongs to no entity - a quest log, world flags - so there is nothing to
    /// pair with, and <see cref="SaveKey"/> alone is the address. <b>It must therefore be unique
    /// across the whole game.</b>
    ///
    /// An earlier version of this comment said saved state was keyed by a definition uid plus a local
    /// key. That described neither scheme and would have led a plugin author to design keys against a
    /// rule nothing implements. Corrected 2026-09-05 after review (finding F-003).
    /// </remarks>
    public interface ISaveable
    {
        /// <summary>
        /// The address this state is saved under. Must be unique across the whole game, and stable
        /// across versions - it is the entire key a save file is written against, so changing it
        /// orphans existing saves and colliding with another saveable overwrites them.
        /// </summary>
        string SaveKey { get; }

        /// <summary>Returns the state to persist, as a plain serializable object.</summary>
        object Capture();

        /// <summary>
        /// Restores previously captured state. Implementations must tolerate state written by an older
        /// version, including state that is null or of an unexpected shape.
        /// </summary>
        void Restore(object _state);
    }
}
