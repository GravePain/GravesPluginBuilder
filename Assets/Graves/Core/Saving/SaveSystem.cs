using System.Collections.Generic;
using UnityEngine;

namespace Graves.Core.Saving
{
    /// <summary>
    /// Writes and restores the state of every entity in the loaded scenes, plus anything else that
    /// implements <see cref="ISaveable"/>.
    /// </summary>
    /// <remarks>
    /// <b>This plugin extends no definition</b>, and that is worth noticing rather than hiding. The
    /// core's extension point attaches DATA to a definition; saving is a service that operates across
    /// everything and has no per-definition settings. It is still a plugin by every rule that matters
    /// - native C#, an assembly referencing only the core, deletable without breaking anything - it
    /// simply does not use <c>DefinitionModule</c>. Not every plugin will.
    ///
    /// <b>What is saved.</b> Every <see cref="GameEntity"/> with a persistent id, by that id, along
    /// with its instance state. Entities spawned at runtime are NOT handled: nothing in the framework
    /// spawns them yet, and inventing a scheme for a case that does not exist is how the wrong scheme
    /// gets locked in.
    ///
    /// <b>What it never does.</b> It does not delete state it cannot read, and it does not substitute
    /// anything for state it cannot resolve. A plugin that is currently uninstalled left state behind,
    /// and reinstalling it should bring that state back - so unreadable state is reported and
    /// preserved, never quietly dropped.
    /// </remarks>
    public static class SaveSystem
    {
        /// <summary>Format version written into new saves.</summary>
        public const int CurrentVersion = 1;

        /// <summary>
        /// State that could not be resolved on load, kept so the next save writes it back out.
        /// </summary>
        /// <remarks>
        /// <b>This is the difference between reporting a problem and destroying someone's save.</b>
        /// State whose plugin is uninstalled cannot be rebuilt into an instance, so it is not there to
        /// be captured next time - and without this, the next save silently drops it and reinstalling
        /// the plugin brings back nothing. The player loses their enchantments and nothing ever
        /// reported an error.
        ///
        /// It is a side table in the plugin rather than a field on the instance, because carrying
        /// unreadable bytes around is a save system's problem and the core should not know that save
        /// files exist.
        ///
        /// Keyed by instance id. Cleared per load, so it always describes the save currently in play.
        /// </remarks>
        private static readonly Dictionary<string, List<SavedState>> CarriedForward =
            new Dictionary<string, List<SavedState>>();

        /// <summary>
        /// Blobs nobody claimed on load, kept so the next save writes them back out.
        /// </summary>
        /// <remarks>
        /// The same protection as <see cref="CarriedForward"/>, for state that does not belong to an
        /// entity. It was missing entirely until review found it (finding F-001): the entity path was
        /// built, verified, and described as "unreadable state is never dropped" while the blob path
        /// one method away both failed to preserve an orphan AND failed to report one, because
        /// restoration was driven by the saveables present in the scene rather than by the contents of
        /// the file. A quest log plugin removed and reinstalled would have come back empty in silence.
        /// </remarks>
        private static readonly List<SavedBlob> CarriedForwardBlobs = new List<SavedBlob>();

        /// <summary>How many state entries are being carried forward from an uninstalled plugin.</summary>
        public static int CarriedForwardCount
        {
            get
            {
                int total = CarriedForwardBlobs.Count;
                foreach (var pair in CarriedForward) total += pair.Value.Count;
                return total;
            }
        }

        /// <summary>Forgets carried-forward state. Only for tests and tooling.</summary>
        public static void ClearCarriedForward()
        {
            CarriedForward.Clear();
            CarriedForwardBlobs.Clear();
        }

        /// <summary>Result of a load, so a caller can react without parsing log output.</summary>
        public class LoadResult
        {
            /// <summary>True when a file was found and read.</summary>
            public bool Loaded;

            /// <summary>Entities matched and restored.</summary>
            public int EntitiesRestored;

            /// <summary>Entities in the save with no matching entity in the scene.</summary>
            public int EntitiesMissing;

            /// <summary>How many entities the load had to build, because they were made at runtime.</summary>
            public int EntitiesRebuilt;

            /// <summary>State entries whose type could not be resolved. Preserved, not dropped.</summary>
            public int StateUnresolved;

