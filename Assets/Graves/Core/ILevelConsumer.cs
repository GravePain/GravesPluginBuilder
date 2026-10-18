namespace Graves.Core
{
    /// <summary>
    /// Something that reads a level and holds on to where it found it.
    /// </summary>
    /// <remarks>
    /// <b>This exists so that progression can tell things to look again without knowing what they
    /// are.</b> Modules attach in author order. Anything reading a level before a level source has
    /// been installed correctly concludes there is none, caches that, and never scales again.
    ///
    /// The obvious fix is for the progression module to reach out and refresh health and the
    /// attacker by name, which would make a plugin about levels depend on a plugin about combat, and
    /// force every game wanting levels to ship a combat system. An interface costs nothing and keeps
    /// the dependency pointing the right way: consumers know about levels, levels know nothing about
    /// consumers.
    /// </remarks>
    public interface ILevelConsumer
    {
        /// <summary>Look for a level source again, discarding whatever was found before.</summary>
        void RefreshLevelSource();
    }
}
