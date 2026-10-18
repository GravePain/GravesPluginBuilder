namespace Graves.Core
{
    /// <summary>
    /// Something on this entity that knows what level it is.
    /// </summary>
    /// <remarks>
    /// <b>Pulled, not pushed, and for the same reason the bonus interfaces are.</b> A level could be
    /// pushed into every stat whenever it changes, which means every stat must be told, and the one
    /// that is forgotten silently keeps its old value. Asking at the moment of use has no such
    /// failure: there is nothing to remember and nothing to go stale.
    ///
    /// <b>No source means level one.</b> A game with no progression system never implements this,
    /// every level-based value evaluates at its base, and nothing has to know that levels are absent.
    /// That is what let vitals and combat be built and shipped before progression existed.
    ///
    /// <b>Where this lives, and the two moves it took to get here.</b> It started in the vitals
    /// assembly, next to its first consumer, with a note saying it should move if a third consumer
    /// appeared that wanted levels without wanting health. Skills was the third, so it moved into a
    /// stats assembly of its own. Then removing that assembly turned out to break seven plugins and
    /// fourteen assemblies - more than removing anything else in the framework - which is not a
    /// plugin with many dependents but a standard library that had been mislabelled as optional. So
    /// it moved again, into the core.
    ///
    /// Both moves were made because a number said so rather than because the shape felt wrong, and
    /// the rule underneath them is the same one each time: something everything needs is not a
    /// plugin, and something one thing needs is not an assembly.
    /// </remarks>
    public interface ILevelSource
    {
        /// <summary>The current level. One-based, and never below one.</summary>
        int Level { get; }
    }
}