            /// <summary>Human-readable problems, one per line.</summary>
            public List<string> Problems = new List<string>();
        }

        /// <summary>
        /// Writes every saveable thing in the loaded scenes to a slot.
        /// </summary>
        /// <returns>True when a file was written.</returns>
        public static bool Save(string _slot, string _label = null)
        {
            var file = new SaveFile
            {
                Version = CurrentVersion,
                FrameworkVersion = CoreVersion.Version,
                SavedAtUtc = System.DateTime.UtcNow.ToString("O"),
                Label = _label
            };

            var seen = new Dictionary<string, GameEntity>();

            foreach (var entity in Object.FindObjectsByType<GameEntity>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (entity == null || entity.Instance == null) continue;

                var id = entity.Instance.InstanceId;
                if (string.IsNullOrEmpty(id)) continue;

                // Made at runtime rather than placed. Skipped unless it asked to be kept - see
                // GameEntity.PersistAcrossSessions for why transient is the right default. Silent,
                // because this is the ordinary case: a spawner's occupants are meant to go.
                bool authored = !string.IsNullOrEmpty(entity.PersistentId);
                if (!authored && !entity.PersistAcrossSessions) continue;

                // Reported, never resolved by picking one. Two entities sharing an id would restore
                // each other's state, which is silent and extremely hard to see afterwards.
                if (seen.TryGetValue(id, out var other))
                {
                    Debug.LogError("[Save] '" + entity.name + "' and '" + other.name + "' share the " +
                                   "persistent id '" + id + "'. Neither is saved. This normally means " +
                                   "a scene object was copied, or a prefab carrying an id was placed " +
                                   "more than once.", entity);
                    continue;
                }

                seen.Add(id, entity);

                var captured = CaptureEntity(entity.Instance);
                if (!authored)
                {
                    captured.CreatedAtRuntime = true;
                    captured.Position = entity.transform.position;
                    captured.Rotation = entity.transform.rotation;
                }

                file.Entities.Add(captured);
            }

            var producedKeys = new HashSet<string>();

            foreach (var saveable in FindSaveables())
            {
                var blob = CaptureSaveable(saveable);
                if (blob == null) continue;

                // A blob is addressed by its key alone, so two saveables claiming the same key would
                // overwrite each other on load. Reported rather than silently letting one win.
                if (!producedKeys.Add(blob.Key))
                {
                    Debug.LogError("[Save] Two ISaveable components both use the SaveKey '" + blob.Key +
                                   "'. A key must be unique across the game. Only the first was saved.");
                    continue;
                }

                file.Blobs.Add(blob);
            }

            // Blobs whose plugin is not installed go back out untouched, unless it has since appeared
            // and produced one of its own above.
            foreach (var orphan in CarriedForwardBlobs)
            {
                if (!producedKeys.Contains(orphan.Key)) file.Blobs.Add(orphan);
            }

            SaveSlots.WriteAtomic(_slot, JsonUtility.ToJson(file, true));
            Debug.Log("[Save] Wrote slot '" + _slot + "': " + file.Entities.Count + " entities, " +
                      file.Blobs.Count + " blobs, to " + SaveSlots.PathFor(_slot));
            return true;
        }

