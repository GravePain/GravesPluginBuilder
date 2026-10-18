using UnityEngine;

namespace Graves.Core
{
    /// <summary>
    /// The extension point. A plugin adds data to the game by declaring a subclass of this; the core
    /// stores it on a <see cref="GameDefinition"/> without ever knowing the type.
    /// </summary>
    /// <remarks>
    /// This is the whole plugin contract at runtime, and it is deliberately one method. The core calls
    /// <see cref="AttachTo"/> once, when an entity wakes, and then gets out of the way - it does not
    /// tick, update, or drive anything. A module that needs a frame loop adds its own component to the
    /// entity and owns it from there.
    ///
    /// A module is normally stored as a sub-asset of the definition it belongs to, so it travels with
    /// it and cannot be orphaned.
    /// </remarks>
    public abstract class DefinitionModule : ScriptableObject
    {
        /// <summary>
        /// Called once per entity, during <c>Awake</c>, in author order. Default does nothing, so a
        /// module that is pure data needs no code at all.
        /// </summary>
        /// <param name="_entity">The live entity this definition was spawned as.</param>
        public virtual void AttachTo(GameEntity _entity) { }
    }
}
