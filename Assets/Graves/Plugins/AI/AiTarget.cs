using System.Collections.Generic;
using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.AI
{
    /// <summary>
    /// Marks something that hostile brains will notice and go after.
    /// </summary>
    /// <remarks>
    /// <b>An explicit marker rather than an inference.</b> A brain could look for the player by
    /// asking which entity has a person driving it, but that would make every AI depend on the
    /// player-input plugin, and it would silently stop working the moment a game wanted a monster to
    /// chase a villager or a summoned pet to be attacked. Saying outright which things are worth
    /// chasing costs one module on a definition and keeps the AI ignorant of who is playing.
    ///
    /// <b>It is deliberately not a faction system.</b> There are no teams, no hostility matrix, no
    /// reputation. Everything carrying this is a target to everything that hunts, which is wrong for
    /// a real game and exactly right for the first one. When two kinds of hostility are genuinely
    /// needed, this is the thing that grows a field, and no brain has to change to gain it.
    ///
    /// <b>Instances keep their own register.</b> The alternative is every brain searching the scene
    /// each time it wants a target, which is the classic way an enemy count turns into a frame rate
    /// problem. A list that maintains itself on enable and disable costs nothing to read.
    /// </remarks>
    [DisallowMultipleComponent]
    public class AiTarget : MonoBehaviour
    {
        private static readonly List<AiTarget> Active = new List<AiTarget>();

        /// <summary>Every target currently in the scene and enabled.</summary>
        /// <remarks>
        /// Exposed as a read-only view so a brain cannot quietly remove something it has lost
        /// interest in. Entries are never null while enabled, because the register is maintained by
        /// the objects themselves rather than swept later.
        /// </remarks>
        public static IReadOnlyList<AiTarget> All => Active;

        /// <summary>The entity this target belongs to, when it has one.</summary>
        public GameEntity Entity { get; private set; }

        private void Awake() => Entity = GetComponent<GameEntity>();

        private void OnEnable()
        {
            if (!Active.Contains(this)) Active.Add(this);
        }

        private void OnDisable() => Active.Remove(this);
    }
}
