using System;
using UnityEngine;

namespace Graves.Core
{
    /// <summary>
    /// Something that can be dead, and says so when it happens.
    /// </summary>
    /// <remarks>
    /// <b>This exists so that "is it dead?" is not a question only one plugin can answer.</b> Three
    /// plugins asked it — a spawner pruning corpses from its population, a brain skipping dead targets,
    /// an experience reward paying out on a kill — and all three reached for the vitals plugin's
    /// <c>Health</c> component to get it. None of them wanted health. They wanted a yes or a no, and
    /// they took a dependency on a whole system of maximums, damage, healing and level scaling to get
    /// it.
    ///
    /// The cost was measurable and it was the second worst in the framework: removing vitals killed
    /// five other plugins outright. Four of those five did not care about health at all.
    ///
    /// <b>An interface only decouples when it lives where everyone already looks.</b> Putting this in
    /// the vitals plugin would have changed nothing — naming the type still requires referencing the
    /// assembly that declares it, so the dependency survives the indirection and only becomes harder
    /// to see. It is in the core because everything references the core already.
    ///
    /// <b>Health is not the only way to die.</b> A trap that deletes whatever it touches, a story beat
    /// that removes a character, a plant that has been harvested — each can implement this and every
    /// plugin above keeps working, without a health bar anywhere in the game.
    /// </remarks>
    public interface IMortal
    {
        /// <summary>True once this thing is dead. Nothing is required to make it true again.</summary>
        bool IsDead { get; }

        /// <summary>
        /// Raised once, when it dies. Carries whatever caused it, which may be null.
        /// </summary>
        /// <remarks>
        /// <b>Once.</b> A second killing blow on something already dead must not raise this again —
        /// two death events for one corpse is how a thing drops its loot twice, and the duplicate
        /// looks like a loot bug rather than a death bug.
        /// </remarks>
        event Action<GameObject> Died;
    }
}
