namespace Graves.Plugins.Skills
{
    /// <summary>
    /// Something that must be true before a skill may be used.
    /// </summary>
    /// <remarks>
    /// <b>A requirement explains itself.</b> The reason is returned rather than logged, so a caller
    /// can put it in front of the player - "you need a weapon" beats a button that does nothing.
    /// The reference gates skills on a weapon category and returns a bare false; the player is left
    /// to work out why nothing happened.
    ///
    /// Implemented by a module on the skill, so requirements are authored per skill and any plugin
    /// can invent a new kind without the skills plugin knowing it exists.
    /// </remarks>
    public interface ISkillRequirement
    {
        /// <summary>True when this requirement is satisfied. When false, says why in plain words.</summary>
        bool IsMet(SkillUse _use, out string _reason);
    }
}