        /// <summary>
        /// Restores a slot into the loaded scenes.
        /// </summary>
        /// <remarks>
        /// Matches entities by persistent id. An entity in the save with no counterpart in the scene
        /// is reported rather than ignored - it usually means the scene changed since the save, which
        /// is exactly the situation a player would otherwise experience as things silently missing.
        /// </remarks>
        public static LoadResult Load(string _slot)
        {
            var result = new LoadResult();

            var text = SaveSlots.Read(_slot);
            if (text == null)
            {
                result.Problems.Add("No save in slot '" + _slot + "'.");
                return result;
            }

            var file = JsonUtility.FromJson<SaveFile>(text);
            if (file == null)
            {
                result.Problems.Add("Slot '" + _slot + "' could not be parsed. It may be from a " +
                                    "different game, or truncated.");
                return result;
            }

            // A save from the future is refused rather than half-read. Reading it would apply whatever
            // this build happens to understand and discard the rest, which looks like corruption.
            if (file.Version > CurrentVersion)
            {
                result.Problems.Add("Slot '" + _slot + "' was written by a newer version (format " +
                                    file.Version + ", this build reads " + CurrentVersion +
                                    "). Refusing to load rather than read it partially.");
                return result;
            }

            result.Loaded = true;

            // Rebuilt from this load, so it always describes the save currently in play rather than
            // accumulating orphans from every save ever opened this session. Both tables, not just
            // the entity one - forgetting the blob table here would carry another save's orphans into
            // this one.
            ClearCarriedForward();

            var byId = new Dictionary<string, GameEntity>();
            foreach (var entity in Object.FindObjectsByType<GameEntity>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (entity == null || entity.Instance == null) continue;
                var id = entity.Instance.InstanceId;
                if (!string.IsNullOrEmpty(id) && !byId.ContainsKey(id)) byId.Add(id, entity);
            }

            foreach (var saved in file.Entities)
            {
                if (!byId.TryGetValue(saved.InstanceId, out var entity))
                {
                    // Nothing in the scene answers to this id. If it was made at runtime, that is
                    // expected and the save carries what it takes to build it again.
                    if (saved.CreatedAtRuntime)
                    {
                        entity = Rebuild(saved, result);
                        if (entity == null) continue;
                    }
                    else
                    {
                        result.EntitiesMissing++;
                        result.Problems.Add("Saved entity '" + saved.InstanceId +
                                            "' has no counterpart in the scene. The scene has " +
                                            "changed since this save was written.");
                        continue;
                    }
                }

                if (entity.Definition != null && saved.DefinitionUid != entity.Definition.Uid)
                {
                    result.Problems.Add("'" + entity.name + "' was saved as definition '" +
                                        saved.DefinitionUid + "' but is now '" + entity.Definition.Uid +
                                        "'. Its state is restored anyway; check this is intended.");
                }

                RestoreEntity(entity.Instance, saved, result);
                result.EntitiesRestored++;
            }

            // Driven from the saveables present, so blobs with no matching component are not visited
            // here. That gap is closed immediately below - it is what F-001 was.
            var claimed = new HashSet<string>();
            foreach (var saveable in FindSaveables())
            {
                if (RestoreSaveable(saveable, file, result)) claimed.Add(saveable.SaveKey);
            }

            foreach (var blob in file.Blobs)
            {
                if (claimed.Contains(blob.Key)) continue;

                CarriedForwardBlobs.Add(blob);
                result.StateUnresolved++;
                result.Problems.Add("Saved state '" + blob.Key + "' of type '" + blob.Type +
                                    "' has nothing in the scene to restore it - its plugin is not " +
                                    "installed. It is being carried forward, so saving now will not " +
                                    "lose it.");
            }

            if (result.Problems.Count > 0)
            {
                foreach (var problem in result.Problems) Debug.LogWarning("[Save] " + problem);
            }

            Debug.Log("[Save] Loaded slot '" + _slot + "': " + result.EntitiesRestored +
                      " entities restored (" + result.EntitiesRebuilt + " rebuilt), " +
                      result.EntitiesMissing + " missing, " +
                      result.StateUnresolved + " unresolved state entries.");
            return result;
        }

        /// <summary>Deletes a slot.</summary>
        public static bool Delete(string _slot) => SaveSlots.Delete(_slot);

        /// <summary>True when the slot exists.</summary>
        public static bool Exists(string _slot) => SaveSlots.Exists(_slot);

        /// <summary>Every slot on disk, newest first.</summary>
        public static List<string> Slots() => SaveSlots.List();

