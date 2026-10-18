namespace Graves.Core
{
    /// <summary>
    /// Identifies which build of the framework is running.
    /// </summary>
    /// <remarks>
    /// This exists so a bug reported from a project that cannot be inspected still says what it is
    /// running. Without it, the first question on every report is "which version", and the honest
    /// answer would be that nobody knows.
    ///
    /// <b>Keep <see cref="Version"/> in step with package.json.</b> When the framework is consumed as
    /// a package the real version comes from there, and the diagnostic report prefers it; this
    /// constant is the answer when the framework is sitting in a project's Assets folder, where there
    /// is no package to ask.
    /// </remarks>
    public static class CoreVersion
    {
        /// <summary>Framework version. Must match the <c>version</c> field in package.json.</summary>
        public const string Version = "0.1.0";

        /// <summary>Package name, for locating the package entry when installed as one.</summary>
        public const string PackageName = "com.graves.core";
    }
}
