using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Graves.Core.Editor
{
    /// <summary>
    /// Produces a plain-text report describing this installation of the framework.
    /// </summary>
    /// <remarks>
    /// <b>Why this exists.</b> Serious testing happens in a real game project, which is a project the
    /// framework's authors cannot compile, play, or read a console from. Without something like this,
    /// every bug report starts with a round of questions - which version, which plugins, is the
    /// boundary intact, how many definitions - each costing a message and an answer.
    ///
    /// It reports facts that are read back from the running editor rather than assumed. The assembly
    /// section in particular is taken from the loaded domain, so it describes what is ACTUALLY
    /// referenced rather than what an assembly definition file claims.
    ///
    /// Nothing here is sent anywhere. It goes to the clipboard and the console, and it is the
    /// person's decision what to do with it.
    /// </remarks>
    public static class CoreDiagnostics
    {
        /// <summary>Builds the report, copies it to the clipboard, and logs it.</summary>
        [MenuItem("Graves/Copy Diagnostic Report")]
        public static void CopyToClipboard()
        {
            var report = Build();
            EditorGUIUtility.systemCopyBuffer = report;
            Debug.Log("[Graves] Diagnostic report copied to the clipboard.\n\n" + report);
        }

        /// <summary>Builds the report as text.</summary>
        public static string Build()
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== Graves framework diagnostic report ===");
            sb.AppendLine("generated  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine();

            AppendVersion(sb);
            AppendEnvironment(sb);
            AppendAssemblies(sb);
            AppendPlugins(sb);
            AppendData(sb);

            return sb.ToString();
        }

        private static void AppendVersion(StringBuilder _sb)
        {
            _sb.AppendLine("-- framework --");

            // When installed as a package the package manager is the authority; the constant is the
            // fallback for a copy sitting loose in a project's Assets folder.
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(CoreSettings).Assembly);

            if (package != null)
            {
                _sb.AppendLine("version    " + package.version + "  (package " + package.name + ")");
                _sb.AppendLine("source     " + package.source + " at " + package.resolvedPath);
            }
            else
            {
                _sb.AppendLine("version    " + CoreVersion.Version + "  (loose in Assets, not a package)");
            }

            _sb.AppendLine();
        }

        private static void AppendEnvironment(StringBuilder _sb)
        {
            _sb.AppendLine("-- environment --");
            _sb.AppendLine("unity      " + Application.unityVersion);
            _sb.AppendLine("platform   " + Application.platform);
            _sb.AppendLine("project    " + Application.productName);

            // currentRenderPipeline, not defaultRenderPipeline. The default is null whenever the
            // pipeline is set on the quality level rather than globally, and this project - which is
            // URP - reported "Built-in" until that was corrected. A confidently wrong fact in a
            // diagnostic report is worse than a missing one.
            var pipeline = GraphicsSettings.currentRenderPipeline ?? GraphicsSettings.defaultRenderPipeline;
            _sb.AppendLine("pipeline   " + (pipeline != null ? pipeline.name + " (" +
                                            pipeline.GetType().Name + ")" : "Built-in"));
            _sb.AppendLine("colorSpace " + PlayerSettings.colorSpace);
            _sb.AppendLine();
        }

        /// <summary>
        /// Reports the assembly reference graph, read from the loaded domain.
        /// </summary>
        /// <remarks>
        /// This is the section worth reading first on any report. The framework's central claim is
        /// that the core depends on no plugin, and this is the only place that claim is checked
        /// against reality instead of against an .asmdef file.
        /// </remarks>
        private static void AppendAssemblies(StringBuilder _sb)
        {
            _sb.AppendLine("-- assemblies (from the loaded domain, not from asmdef files) --");

            var ours = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name.StartsWith("Graves", StringComparison.Ordinal))
                .OrderBy(a => a.GetName().Name, StringComparer.Ordinal)
                .ToList();

            if (ours.Count == 0)
            {
                _sb.AppendLine("NONE FOUND - the framework is not compiled into this project.");
                _sb.AppendLine();
                return;
            }

            foreach (var assembly in ours)
            {
                var referenced = assembly.GetReferencedAssemblies()
                    .Select(r => r.Name)
                    .Where(n => n.StartsWith("Graves", StringComparison.Ordinal)
                             || n.StartsWith("Unity", StringComparison.Ordinal))
                    .OrderBy(n => n, StringComparer.Ordinal);

                _sb.AppendLine("  " + assembly.GetName().Name);
                _sb.AppendLine("      -> " + string.Join(", ", referenced));
            }

            // Called out explicitly because it is the one line most worth checking.
            var core = ours.FirstOrDefault(a => a.GetName().Name == "Graves.Core");
            if (core != null)
            {
                bool leaks = core.GetReferencedAssemblies()
                    .Any(r => r.Name.StartsWith("Graves.Plugin", StringComparison.Ordinal));
                _sb.AppendLine("  core depends on a plugin: " + (leaks ? "YES - THIS IS A BUG" : "no"));
            }

            _sb.AppendLine();
        }

        private static void AppendPlugins(StringBuilder _sb)
        {
            _sb.AppendLine("-- plugins --");

            var categories = DefinitionCatalogue.DiscoverCategories();
            var modules = DefinitionCatalogue.DiscoverModuleTypes();

            _sb.AppendLine("definition types  " + categories.Count);
            foreach (var entry in categories)
            {
                _sb.AppendLine("    " + entry.Category + "  (" + entry.Type.FullName + ", " +
                               entry.Type.Assembly.GetName().Name + ")");
            }

            _sb.AppendLine("module types      " + modules.Count);
            foreach (var type in modules)
            {
                _sb.AppendLine("    " + type.FullName + "  (" + type.Assembly.GetName().Name + ")");
            }

            // Two plugins claiming one state key corrupts saves without erroring, and a bug report
            // from a project nobody else can run should say so on its face.
            var keyProblems = DefinitionCatalogue.ValidateStateKeys();
            _sb.AppendLine("state key clashes " + keyProblems.Count);
            foreach (var problem in keyProblems) _sb.AppendLine("    " + problem);

            _sb.AppendLine();
        }

        /// <summary>
        /// Reports what has been authored, and every integrity problem found in it.
        /// </summary>
        /// <remarks>
        /// Deliberately runs the same <see cref="DefinitionRegistry.Validate"/> the window's Validate
        /// button runs, so a report and a manual check can never disagree.
        /// </remarks>
        private static void AppendData(StringBuilder _sb)
        {
            _sb.AppendLine("-- authored data --");

            var settings = CoreSettings.Instance;
            _sb.AppendLine("settings asset    " + (settings != null ? "found" : "MISSING"));
            if (settings != null)
            {
                _sb.AppendLine("registry linked   " + (settings.Registry != null ? "yes" : "NO"));
                _sb.AppendLine("default body      " +
                               (settings.DefaultRepresentation != null
                                   ? settings.DefaultRepresentation.name
                                   : "none (entities fall back to a primitive capsule)"));
            }

            var definitions = DefinitionCatalogue.LoadAll();
            _sb.AppendLine("definitions       " + definitions.Count);

            var byType = definitions.GroupBy(d => d.GetType().Name).OrderBy(g => g.Key);
            foreach (var group in byType) _sb.AppendLine("    " + group.Key + "  " + group.Count());

            var registry = settings != null ? settings.Registry : null;
            if (registry == null)
            {
                _sb.AppendLine("problems          cannot check - no registry");
                _sb.AppendLine();
                return;
            }

            var problems = registry.Validate();
            _sb.AppendLine("problems          " + problems.Count);
            foreach (var problem in problems) _sb.AppendLine("    " + problem);

            _sb.AppendLine();
        }
    }
}
