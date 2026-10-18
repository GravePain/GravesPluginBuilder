using System;
using System.IO;
using System.Text;
using Graves.Core;
using Graves.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace Graves.Proof
{
    /// <summary>
    /// Measures the things a fresh install gets wrong, rather than asserting them.
    /// </summary>
    /// <remarks>
    /// Each of these was a suspicion first. Written as a measurement because a packaging report full
    /// of confident guesses is worth less than one short list of things that were actually observed.
    /// </remarks>
    public static class InstallGaps
    {
        /// <summary>Runs the measurements and writes them beside the project.</summary>
        public static void Run()
        {
            var report = new StringBuilder();

            // 1. Does an entity built from script get a persistent id?
            var host = new GameObject("Gap Probe");
            host.SetActive(false);
            var live = host.AddComponent<GameEntity>();
            report.Append("persistent id after AddComponent from script: ")
                  .Append(string.IsNullOrEmpty(live.PersistentId) ? "EMPTY" : live.PersistentId)
                  .Append('\n');

            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Gaps"));
            const string path = "Assets/Gaps/GapProbe.prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(host, path);
            UnityEngine.Object.DestroyImmediate(host);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var reloaded = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var onPrefab = reloaded != null ? reloaded.GetComponent<GameEntity>() : null;
            report.Append("persistent id on the saved prefab: ")
                  .Append(onPrefab == null ? "no component"
                          : string.IsNullOrEmpty(onPrefab.PersistentId) ? "EMPTY" : onPrefab.PersistentId)
                  .Append('\n');

            AssetDatabase.DeleteAsset(path);

            // 2. Does the manual find anything in a project that is not the framework's repository?
            int pages = ManualPage.FindPages().Count;
            report.Append("manual pages found in a fresh install: ").Append(pages).Append('\n');
            foreach (var folder in ManualPage.Folders)
            {
                var root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", folder));
                report.Append("   root '").Append(folder).Append("' exists: ")
                      .Append(Directory.Exists(root)).Append('\n');
            }

            // 3. Where does authored data have to go?
            report.Append("data root the package insists on: ").Append(DefinitionCatalogue.DataRoot)
                  .Append(" (a compile-time constant inside the package)\n");

            // 4. What does the package actually ship?
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(GameDefinition).Assembly);
            if (info != null)
            {
                report.Append("package resolved from: ").Append(info.resolvedPath).Append('\n');
                report.Append("package documentationUrl: ")
                      .Append(string.IsNullOrEmpty(info.documentationUrl) ? "NONE" : info.documentationUrl)
                      .Append('\n');
                foreach (var name in new[] { "README.md", "CHANGELOG.md", "LICENSE.md", "Documentation~" })
                {
                    var at = Path.Combine(info.resolvedPath, name);
                    report.Append("   ships ").Append(name).Append(": ")
                          .Append(File.Exists(at) || Directory.Exists(at)).Append('\n');
                }
            }

            var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../gaps-report.txt"));
            File.WriteAllText(outPath, report.ToString());
            Debug.Log("[Gaps]\n" + report);

            EditorApplication.Exit(0);
        }
    }
}
