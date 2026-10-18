using System;
using System.IO;
using System.Text;
using Graves.Core;
using Graves.Core.Editor;
using Graves.Plugins.Vitals;
using UnityEditor;
using UnityEngine;

namespace Graves.Proof
{
    /// <summary>
    /// The smallest thing that proves the framework works when installed somewhere else.
    /// </summary>
    /// <remarks>
    /// This is not a test suite and does not pretend to be. It is one straight line through the
    /// contract the core actually makes: author a definition, give it a module, spawn it, change the
    /// state, write it out, read it back. If any step needs the original repository to be present,
    /// this is where it shows.
    /// </remarks>
    public static class InstallProof
    {
        private static readonly StringBuilder Report = new StringBuilder();
        private static int _failures;

        private static void Step(string _name, bool _passed, string _detail)
        {
            if (!_passed) _failures++;
            Report.Append(_passed ? "PASS  " : "FAIL  ").Append(_name);
            if (!string.IsNullOrEmpty(_detail)) Report.Append("   ").Append(_detail);
            Report.Append('\n');
        }

        /// <summary>Runs the proof and writes a report beside the project.</summary>
        public static void Run()
        {
            try
            {
                Execute();
            }
            catch (Exception error)
            {
                _failures++;
                Report.Append("THREW  ").Append(error.GetType().Name).Append(": ")
                      .Append(error.Message).Append('\n').Append(error.StackTrace).Append('\n');
            }

            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../proof-report.txt"));
            File.WriteAllText(path, Report.ToString());
            Debug.Log("[Proof]\n" + Report);

            EditorApplication.Exit(_failures == 0 ? 0 : 1);
        }

        private static void Execute()
        {
            // 1. The package resolved and its types are here.
            Step("core package present", typeof(GameDefinition) != null,
                 "assembly=" + typeof(GameDefinition).Assembly.GetName().Name);

            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(GameDefinition).Assembly);
            Step("core is a package, not loose files in Assets", packageInfo != null,
                 packageInfo != null
                     ? packageInfo.name + "@" + packageInfo.version + " source=" + packageInfo.source
                     : "FindForAssembly returned null - the core compiled as project code");

            // 2. The editor half of the core came with it.
            Step("core editor assembly present", typeof(DefinitionCatalogue) != null,
                 "assembly=" + typeof(DefinitionCatalogue).Assembly.GetName().Name);

            // 3. Plugins copied into Assets compile against the package.
            Step("vitals plugin bound to the packaged core",
                 typeof(VitalsModule).IsSubclassOf(typeof(DefinitionModule)),
                 "VitalsModule -> DefinitionModule across the package boundary");

            // 4. Settings and registry can be produced in a project that has never seen them.
            var settings = DefinitionCatalogue.GetOrCreateSettings();
            Step("settings created from nothing", settings != null,
                 settings != null ? AssetDatabase.GetAssetPath(settings) : "null");

            // 5. Author a definition through the same catalogue the window uses.
            var entity = DefinitionCatalogue.Create(typeof(EntityDefinition), "Proof Dummy")
                         as EntityDefinition;
            Step("definition authored", entity != null,
                 entity != null ? AssetDatabase.GetAssetPath(entity) : "null");
            if (entity == null) return;

            var vitals = DefinitionCatalogue.AddModule(entity, typeof(VitalsModule)) as VitalsModule;
            Step("plugin module added to a core definition", vitals != null,
                 "modules=" + entity.Modules.Count);

            var registry = DefinitionCatalogue.RebuildRegistry();
            AssetDatabase.SaveAssets();
            Step("registry rebuilt and resolves the new definition",
                 registry != null && registry.Resolve(entity.Uid) == entity,
                 "uid=" + entity.Uid);

            // 6. Save it as a prefab the play mode test can load.
            //
            // The runtime half is not attempted here, and the reason is a finding in its own right.
            // GameEntity does its entire job in Awake, and Unity does not call Awake in edit mode for
            // an ordinary MonoBehaviour. An -executeMethod run therefore cannot spawn anything,
            // whatever order the object is built in.
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources/Proof"));

            var host = new GameObject("Proof Dummy");
            host.SetActive(false);
            var live = host.AddComponent<GameEntity>();

            var so = new SerializedObject(live);
            so.Update();
            so.FindProperty("_definition").objectReferenceValue = entity;

            so.ApplyModifiedPropertiesWithoutUndo();

            // An earlier version set _persistentId by hand here, on the assumption that a component
            // added from script gets neither Reset nor OnValidate and so never mints one. Measured
            // in InstallGaps: it does get an id, and the same id survives onto the saved prefab. The
            // assumption was wrong and the line is gone.

            const string prefabPath = "Assets/Resources/Proof/ProofDummy.prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(host, prefabPath);
            UnityEngine.Object.DestroyImmediate(host);

            Step("entity prefab authored for the play mode half", saved != null, prefabPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