        private static SavedEntity CaptureEntity(GameInstance _instance)
        {
            var saved = new SavedEntity
            {
                InstanceId = _instance.InstanceId,
                DefinitionUid = _instance.DefinitionUid
            };

            foreach (var state in _instance.State)
            {
                if (state == null) continue;

                saved.State.Add(new SavedState
                {
                    Key = state.StateKey,
                    Type = TypeNames.Write(state.GetType()),

                    // Serialized one at a time so the result is an opaque blob this build can keep
                    // even when it cannot read it. JsonUtility would handle the polymorphism itself
                    // (verified - an earlier comment here claimed otherwise and was wrong), but it
                    // DROPS a type it cannot resolve, and the next save would then erase it.
                    Json = JsonUtility.ToJson(state)
                });
            }

            // Anything the last load could not read goes back out untouched, unless its plugin has
            // since been installed and the state is now genuinely present above.
            if (CarriedForward.TryGetValue(_instance.InstanceId, out var carried))
            {
                foreach (var orphan in carried)
                {
                    // Matched on key rather than type, for the same reason the restore is: a plugin
                    // that came back under a renamed class is the same plugin, and matching on the
                    // type string would write its state out twice - once live, once as the orphan
                    // that nothing will ever claim again.
                    bool nowResolved = false;
                    foreach (var written in saved.State)
                    {
                        bool sameKey = !string.IsNullOrEmpty(orphan.Key) && written.Key == orphan.Key;
                        if (sameKey || written.Type == orphan.Type) { nowResolved = true; break; }
                    }

                    if (!nowResolved) saved.State.Add(orphan);
                }
            }

            return saved;
        }

        /// <summary>
        /// Builds an entity the save recorded and the scene does not contain.
        /// </summary>
        /// <remarks>
        /// <b>It comes back as the same thing, not a copy of it.</b> The saved instance id is handed
        /// to the new instance, so the next save writes it under the id it already had and a load
        /// after that finds it again. Minting a fresh one would work exactly once and then quietly
        /// duplicate the thing on every subsequent cycle.
        ///
        /// The definition is resolved from the registry by uid, which is the whole reason a save
        /// stores uids rather than names or references.
        /// </remarks>
        private static GameEntity Rebuild(SavedEntity _saved, LoadResult _result)
        {
            var settings = CoreSettings.Instance;
            var registry = settings != null ? settings.Registry : null;

            if (registry == null)
            {
                _result.Problems.Add("Cannot rebuild saved entity '" + _saved.InstanceId +
                                     "': the project has no definition registry, so a uid cannot be " +
                                     "turned back into a definition.");
                return null;
            }

            var definition = registry.Resolve<EntityDefinition>(_saved.DefinitionUid);
            if (definition == null)
            {
                _result.Problems.Add("Cannot rebuild saved entity '" + _saved.InstanceId +
                                     "': no entity definition has uid '" + _saved.DefinitionUid +
                                     "'. It was deleted or its plugin is not installed.");
                return null;
            }

            var identity = new GameInstance(definition, _saved.InstanceId);
            var rebuilt = GameEntity.Create(definition, _saved.Position, _saved.Rotation, null, identity);
            if (rebuilt == null) return null;

            // Kept, or it would be saved once and lost on the cycle after this one.
            rebuilt.PersistAcrossSessions = true;
            _result.EntitiesRebuilt++;
            return rebuilt;
        }

