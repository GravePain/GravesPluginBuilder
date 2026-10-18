using UnityEngine;

namespace Graves.Core
{
    /// <summary>
    /// Anything in the world that acts or is acted upon - the player, an NPC, an enemy. One type
    /// covers all three; what separates them is data and modules, not a class hierarchy.
    /// </summary>
    /// <remarks>
    /// The core knows nothing here about 2D or 3D, about how the thing moves, or about how it
    /// collides. <see cref="Representation"/> is a prefab slot and nothing more: the core will spawn
    /// it, and has no opinion on what is inside it.
    ///
    /// <b>There is deliberately no "kind" field.</b> One existed - player, NPC, enemy - and was
    /// removed on 2026-09-05 with no consumer anywhere in the project. What separates a player from
    /// a monster is which modules a definition carries: one that reads a keyboard, or one that picks
    /// its own targets. Swap those and the same body changes hands. A field saying the same thing
    /// would be a second truth to keep in step, and able to disagree with the first.
    /// </remarks>
    [DefinitionCategory("Entities", Order = 10)]
    [CreateAssetMenu(fileName = "Entity", menuName = "Graves/Entity Definition")]
    public class EntityDefinition : GameDefinition
    {
        [SerializeField]
        [Tooltip("Optional prefab spawned as this entity's body. Leave empty to fall back to the " +
                 "project default, and then to a primitive capsule.")]
        private GameObject _representation;

        /// <summary>Optional prefab used as this entity's body. May be null.</summary>
        public GameObject Representation => _representation;
    }
}
