using UnityEngine;

namespace Graves.Core
{
    /// <summary>
    /// A definition made live. Put this on a GameObject, give it an <see cref="EntityDefinition"/>,
    /// and on <c>Awake</c> it spawns a body and hands itself to every module the definition carries.
    /// </summary>
    /// <remarks>
    /// This is the entire runtime surface of the core, and it does three things: spawn a
    /// representation, attach modules, then stop. There is no <c>Update</c> here and there will not
    /// be one - a frame loop is behaviour, and behaviour belongs to plugins. A module that needs
    /// ticking adds its own component during <see cref="DefinitionModule.AttachTo"/>.
    /// </remarks>
    [DisallowMultipleComponent]
    public class GameEntity : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("What this entity is. Required.")]
        private EntityDefinition _definition;

        [SerializeField]
        [Tooltip("Where the body is parented. Leave empty to parent it to this object.")]
        private Transform _representationRoot;

        [SerializeField, HideInInspector]
        private string _persistentId;

        /// <summary>
        /// This scene object's identity, authored once and kept forever.
        /// </summary>
        /// <remarks>
        /// <b>Why an authored id rather than a runtime one.</b> A save has to find its way back to the
        /// same entity next session, and a GUID minted in <c>Awake</c> is different every run - so the
        /// save would load into nothing and report nothing wrong. The id has to be decided at author
        /// time and stored in the scene.
        ///
        /// <b>Duplicates are possible and are reported, not repaired.</b> Copying a scene object, or
        /// placing several instances of a prefab that carries one, gives two entities the same id.
        /// That is the same class of failure as two definitions sharing a uid, and a save system that
        /// silently picked one would restore the wrong entity's state - so it is surfaced instead.
        /// </remarks>
        public string PersistentId => _persistentId;

        /// <summary>
        /// What this entity is. Null only when unassigned, which is a configuration error.
        /// </summary>
        /// <remarks>
        /// <b>Settable, but only before the entity wakes.</b> An entity changing what it is midway
        /// through its life is a category of bug nobody wants - modules have already attached, state
        /// has already been seeded, and the save now describes a thing that no longer exists. So the
        /// setter refuses once <c>Awake</c> has run, and says so rather than half-applying.
        ///
        /// The window before waking is real and legitimate: something instantiates a prefab whose root
        /// is inactive, says what it is, and switches it on. That is what <see cref="Create"/> does,
        /// and this setter is what lets it do so without picking the lock on a private field.
        /// </remarks>
        public EntityDefinition Definition
        {
            get => _definition;
            set
            {
                if (Instance != null)
                {
                    Debug.LogError("[GameEntity] '" + name + "' has already woken as '" +
                                   (_definition != null ? _definition.DisplayName : "nothing") +
                                   "' and cannot become something else. Set the definition while the " +
                                   "object is still inactive, or spawn a new entity.", this);
                    return;
                }

                _definition = value;
            }
        }

        /// <summary>The spawned body. Never null after a successful <c>Awake</c>.</summary>
        public GameObject Representation { get; private set; }

        /// <summary>
        /// This entity as a particular thing rather than a kind of thing — its identity and its
        /// per-object state.
        /// </summary>
        /// <remarks>
        /// An entity in the world IS an instance of its definition, so it gets one, and plugins put
        /// per-entity runtime state here rather than inventing their own storage. That is what makes
        /// a goblin's remaining health persistable by the same mechanism as a sword's durability.
        ///
        /// Created fresh on <c>Awake</c>.
        ///
        /// <b>A load happens after modules have attached, not before.</b> This said the opposite -
        /// that a save plugin substitutes a loaded instance first - and no plugin has ever done that.
        /// A save finds entities that are already awake and overwrites their state in place, and at
        /// least one plugin depends on exactly that order: the inventory seeds its starting contents
        /// while attaching, and the load then overwrites both the contents and the flag saying they
        /// were seeded. Under the order this used to describe, a loaded character would be handed
        /// their starting gear on top of their saved bag, every time.
        ///
        /// <b>Modules must still read this in <c>AttachTo</c> rather than caching it earlier</b>, and
        /// must write through the state object rather than holding a copy - that is what makes the
        /// overwrite reach them.
        ///
        /// The setter is kept, and is currently used by nobody. It is the door for restoring
        /// something that has no live instance to overwrite - an entity created at runtime rather
        /// than placed in a scene. That case is open, not solved.
        /// </remarks>
        public GameInstance Instance { get; set; }

        /// <summary>
        /// Whether a save should record this entity and rebuild it next session.
        /// </summary>
        /// <remarks>
        /// <b>Only meaningful for entities created at runtime, and false by default.</b> One placed
        /// in a scene has an authored id and is always saved; one made by <see cref="Create"/> has no
        /// authored identity, because nobody authored it.
        ///
        /// <b>False is the right default and it is not laziness.</b> The things that spawn in a game
        /// are mostly scenery with legs: a spawner refills its clearing, and rebuilding the exact
        /// three grunts that were standing there is worth nothing and costs a save entry each. Left
        /// transient, they simply are not saved, the spawner refills from empty on load, and the
        /// world comes back stocked - which is what a player expects.
        ///
        /// <b>Set it true for the things that would be missed.</b> A dropped sword, a tamed animal, a
        /// character who arrived through a story beat. Those get their position and definition
        /// recorded and are rebuilt on load, carrying the identity they had.
        ///
        /// The alternative - saving everything spawned - was tried on paper and rejected: it trades a
        /// spurious "entity missing" report for a real bug, because a spawner restores its old
        /// occupants and then refills to its quota on top of them.
        /// </remarks>
        public bool PersistAcrossSessions { get; set; }

        /// <summary>Where the body was parented.</summary>
        public Transform RepresentationRoot => _representationRoot != null ? _representationRoot : transform;

        /// <summary>
        /// Makes a live entity from a definition, at a place.
        /// </summary>
        /// <remarks>
        /// <b>This exists because the ordering is a trap.</b> A GameEntity resolves everything it needs
        /// in <c>Awake</c>, and <c>Awake</c> runs the instant the component exists - so a caller that
        /// adds the component and then assigns the definition gets an entity that reported having none
        /// and disabled itself. Every plugin that ever spawns would rediscover that, and the first
        /// one that did reached past the core with reflection to work around it.
        ///
        /// <b>The prefab is what actually solves it, not this method.</b> Instantiating a prefab whose
        /// root is saved inactive does not run <c>Awake</c>, so there is an honest window in which to
        /// say what the thing is. With no prefab assigned a bare inactive host is made instead, which
        /// is the same window by a plainer route - the body comes from the definition either way, so
        /// nothing is missing. A project assigns the prefab when it wants every spawned entity to
        /// start with something in common: a layer, a tag, a rigidbody, a collider.
        /// </remarks>
        /// <param name="_what">The definition to become. Required.</param>
        /// <param name="_where">World position.</param>
        /// <param name="_facing">World rotation.</param>
        /// <param name="_parent">Optional parent. Null leaves it at the scene root.</param>
        /// <param name="_identity">
        /// An instance to adopt instead of minting a fresh one. For a save rebuilding something it
        /// recorded, so the thing comes back as the same thing rather than as a copy of it. This is
        /// the door <c>Instance</c>'s setter exists for.
        /// </param>
        /// <returns>The live entity, or null when no definition was given.</returns>
        public static GameEntity Create(EntityDefinition _what, Vector3 _where, Quaternion _facing,
                                        Transform _parent = null, GameInstance _identity = null)
        {
            if (_what == null)
            {
                Debug.LogError("[GameEntity] Create was given no definition. Nothing was spawned.");
                return null;
            }

            var settings = CoreSettings.Instance;
            var prefab = settings != null ? settings.EntityPrefab : null;

            GameObject host;
            GameEntity entity;

            if (prefab != null)
            {
                host = Instantiate(prefab, _where, _facing, _parent);
                entity = host.GetComponent<GameEntity>();

                if (entity == null)
                {
                    // Reported rather than repaired. Adding the component here would wake it on the
                    // spot with no definition, so the failure would arrive as a disabled entity
                    // instead of as the misconfigured prefab it actually is.
                    Debug.LogError("[GameEntity] The project's entity prefab '" + prefab.name +
                                   "' has no GameEntity component, so nothing can be made from it.",
                                   prefab);
                    Destroy(host);
                    return null;
                }

                if (host.activeSelf)
                {
                    // Already awake, and the definition is now unsettable. Saying which asset is at
                    // fault matters more than the symptom, because the symptom is an entity that
                    // disabled itself for no visible reason.
                    Debug.LogError("[GameEntity] The project's entity prefab '" + prefab.name +
                                   "' is saved active, so it woke before it could be told what it " +
                                   "is. Deactivate the prefab's root object.", prefab);
                    Destroy(host);
                    return null;
                }
            }
            else
            {
                host = new GameObject(_what.DisplayName);
                host.SetActive(false);
                host.transform.SetParent(_parent);
                host.transform.SetPositionAndRotation(_where, _facing);
                entity = host.AddComponent<GameEntity>();
            }

            // Cleared, not kept. A prefab carries whatever persistent id it was authored with, so
            // every entity instantiated from it would arrive claiming to be the same one - and the
            // core treats a shared id as a fault worth reporting, not a coincidence. An entity made
            // at runtime was never authored, so it has no authored identity to preserve: an empty id
            // makes GameInstance mint a fresh one.
            entity._persistentId = null;

            entity.Definition = _what;

            // Before activation, which is the only window in which it can be set: Awake mints a fresh
            // instance the moment the object switches on, and a rebuilt entity carrying a new random
            // id is a different entity as far as the next save is concerned.
            if (_identity != null) entity.Instance = _identity;

            host.name = _what.DisplayName;
            host.SetActive(true);
            return entity;
        }

        private void Awake()
        {
            if (_definition == null)
            {
                Debug.LogError("[GameEntity] '" + name + "' has no definition assigned. Nothing was " +
                               "spawned and this entity is disabled.", this);
                enabled = false;
                return;
            }

            // Before the body and before any module, so a module can rely on it existing. The
            // authored id becomes the instance id, which is what lets a save find this entity again.
            if (Instance == null) Instance = new GameInstance(_definition, _persistentId);

            Representation = SpawnRepresentation();

            var modules = _definition.Modules;
            for (int i = 0; i < modules.Count; i++)
            {
                var module = modules[i];
                if (module == null)
                {
                    Debug.LogWarning("[GameEntity] '" + _definition.DisplayName + "' has a missing " +
                                     "module at index " + i + ". Its plugin is probably not installed.",
                                     this);
                    continue;
                }

                module.AttachTo(this);
            }
        }

        /// <summary>
        /// Spawns the body, preferring the definition's own prefab, then the project default, then a
        /// primitive capsule.
        /// </summary>
        /// <remarks>
        /// Every fallback announces itself. A silent fallback is how a project ends up shipping the
        /// placeholder - the log line is the whole point, and it names which step was taken so the
        /// missing piece is obvious without opening anything.
        ///
        /// The placeholder is stripped to visuals only, and it is stripped WITHOUT naming any
        /// component type. That is not stylistic. <c>CreatePrimitive</c> attaches a collider, and
        /// writing <c>GetComponent&lt;Collider&gt;()</c> to remove it put UnityEngine.PhysicsModule
        /// into this assembly's reference list - the core declaring a physics dependency in the very
        /// line claiming it has no opinion on physical interaction. Keeping whatever renders and
        /// discarding the rest says what is meant and costs the core nothing.
        /// </remarks>
        private GameObject SpawnRepresentation()
        {
            var parent = RepresentationRoot;

            if (_definition.Representation != null)
            {
                return Instantiate(_definition.Representation, parent.position, parent.rotation, parent);
            }

            var settings = CoreSettings.Instance;
            if (settings != null && settings.DefaultRepresentation != null)
            {
                Debug.Log("[GameEntity] '" + _definition.DisplayName + "' has no representation of " +
                          "its own - using the project default '" +
                          settings.DefaultRepresentation.name + "'.", this);
                return Instantiate(settings.DefaultRepresentation, parent.position, parent.rotation, parent);
            }

            Debug.LogWarning("[GameEntity] '" + _definition.DisplayName + "' has no representation and " +
                             "the project has no default - falling back to a primitive capsule.", this);

            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = _definition.DisplayName + " (placeholder capsule)";

            var attached = capsule.GetComponents<Component>();
            for (int i = 0; i < attached.Length; i++)
            {
                var component = attached[i];
                if (component is Transform || component is MeshFilter || component is Renderer) continue;
                Destroy(component);
            }

            capsule.transform.SetParent(parent, false);
            capsule.transform.localPosition = Vector3.zero;
            capsule.transform.localRotation = Quaternion.identity;
            return capsule;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Mints the persistent id once, at author time.
        /// </summary>
        /// <remarks>
        /// Editor-only and deliberately so: minting at runtime would produce a different id every
        /// session, which is the exact failure this field exists to prevent. It is never reassigned
        /// once set, so a duplicate created by copying a scene object stays a duplicate until it is
        /// reported - which is the intended behaviour, because silently re-minting would break the
        /// save that already refers to the original.
        ///
        /// <b>No <c>EditorUtility.SetDirty</c> call.</b> It was here, and it put a reference to
        /// UnityEditor.CoreModule into the core's RUNTIME assembly - stripped from builds by the
        /// guard, and harmless, but it made the diagnostic report read as though the core depended on
        /// the editor. Unity already treats a serialized field changed inside OnValidate as part of
        /// the serialization pass, so the call bought nothing and cost the one thing the report exists
        /// to show clearly. Verified after removal: a fresh entity still gets an id and it still
        /// survives a scene save.
        /// </remarks>
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(_persistentId)) return;
            _persistentId = System.Guid.NewGuid().ToString("N");
        }

        private void Reset() => OnValidate();
#endif
    }
}
