using UnityEngine;

namespace Graves.Core
{
    /// <summary>
    /// Project-wide core configuration. Loaded from <c>Resources</c> so it resolves at runtime with no
    /// scene wiring.
    /// </summary>
    /// <remarks>
    /// Everything here is a slot the core provides and the project fills. The core ships no content of
    /// its own - no model, no prefab, no clip - because bundling any would be both an asset dependency
    /// and an opinion about what a game looks like. Every slot may be left empty, and the core says
    /// what it fell back to.
    /// </remarks>
    public class CoreSettings : ScriptableObject
    {
        /// <summary>Path within a <c>Resources</c> folder where the settings asset is expected.</summary>
        public const string ResourcePath = "GravesCoreSettings";

        /// <summary>Where authored data lives when a project has not said otherwise.</summary>
        public const string DefaultDataRoot = "Assets/Graves/Data";

        [SerializeField]
        [Tooltip("The catalogue of every definition. Required for uid lookups at runtime.")]
        private DefinitionRegistry _registry;

        [SerializeField]
        [Tooltip("Prefab spawned for an entity that supplies no representation of its own. Leave " +
                 "empty and the core falls back to a primitive capsule, announcing that it did.")]
        private GameObject _defaultRepresentation;

        [SerializeField]
        [Tooltip("Base prefab for entities created at runtime. Its root object must be saved " +
                 "INACTIVE. Leave empty and a bare object is made instead, which is fine - fill it " +
                 "when every spawned entity should share a layer, a tag or a rigidbody.")]
        private GameObject _entityPrefab;

        [SerializeField]
        [Tooltip("Project folder holding authored definitions and this settings asset. Must start " +
                 "with 'Assets/'. Leave empty for the default, Assets/Graves/Data.")]
        private string _dataRoot = DefaultDataRoot;

        private static CoreSettings _instance;
        private static bool _lookedUp;

        /// <summary>The catalogue, or null when none has been assigned.</summary>
        public DefinitionRegistry Registry => _registry;

        /// <summary>Fallback body prefab. May be null.</summary>
        public GameObject DefaultRepresentation => _defaultRepresentation;

        /// <summary>
        /// Base prefab for entities made at runtime by <c>GameEntity.Create</c>. May be null.
        /// </summary>
        /// <remarks>
        /// <b>Its root must be saved inactive.</b> That is not a preference - instantiating an active
        /// root runs <c>Awake</c> before anyone can say what the entity is, and the entity correctly
        /// disables itself for having no definition. The core checks and refuses rather than shipping
        /// that as a mystery.
        ///
        /// This is a slot rather than an asset the core provides, for the same reason as the one above
        /// it: what a spawned thing should start with - which layer, which tag, whether it has a
        /// rigidbody - is a decision about a particular game.
        /// </remarks>
        public GameObject EntityPrefab => _entityPrefab;

        /// <summary>
        /// Project folder holding authored definitions and this settings asset.
        /// </summary>
        /// <remarks>
        /// <b>A field rather than a constant, because a constant inside a package cannot be changed
        /// by the project that installs it.</b> It was one, and a consuming project could not move
        /// its own content without forking the framework - which is the whole failure this framework
        /// exists to avoid, in miniature.
        ///
        /// <b>Authored data is deliberately not inside the framework's folders.</b> The core and the
        /// plugins ship; a game's definitions never do. Separate trees make that boundary something
        /// you can see rather than something to remember.
        /// </remarks>
        public string DataRoot =>
            string.IsNullOrWhiteSpace(_dataRoot) ? DefaultDataRoot : _dataRoot.TrimEnd('/');

        /// <summary>
        /// The project's settings asset, or null when there is none.
        /// </summary>
        /// <remarks>
        /// The lookup is attempted once per domain and the result cached either way, so a project with
        /// no settings asset does not pay for a failed <c>Resources.Load</c> on every access.
        /// </remarks>
        public static CoreSettings Instance
        {
            get
            {
                if (_lookedUp) return _instance;
                _lookedUp = true;
                _instance = Resources.Load<CoreSettings>(ResourcePath);
                return _instance;
            }
        }

        /// <summary>Drops the cached lookup. For editor tooling that has just created the asset.</summary>
        public static void ClearCache()
        {
            _instance = null;
            _lookedUp = false;
        }
    }
}
