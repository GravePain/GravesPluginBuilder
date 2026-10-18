namespace Graves.Plugins.Skills
{
    /// <summary>
    /// Something a skill actually does when used.
    /// </summary>
    /// <remarks>
    /// A skill may carry several. They are applied in author order, so a skill that damages and then
    /// heals the user is two modules on one definition rather than a new kind of skill.
    ///
    /// <b>An effect never checks whether it is allowed.</b> Requirements and cooldowns are settled
    /// before anything is applied, so an effect that has been reached is an effect that should
    /// happen. Mixing the two is how a skill ends up half-applied when a later check fails.
    /// </remarks>
    public interface ISkillEffect
    {
        /// <summary>Do it.</summary>
        void Apply(SkillUse _use);
    }
}
