using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Graves.Core.Editor
{
    /// <summary>
    /// Editor-side discovery and asset operations for definitions. Everything the authoring window
    /// does to disk goes through here, so the window itself stays presentation.
    /// </summary>
    /// <remarks>
    /// Categories are discovered by reflection over <see cref="DefinitionCategoryAttribute"/>, which is
    /// what lets a plugin appear in the window without the core being edited - and disappear cleanly
    /// when the plugin is deleted.
    /// </remarks>
    public static class DefinitionCatalogue
    {
        /// <summary>
        /// Where this project's authored data lives, kept apart from the framework itself.
        /// </summary>
        /// <remarks>
        /// This is deliberately NOT inside the framework folders. The core and the plugins ship; a
        /// game's definitions never do. Keeping them in separate trees is what makes the packaging
        /// boundary obvious rather than something to remember - see Docs/packaging.md.
        ///
        /// <b>Read from the settings asset, not baked in.</b> It was a constant, which meant a
        /// project that installed the framework as a package could not move its own authored content
        /// without forking. Falls back to the default before any settings asset exists, which is the
        /// state this window is in the first time it opens a fresh project.
        /// </remarks>
        public static string DataRoot
        {
            get
            {
                var settings = CoreSettings.Instance;
                return settings != null ? settings.DataRoot : CoreSettings.DefaultDataRoot;
            }
        }

        /// <summary>Folder holding the settings asset. Must be a <c>Resources</c> folder.</summary>
        public static string SettingsFolder => DataRoot + "/Resources";

        /// <summary>A definition type and the category it was declared under.</summary>
        public readonly struct CategoryEntry
        {
            /// <summary>The concrete <see cref="GameDefinition"/> subclass.</summary>
            public readonly Type Type;

            /// <summary>Sidebar heading it belongs to.</summary>
            public readonly string Category;

            /// <summary>Sort position within the sidebar.</summary>
            public readonly int Order;

            /// <summary>Creates an entry pairing a definition type with its declared category.</summary>
            public CategoryEntry(Type _type, string _category, int _order)
            {
                Type = _type;
                Category = _category;
                Order = _order;
            }
        }

        /// <summary>
        /// Every definition type carrying a <see cref="DefinitionCategoryAttribute"/>, sorted by
        /// declared order then name.
        /// </summary>
        /// <remarks>
        /// Uses <see cref="TypeCache"/>, which Unity maintains across domain reloads, rather than
        /// walking assemblies by hand. Abstract types are skipped - they cannot be authored.
        /// </remarks>
        public static List<CategoryEntry> DiscoverCategories()
        {
            var found = new List<CategoryEntry>();

            foreach (var type in TypeCache.GetTypesDerivedFrom<GameDefinition>())
            {
                if (type.IsAbstract) continue;

                var attribute = (DefinitionCategoryAttribute)Attribute.GetCustomAttribute(
                    type, typeof(DefinitionCategoryAttribute));
                if (attribute == null) continue;

                found.Add(new CategoryEntry(type, attribute.Category, attribute.Order));
            }

            return found
                .OrderBy(entry => entry.Order)
                .ThenBy(entry => entry.Category, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Every asset of the given definition type, sorted by display name.</summary>
        public static List<GameDefinition> LoadAll(Type _type)
        {
            var results = new List<GameDefinition>();
            if (_type == null) return results;

            foreach (var guid in AssetDatabase.FindAssets("t:" + _type.Name))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath(path, _type) as GameDefinition;

                // FindAssets matches subclasses too; keep only exact matches so a subclass does not
                // appear under two categories at once.
                if (asset != null && asset.GetType() == _type) results.Add(asset);
            }

            return results.OrderBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>Every definition in the project, of every type.</summary>
        public static List<GameDefinition> LoadAll()
        {
            var results = new List<GameDefinition>();

            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(GameDefinition)))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameDefinition>(path);
                if (asset != null) results.Add(asset);
            }

            return results;
        }

        /// <summary>
        /// Creates a new definition asset of the given type and returns it.
        /// </summary>
        public static GameDefinition Create(Type _type, string _displayName)
        {
            var folder = DataRoot + "/Definitions/" + _type.Name;
            EnsureFolder(folder);

            var instance = (GameDefinition)ScriptableObject.CreateInstance(_type);
            instance.EnsureUid();

            var safeName = string.IsNullOrWhiteSpace(_displayName) ? _type.Name : _displayName.Trim();
            var path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + safeName + ".asset");

            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssets();
            return instance;
        }

        /// <summary>
        /// Copies a definition, giving the copy a NEW identity.
        /// </summary>
        /// <remarks>
        /// Minting a fresh uid is the entire reason this method exists. Duplicating the asset on disk
        /// - through the project window, or by copying the file - carries the uid across, producing
        /// two definitions claiming to be the same thing. That is the one integrity failure
        /// <see cref="DefinitionRegistry.Validate"/> reports, and this is how it is avoided.
        /// </remarks>
        public static GameDefinition Duplicate(GameDefinition _source)
        {
            if (_source == null) return null;

            var sourcePath = AssetDatabase.GetAssetPath(_source);
            var newPath = AssetDatabase.GenerateUniqueAssetPath(sourcePath);
            if (!AssetDatabase.CopyAsset(sourcePath, newPath)) return null;

            var copy = AssetDatabase.LoadAssetAtPath<GameDefinition>(newPath);
            if (copy == null) return null;

            var serialized = new SerializedObject(copy);
            serialized.FindProperty("_uid").stringValue = Guid.NewGuid().ToString("N");
            serialized.FindProperty("_displayName").stringValue = _source.DisplayName + " Copy";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            return copy;
        }

        /// <summary>Deletes a definition asset and everything stored inside it.</summary>
        public static bool Delete(GameDefinition _definition)
        {
            if (_definition == null) return false;
            var path = AssetDatabase.GetAssetPath(_definition);
            return !string.IsNullOrEmpty(path) && AssetDatabase.DeleteAsset(path);
        }

        /// <summary>
        /// Reports two installed plugins claiming the same <c>InstanceState.StateKey</c>.
        /// </summary>
        /// <remarks>
        /// <b>Why this is worth a check of its own.</b> A state key is the address a save file writes
        /// against. Two types claiming the same one means a save restores one plugin's data into the
        /// other plugin's structure — silently, with no error, and presenting later as corrupted
        /// values rather than as a collision.
        ///
        /// It is not hypothetical. A category can have competing implementations — a slot-and-grid
        /// inventory and a weight-based one are candidates for the same slot, and both would
        /// naturally reach for the key "inventory". Installing both is the mistake; this is what says
        /// so out loud instead of letting the save find out.
        ///
        /// Types are instantiated to read the key, because <c>StateKey</c> is a virtual property
        /// rather than an attribute. A type with no parameterless constructor cannot be checked and is
        /// reported as such rather than skipped quietly.
        /// </remarks>
        public static List<string> ValidateStateKeys()
        {
            var problems = new List<string>();
            var claimedBy = new Dictionary<string, Type>();

            foreach (var type in TypeCache.GetTypesDerivedFrom<InstanceState>())
            {
                if (type.IsAbstract) continue;

                string key;
                try
                {
                    var probe = Activator.CreateInstance(type) as InstanceState;
                    if (probe == null) continue;
                    key = probe.StateKey;
                }
                catch (Exception error)
                {
                    problems.Add("'" + type.FullName + "' could not be created to read its StateKey (" +
                                 error.GetType().Name + "). Give it a parameterless constructor - a " +
                                 "save cannot rebuild it either.");
                    continue;
                }

                if (string.IsNullOrEmpty(key))
                {
                    problems.Add("'" + type.FullName + "' has an empty StateKey. It has no address to " +
                                 "be saved under.");
                    continue;
                }

                if (claimedBy.TryGetValue(key, out var other))
                {
                    problems.Add("'" + type.FullName + "' and '" + other.FullName + "' both use the " +
                                 "StateKey '" + key + "'. A save written with both installed restores " +
                                 "one into the other. If these are competing plugins for the same job, " +
                                 "install only one.");
                    continue;
                }

                claimedBy.Add(key, type);
            }

            return problems;
        }

        /// <summary>Every concrete module type a plugin has declared.</summary>
        public static List<Type> DiscoverModuleTypes()
        {
            return TypeCache.GetTypesDerivedFrom<DefinitionModule>()
                .Where(type => !type.IsAbstract)
                .OrderBy(type => type.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Creates a module of the given type as a sub-asset of the definition and attaches it.
        /// </summary>
        /// <remarks>
        /// Stored as a sub-asset so the module travels with its definition and cannot be orphaned by
        /// a move or a rename.
        /// </remarks>
        public static DefinitionModule AddModule(GameDefinition _definition, Type _moduleType)
        {
            if (_definition == null || _moduleType == null) return null;

            var module = (DefinitionModule)ScriptableObject.CreateInstance(_moduleType);
            module.name = _moduleType.Name;

            Undo.RegisterCreatedObjectUndo(module, "Add Module");
            AssetDatabase.AddObjectToAsset(module, _definition);

            Undo.RecordObject(_definition, "Add Module");
            _definition.AddModule(module);

            EditorUtility.SetDirty(_definition);
            AssetDatabase.SaveAssets();
            return module;
        }

        /// <summary>Detaches a module and destroys the sub-asset.</summary>
        /// <summary>
        /// Moves a module earlier or later in a definition's list, and returns whether it moved.
        /// </summary>
        /// <remarks>
        /// <b>Order is not cosmetic, which is why this exists.</b> Modules attach in the order they
        /// appear here, and a module that reads something another module installs must come after
        /// it. That is not a rule anybody can see in the inspector, and getting it wrong produces a
        /// bug that looks like a balance problem: on 2026-09-05 vitals attached before progression,
        /// concluded there was no level on the entity, cached that, and stopped scaling maximum
        /// health while damage - read later - scaled correctly.
        ///
        /// The framework's answer to that is a refresh call the installer makes, so ordering is not
        /// load-bearing for correctness. It is still load-bearing for reading the thing, and a list
        /// whose order matters and cannot be changed is a list that will be fought.
        /// </remarks>
        public static bool MoveModule(GameDefinition _definition, DefinitionModule _module, int _delta)
        {
            if (_definition == null || _module == null || _delta == 0) return false;

            var serialized = new SerializedObject(_definition);
            serialized.Update();

            var list = serialized.FindProperty("_modules");
            if (list == null || !list.isArray) return false;

            int from = -1;
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == _module) { from = i; break; }
            }
            if (from < 0) return false;

            int to = Mathf.Clamp(from + _delta, 0, list.arraySize - 1);
            if (to == from) return false;

            list.MoveArrayElement(from, to);
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(_definition);
            AssetDatabase.SaveAssets();
            return true;
        }

        public static void RemoveModule(GameDefinition _definition, DefinitionModule _module)
        {
            if (_definition == null || _module == null) return;

            Undo.RecordObject(_definition, "Remove Module");
            _definition.RemoveModule(_module);

            Undo.DestroyObjectImmediate(_module);
            EditorUtility.SetDirty(_definition);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Rebuilds the registry from what is actually on disk, creating the registry and settings
        /// assets if the project has none yet.
        /// </summary>
        /// <returns>The registry, never null.</returns>
        public static DefinitionRegistry RebuildRegistry()
        {
            var registry = LoadOrCreate<DefinitionRegistry>(DataRoot, "DefinitionRegistry");

            var all = LoadAll();
            foreach (var definition in all)
            {
                if (!definition.EnsureUid()) continue;
                EditorUtility.SetDirty(definition);
                Debug.Log("[Graves] Minted a uid for '" + definition.name + "', which had none.",
                          definition);
            }

            registry.SetContents(all);
            EditorUtility.SetDirty(registry);

            var settings = LoadOrCreate<CoreSettings>(SettingsFolder, CoreSettings.ResourcePath);
            var serialized = new SerializedObject(settings);
            var registryField = serialized.FindProperty("_registry");
            if (registryField.objectReferenceValue != registry)
            {
                registryField.objectReferenceValue = registry;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();
            CoreSettings.ClearCache();
            return registry;
        }

        /// <summary>The project's settings asset, creating it if absent.</summary>
        public static CoreSettings GetOrCreateSettings()
            => LoadOrCreate<CoreSettings>(SettingsFolder, CoreSettings.ResourcePath);

        private static T LoadOrCreate<T>(string _folder, string _assetName) where T : ScriptableObject
        {
            var existing = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            foreach (var guid in existing)
            {
                var found = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (found != null) return found;
            }

            EnsureFolder(_folder);
            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, _folder + "/" + _assetName + ".asset");
            AssetDatabase.SaveAssets();
            Debug.Log("[Graves] Created " + typeof(T).Name + " at " + _folder + "/" + _assetName +
                      ".asset - the project had none.", created);
            return created;
        }

        private static void EnsureFolder(string _path)
        {
            if (AssetDatabase.IsValidFolder(_path)) return;

            var parts = _path.Split('/');
            var built = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = built + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(built, parts[i]);
                built = next;
            }
        }
    }
}