        /// <summary>
        /// Restores state onto an instance, addressing each piece by its key.
        /// </summary>
        /// <remarks>
        /// Overwrites in place rather than replacing the list, so a plugin holding a reference to its
        /// own state object does not find itself writing to an orphan after a load - a bug that
        /// produces changes that appear to work and then vanish.
        ///
        /// <b>The key is the address; the type only says how to build one that is missing.</b> This
        /// was the other way round, and the reviewer was right that it made the framework's own advice
        /// false: <c>InstanceState</c> tells a plugin author that changing <c>StateKey</c> orphans
        /// saves, when what actually orphaned them was renaming the class or moving it between
        /// assemblies - the one thing a refactor does without anybody deciding to.
        ///
        /// Addressing by key is the same rule the rest of the framework already follows. A definition
        /// is found by uid and never by name, because a C# identifier is a thing tools rewrite. State
        /// is now found the same way, and a renamed class loads its old saves.
        ///
        /// <b>It also makes the key-collision check earn its place.</b> Two state types claiming one
        /// key used to be harmless here, because each was restored by its own type string. Now it
        /// would restore one plugin's data into the other's structure - so it is caught at load and
        /// reported, as well as by the editor check that was already looking for it.
        /// </remarks>
        private static void RestoreEntity(GameInstance _instance, SavedEntity _saved, LoadResult _result)
        {
            foreach (var savedState in _saved.State)
            {
                // By key first, and before the type is even resolved - which is what lets a state
                // whose class was renamed still find its way home, because the live state standing
                // there answers to the same key.
                InstanceState target = null;
                if (!string.IsNullOrEmpty(savedState.Key))
                {
                    foreach (var existing in _instance.State)
                    {
                        if (existing != null && existing.StateKey == savedState.Key)
                        {
                            target = existing;
                            break;
                        }
                    }
                }

                var type = TypeNames.Read(savedState.Type);

                if (target != null)
                {
                    // Both known and disagreeing means two types claim one key, which is the
                    // corruption the editor check exists to prevent. Reported and skipped rather than
                    // written, because writing it puts one plugin's data inside the other's structure
                    // and nothing downstream can tell that it happened.
                    if (type != null && target.GetType() != type)
                    {
                        _result.Problems.Add("State key '" + savedState.Key + "' is claimed by '" +
                                             savedState.Type + "' in the save and by '" +
                                             target.GetType().Name + "' in this build. Nothing was " +
                                             "restored for it - restoring would have put one " +
                                             "plugin's data into another plugin's structure.");
                        continue;
                    }

                    JsonUtility.FromJsonOverwrite(savedState.Json, target);
                    continue;
                }

                if (type == null)
                {
                    _result.StateUnresolved++;

                    // Held so the NEXT save writes it back out. Reporting it and moving on would mean
                    // the next save silently drops it, and reinstalling the plugin would restore
                    // nothing - the loss would be permanent, and nothing would ever have errored.
                    if (!CarriedForward.TryGetValue(_instance.InstanceId, out var carried))
                    {
                        carried = new List<SavedState>();
                        CarriedForward.Add(_instance.InstanceId, carried);
                    }
                    carried.Add(savedState);

                    _result.Problems.Add("State '" + savedState.Key + "' of type '" + savedState.Type +
                                         "' could not be resolved - its plugin is not installed. It " +
                                         "is being carried forward, so saving now will not lose it.");
                    continue;
                }

                // No key match. Either the file predates keys being written, or this plugin has not
                // attached any state to the entity yet. Fall back to the type.
                foreach (var existing in _instance.State)
                {
                    if (existing != null && existing.GetType() == type) { target = existing; break; }
                }

                if (target != null)
                {
                    JsonUtility.FromJsonOverwrite(savedState.Json, target);
                    continue;
                }

                var created = System.Activator.CreateInstance(type) as InstanceState;
                if (created == null)
                {
                    _result.Problems.Add("'" + savedState.Type + "' is not an InstanceState.");
                    continue;
                }

                JsonUtility.FromJsonOverwrite(savedState.Json, created);
                _instance.AddState(created);
            }
        }

        private static SavedBlob CaptureSaveable(ISaveable _saveable)
        {
            var captured = _saveable.Capture();
            if (captured == null) return null;

            return new SavedBlob
            {
                Key = _saveable.SaveKey,
                Type = TypeNames.Write(captured.GetType()),
                Json = JsonUtility.ToJson(captured)
            };
        }

        /// <summary>
        /// Restores one saveable from the file.
        /// </summary>
        /// <returns>
        /// True when a blob with this key was found and dealt with - including when its type could
        /// not be resolved, since in that case the blob is carried forward here rather than being
        /// left for the unclaimed sweep to handle twice.
        /// </returns>
        private static bool RestoreSaveable(ISaveable _saveable, SaveFile _file, LoadResult _result)
        {
            foreach (var blob in _file.Blobs)
            {
                if (blob.Key != _saveable.SaveKey) continue;

                var type = TypeNames.Read(blob.Type);
                if (type == null)
                {
                    _result.StateUnresolved++;
                    CarriedForwardBlobs.Add(blob);
                    _result.Problems.Add("Saved state '" + blob.Key + "' of type '" + blob.Type +
                                         "' could not be resolved even though something in the scene " +
                                         "claims that key. It is being carried forward untouched.");
                    return true;
                }

                var state = System.Activator.CreateInstance(type);
                JsonUtility.FromJsonOverwrite(blob.Json, state);
                _saveable.Restore(state);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Every <see cref="ISaveable"/> component in the loaded scenes.
        /// </summary>
        /// <remarks>
        /// Interfaces cannot be searched for directly, so this walks MonoBehaviours. Only run on save
        /// and load, never per frame.
        /// </remarks>
        private static List<ISaveable> FindSaveables()
        {
            var found = new List<ISaveable>();

            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour is ISaveable saveable) found.Add(saveable);
            }

            return found;
        }
    }
}
