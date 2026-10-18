using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Graves.Core.Editor
{
    /// <summary>
    /// Finds the project's own documentation and renders it inside the authoring window.
    /// </summary>
    /// <remarks>
    /// <b>The point is not convenience, it is that the documents get read.</b> This repository's
    /// hardest-won lesson is that anything living only in a session dies with it, and the answer has
    /// been to write things down. Writing them down is only half of it: the plugin guide is the
    /// instruction set every future author inherits, and it currently sits in a folder nobody has
    /// open while they are authoring. One click away from the thing being authored is a different
    /// document from one in a folder.
    ///
    /// <b>A small hand-rolled parser rather than a dependency.</b> The documents here are plain -
    /// headings, bold, code, bullets, tables, rules - and full Markdown compliance was never the
    /// goal. A parser that handles what is actually written is a hundred lines; a dependency that
    /// handles everything is a dependency, and this framework has a rule about those.
    ///
    /// Taken from a manual renderer Graves had already built for another project, which does the
    /// same job and also exports to HTML for printing. Export is not here yet - it is worth having
    /// and it is not what makes the documents get read.
    /// </remarks>
    public static class ManualPage
    {
        /// <summary>
        /// Where the documents live, relative to the project root rather than to Assets.
        /// </summary>
        /// <remarks>
        /// Outside <c>Assets</c> deliberately: they are not game content, Unity should not import
        /// them, and they belong to the repository rather than to the build.
        ///
        /// <b>Two roots, in the order they are listed.</b> The first is the framework's own
        /// documentation - the plugin contract, the tooling notes, the policies. The second is a
        /// building agent's working record: the decisions taken, why, and the ones that turned out
        /// wrong. Both are things the next author needs and neither is more use than the other in a
        /// folder nobody has open.
        /// </remarks>
        public static readonly string[] Folders = { "Docs", "Agent Memory" };

        /// <summary>The framework's own documentation, shipped inside the package.</summary>
        /// <remarks>
        /// A <c>~</c> folder is invisible to Unity's asset pipeline and travels inside a package, which
        /// is exactly the pair of properties wanted: the guide is not game content and must not be
        /// imported, and it must arrive with the framework rather than be left behind in the
        /// repository that built it.
        ///
        /// Added after installing the framework somewhere else for the first time and finding this tab
        /// silently empty - both project-relative roots were absent, and the documents the next author
        /// most needs were the ones that did not travel.
        /// </remarks>
        public const string PackageDocumentation = "Documentation~";

        /// <summary>Where the documents live. The first root, kept for messages.</summary>
        public const string Folder = "Docs";

        /// <summary>
        /// Every document from every root, grouped by root in the order they are declared and sorted
        /// within each.
        /// </summary>
        /// <remarks>
        /// Grouped rather than merged alphabetically, so the framework's contract does not end up
        /// interleaved with one agent's working notes. They are different kinds of document and a
        /// reader is almost always after one kind or the other.
        /// </remarks>
        public static List<string> FindPages()
        {
            var found = new List<string>();

            foreach (var root in Roots())
            {
                var here = new List<string>(Directory.GetFiles(root, "*.md", SearchOption.AllDirectories));
                here.Sort(System.StringComparer.OrdinalIgnoreCase);
                found.AddRange(here);
            }

            return found;
        }

        /// <summary>
        /// Every documentation root that exists, framework first and then the project's own.
        /// </summary>
        /// <remarks>
        /// The framework's root comes first deliberately. In a consuming project it is usually the
        /// only one, and it holds the contract a new author has to read before anything else.
        /// </remarks>
        public static List<string> Roots()
        {
            var roots = new List<string>();

            var package = PackageDocumentationRoot();
            if (!string.IsNullOrEmpty(package) && Directory.Exists(package)) roots.Add(package);

            foreach (var folder in Folders)
            {
                var root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", folder));
                if (Directory.Exists(root)) roots.Add(root);
            }

            return roots;
        }

        /// <summary>
        /// Where the framework's own documentation is on this machine, or null when it cannot be found.
        /// </summary>
        /// <remarks>
        /// Installed as a package, the package manager knows where it resolved to. Sitting loose in an
        /// Assets folder - which is how the repository that builds this framework uses it, and the only
        /// project in the world that does - there is no package to ask, so the core's own assembly
        /// definition is located instead and the folder beside it is used. No path is hardcoded either
        /// way.
        /// </remarks>
        public static string PackageDocumentationRoot()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(CoreSettings).Assembly);

            if (package != null) return Path.Combine(package.resolvedPath, PackageDocumentation);

            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("Graves.Core t:AssemblyDefinitionAsset"))
            {
                var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (!assetPath.EndsWith("Graves.Core.asmdef", System.StringComparison.Ordinal)) continue;

                var folder = Path.GetDirectoryName(Path.GetFullPath(assetPath));
                return folder == null ? null : Path.Combine(folder, PackageDocumentation);
            }

            return null;
        }

        /// <summary>
        /// Why the manual is empty, for the case where it is.
        /// </summary>
        /// <remarks>
        /// A tab that is blank and says nothing is the worst outcome available: the documents exist,
        /// they were simply left behind, and silence gives the reader no way to know that. This is
        /// the message that should have been there the first time this framework was installed
        /// somewhere else.
        /// </remarks>
        public static string EmptyExplanation()
        {
            var package = PackageDocumentationRoot();

            if (string.IsNullOrEmpty(package))
            {
                return "No documentation found, and the framework's own documentation folder could " +
                       "not be located at all. That means Graves.Core's assembly definition was not " +
                       "found, which is a stranger problem than a missing manual.";
            }

            var text = new StringBuilder();
            text.AppendLine("No documentation found.");
            text.AppendLine();
            text.AppendLine("The framework ships its own guide in a folder called " +
                            PackageDocumentation + " beside the core, and nothing is there:");
            text.AppendLine(package);
            text.AppendLine();
            text.AppendLine("If this framework was installed by copying folders rather than as " +
                            "a package, that folder was probably left behind - a tilde folder " +
                            "is invisible to Unity and easy to miss.");
            text.AppendLine();
            text.Append("The project's own notes would live in Docs or Agent Memory beside " +
                        "Assets, and neither exists here either.");
            return text.ToString();
        }

        /// <summary>
        /// A readable title for a document: its folder and filename, spaced out, without the ordering
        /// prefix or the extension.
        /// </summary>
        public static string TitleFor(string _path)
        {
            if (string.IsNullOrEmpty(_path)) return string.Empty;

            var name = Path.GetFileNameWithoutExtension(_path);
            name = Regex.Replace(name, @"^\d+[-_]", string.Empty);
            name = name.Replace('-', ' ').Replace('_', ' ');

            // Which root this came from, and any folder inside it. With two roots the folder is no
            // longer decoration - "decisions / D-003" and "reviewer / findings" are different kinds
            // of document and the reader needs to know which they are opening.
            var directory = Path.GetDirectoryName(_path) ?? string.Empty;
            var parent = Path.GetFileName(directory);

            // The framework's own folder is called Documentation~ on disk, which is a Unity
            // convention rather than a name a reader should have to see.
            if (string.Equals(parent, PackageDocumentation, System.StringComparison.OrdinalIgnoreCase))
            {
                return "Framework  /  " + name;
            }

            bool isRoot = false;
            foreach (var folder in Folders)
            {
                if (string.Equals(parent, folder, System.StringComparison.OrdinalIgnoreCase)) isRoot = true;
            }

            if (isRoot) return parent + "  /  " + name;

            var grandparent = Path.GetFileName(Path.GetDirectoryName(directory) ?? string.Empty);
            return string.IsNullOrEmpty(grandparent)
                 ? parent + "  /  " + name
                 : grandparent + "  /  " + parent + "  /  " + name;
        }

        /// <summary>
        /// Renders one document into a container as a stack of labels.
        /// </summary>
        /// <remarks>
        /// Labels rather than a single rich-text block, because a fenced code block wants a different
        /// font and no wrapping while prose wants both, and one label cannot be two things. It also
        /// means a very long document costs a lot of elements - acceptable for documents of this
        /// size, and the first thing to change if they grow.
        /// </remarks>
        public static void Render(VisualElement _into, string _path)
        {
            if (_into == null) return;
            _into.Clear();

            if (string.IsNullOrEmpty(_path) || !File.Exists(_path))
            {
                _into.Add(Body("That document is no longer on disk."));
                return;
            }

            var lines = File.ReadAllLines(_path);
            var paragraph = new StringBuilder();
            var code = new StringBuilder();
            bool inCode = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                if (line.TrimStart().StartsWith("```"))
                {
                    if (inCode)
                    {
                        _into.Add(Code(code.ToString().TrimEnd()));
                        code.Clear();
                    }
                    else
                    {
                        Flush(_into, paragraph);
                    }
                    inCode = !inCode;
                    continue;
                }

                if (inCode) { code.AppendLine(line); continue; }

                var trimmed = line.Trim();

                if (trimmed.Length == 0) { Flush(_into, paragraph); continue; }

                if (trimmed.StartsWith("#"))
                {
                    Flush(_into, paragraph);
                    int depth = 0;
                    while (depth < trimmed.Length && trimmed[depth] == '#') depth++;
                    _into.Add(Heading(Inline(trimmed.Substring(depth).Trim()), depth));
                    continue;
                }

                if (trimmed == "---" || trimmed == "***" || trimmed == "___")
                {
                    Flush(_into, paragraph);
                    _into.Add(Rule());
                    continue;
                }

                // A table is shown as-is in a fixed-width block. Laying pipe tables out properly is a
                // lot of work for documents that already line their columns up by hand.
                if (trimmed.StartsWith("|"))
                {
                    Flush(_into, paragraph);
                    _into.Add(Code(line.TrimEnd()));
                    continue;
                }

                if (trimmed.StartsWith("- ") || trimmed.StartsWith("* ") ||
                    Regex.IsMatch(trimmed, @"^\d+\. "))
                {
                    Flush(_into, paragraph);
                    _into.Add(Bullet(Inline(Regex.Replace(trimmed, @"^([-*]|\d+\.)\s+", string.Empty))));
                    continue;
                }

                if (paragraph.Length > 0) paragraph.Append(' ');
                paragraph.Append(trimmed);
            }

            if (inCode && code.Length > 0) _into.Add(Code(code.ToString().TrimEnd()));
            Flush(_into, paragraph);
        }

        private static void Flush(VisualElement _into, StringBuilder _paragraph)
        {
            if (_paragraph.Length == 0) return;
            _into.Add(Body(Inline(_paragraph.ToString())));
            _paragraph.Clear();
        }

        /// <summary>
        /// Converts the inline marks this project actually uses into Unity rich text.
        /// </summary>
        /// <remarks>
        /// Bold before italic, because a single asterisk pattern would otherwise eat half of a double
        /// one and leave stray marks through the text. Angle brackets are escaped first, or a snippet
        /// mentioning a generic type disappears into what Unity reads as a tag.
        /// </remarks>
        private static string Inline(string _text)
        {
            if (string.IsNullOrEmpty(_text)) return string.Empty;

            var text = _text.Replace("<", "&lt;").Replace(">", "&gt;");

            text = Regex.Replace(text, @"\*\*(.+?)\*\*", "<b>$1</b>");
            text = Regex.Replace(text, @"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", "<i>$1</i>");
            text = Regex.Replace(text, @"`(.+?)`", "<color=#c8a0ff>$1</color>");
            text = Regex.Replace(text, @"\[(.+?)\]\((.+?)\)", "<color=#6bb8ff>$1</color>");

            return text;
        }

        private static Label Body(string _text)
        {
            var label = new Label(_text) { enableRichText = true };
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.paddingLeft = 10f;
            label.style.paddingRight = 14f;
            label.style.paddingBottom = 6f;
            return label;
        }

        private static Label Bullet(string _text)
        {
            var label = Body("  •  " + _text);
            label.style.paddingLeft = 20f;
            label.style.paddingBottom = 2f;
            return label;
        }

        private static Label Heading(string _text, int _depth)
        {
            var label = new Label(_text) { enableRichText = true };
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.fontSize = _depth <= 1 ? 18f : _depth == 2 ? 14f : 12f;
            label.style.paddingLeft = 10f;
            label.style.paddingTop = _depth <= 2 ? 12f : 8f;
            label.style.paddingBottom = 4f;
            return label;
        }

        private static VisualElement Code(string _text)
        {
            var label = new Label(_text) { enableRichText = false };
            label.style.whiteSpace = WhiteSpace.Pre;
            label.style.paddingLeft = 10f;
            label.style.paddingRight = 10f;
            label.style.paddingTop = 6f;
            label.style.paddingBottom = 6f;
            label.style.marginLeft = 10f;
            label.style.marginRight = 14f;
            label.style.marginBottom = 6f;
            label.style.backgroundColor = new Color(0f, 0f, 0f, 0.25f);
            label.style.color = new Color(0.78f, 0.85f, 0.72f);

            var scroll = new ScrollView(ScrollViewMode.Horizontal);
            scroll.Add(label);
            return scroll;
        }

        private static VisualElement Rule()
        {
            var line = new VisualElement();
            line.style.height = 1f;
            line.style.marginTop = 8f;
            line.style.marginBottom = 8f;
            line.style.marginLeft = 10f;
            line.style.marginRight = 14f;
            line.style.backgroundColor = new Color(1f, 1f, 1f, 0.15f);
            return line;
        }
    }
}
