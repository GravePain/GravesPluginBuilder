namespace Graves.Plugins.Skills
{
    /// <summary>
    /// Something about the user's condition that stops them using ANY skill.
    /// </summary>
    /// <remarks>
    /// <b>Different from a requirement, and the difference is who it belongs to.</b> A requirement
    /// is authored on a skill and answers "does this particular skill allow itself right now" - a
    /// weapon in hand, a reagent, a target. A blocker is a component on the user and answers "is
    /// this creature in any state to act at all" - dead, stunned, silenced, frozen.
    ///
    /// Putting the second on every skill as a requirement would mean every skill ever written
    /// carrying a module saying it does not work while dead, and the one that forgot would be
    /// castable by a corpse. That is not hypothetical: it is exactly what happened, and it was
    /// found by a player pressing the key while dead and reporting that the only thing still
    /// working was the skill.
    /// </remarks>
    public interface ISkillBlocker
    {
        /// <summary>True when this condition prevents skill use. Says why in plain words.</summary>
        bool BlocksSkills(out string _reason);
    }
}
