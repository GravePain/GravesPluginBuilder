using System;
using System.Collections.Generic;
using UnityEngine;

namespace Graves.Core
{
    /// <summary>
    /// One actual thing: this sword, that goblin. A definition says what something is; an instance is
    /// a particular one of them, with its own identity and its own state.
    /// </summary>
    /// <remarks>
    /// <b>Why the core owns this.</b> Two swords are one definition and two objects, and everything
    /// that outlives a session — a save file, an inventory, a chest — is full of instances rather
    /// than definitions. Identity is the core's job, and an instance is where identity is actually
    /// tested.
    ///
    /// <b>Two identities, and both are needed.</b> <see cref="DefinitionUid"/> says WHAT this is and
    /// is shared by every sword of that kind. <see cref="InstanceId"/> says WHICH ONE, and is unique
    /// to this object for as long as it exists. A save that records only the first cannot tell your
    /// enchanted sword from the one in the shop.
    ///
    /// <b>The definition is stored as a uid, never as a reference.</b> That is the whole lesson of the
    /// previous project, where authored content persisted object references that silently resolved to
    /// unrelated things after a restart — a boot texture, a bag mesh, an arm bone. The resolved
    /// definition here is cached in a field marked <c>[NonSerialized]</c> so it can never be written
    /// to disk, only ever recomputed from the uid.
    ///
    /// <b>An instance carries no game concepts.</b> No stack count, no durability, no equipped flag.
    /// Every one of those is a decision a particular game makes, and they arrive as
    /// <see cref="InstanceState"/> from a plugin.
    ///
    /// <b>It is deliberately dumb.</b> Nothing here raises a notification when state changes. The
    /// moment it did, the core would need a messaging system and the dependency would invert. Change
    /// notification can be added when an event mechanism exists, without disturbing anything written
    /// against this.
    /// </remarks>
    [Serializable]
    public class GameInstance
    {
        [SerializeField]
        [Tooltip("What this is. The definition's permanent uid - never an object reference.")]
        private string _definitionUid;

        [SerializeField]
        [Tooltip("Which one this is. Unique to this object, minted on creation.")]
        private string _instanceId;

        [SerializeReference]
        [Tooltip("Per-object state supplied by plugins. The core stores these without knowing what " +
                 "any of them are.")]
        private List<InstanceState> _state = new List<InstanceState>();

        [NonSerialized]
        private GameDefinition _resolved;

        [NonSerialized]
        private bool _resolveAttempted;

        /// <summary>
        /// True when nothing about this particular one tells it apart from another of its kind.
        /// </summary>
        /// <remarks>
        /// <b>The question is "are these two the same thing?", and it belongs to the core because the
        /// core owns identity.</b> It lived inside the inventory plugin, as one line of its stacking
        /// rule, and everything else that needed it had to depend on inventory to ask: crafting does
        /// exactly that today, to refuse an enchanted sword as material. A shop deciding a price, a
        /// trade window, a quest counting ten identical pelts all want the same answer and none of
        /// them are about bags.
        ///
        /// <b>Any state at all makes an instance particular.</b> Not a list of interesting state
        /// types, not a flag an author sets — the presence of state IS the difference. A sword with
        /// durability has been used; one with an enchantment has been worked on; either way somebody's
        /// history is attached and merging it into a pile destroys that. Enumerating which state
        /// counts would mean every new plugin has to remember to add itself to the list, and the one
        /// that forgets produces an item that silently merges away.
        ///
        /// The cost is that state which genuinely does not distinguish anything still blocks merging.
        /// That is the safe direction: the failure is a fuller bag, not a lost item.
        /// </remarks>
        public bool IsPlain => _state == null || _state.Count == 0;

        /// <summary>Parameterless constructor, for serialization. Produces an instance with no identity.</summary>
        public GameInstance() { }

        /// <summary>Creates an instance of a definition, minting a fresh instance id.</summary>
        public GameInstance(GameDefinition _definition)
            : this(_definition, Guid.NewGuid().ToString("N")) { }

        /// <summary>
        /// Creates an instance of a definition with a specific identity.
        /// </summary>
        /// <remarks>
        /// For anything whose identity is decided elsewhere: an entity placed in a scene and given a
        /// persistent id at author time, or an instance being rebuilt from a save file. Passing an
        /// empty id mints a fresh one rather than leaving the instance anonymous, because an instance
        /// with no identity cannot be saved and would fail later and further away.
        /// </remarks>
        public GameInstance(GameDefinition _definition, string _identity)
        {
            _definitionUid = _definition != null ? _definition.Uid : null;
            _instanceId = string.IsNullOrEmpty(_identity) ? Guid.NewGuid().ToString("N") : _identity;
            _resolved = _definition;
            _resolveAttempted = _definition != null;
        }

        /// <summary>The uid of the definition this is an instance of.</summary>
        public string DefinitionUid => _definitionUid;

        /// <summary>This object's own identity, unique among instances.</summary>
        public string InstanceId => _instanceId;

        /// <summary>The per-object state attached by plugins.</summary>
        public IReadOnlyList<InstanceState> State => _state;

        /// <summary>
        /// The definition this is an instance of, or null when the uid no longer resolves.
        /// </summary>
        /// <remarks>
        /// A null return is meaningful and must not be papered over: it means the definition was
        /// deleted, and the correct response is to report a broken instance rather than substitute
        /// something plausible. The lookup goes through the project registry, is attempted once, and
        /// the result is cached either way so a broken instance does not re-search on every access.
        /// </remarks>
        public GameDefinition Definition
        {
            get
            {
                if (_resolveAttempted) return _resolved;
                _resolveAttempted = true;

                var settings = CoreSettings.Instance;
                var registry = settings != null ? settings.Registry : null;
                _resolved = registry != null ? registry.Resolve(_definitionUid) : null;
                return _resolved;
            }
        }

        /// <summary>The definition cast to <typeparamref name="T"/>, or null.</summary>
        public T DefinitionAs<T>() where T : GameDefinition => Definition as T;

        /// <summary>True when this instance still names a definition that exists.</summary>
        public bool IsValid => !string.IsNullOrEmpty(_instanceId) && Definition != null;

        /// <summary>The first state of type <typeparamref name="T"/>, or null when absent.</summary>
        public T GetState<T>() where T : InstanceState
        {
            for (int i = 0; i < _state.Count; i++)
            {
                if (_state[i] is T match) return match;
            }
            return null;
        }

        /// <summary>True when state of type <typeparamref name="T"/> is present.</summary>
        public bool HasState<T>() where T : InstanceState => GetState<T>() != null;

        /// <summary>
        /// Returns the state of type <typeparamref name="T"/>, adding it if this instance has none.
        /// </summary>
        /// <remarks>
        /// The usual way a plugin reaches its own state: it does not need to know whether the
        /// instance was freshly created or loaded from a save written before the plugin existed.
        /// </remarks>
        public T GetOrAddState<T>() where T : InstanceState, new()
        {
            var existing = GetState<T>();
            if (existing != null) return existing;

            var created = new T();
            _state.Add(created);
            return created;
        }

        /// <summary>
        /// The state with a given key, or null when this instance carries none.
        /// </summary>
        /// <remarks>
        /// <b>An instance can only hold one state of any given type, and that is a real limit.</b>
        /// The typed lookup returns the first match, so an entity carrying a bag and a purse - both
        /// inventories - gets whichever was added first, and which one that is depends on the order
        /// modules happen to sit in. Two containers of the same kind is an ordinary requirement: a
        /// bag and a purse, a bank, a stash.
        ///
        /// <b>The key was already there and was already unused.</b> Every state declares one, the
        /// save file writes it, and nothing has ever read it back - the reviewer recorded exactly
        /// that as an open question about whether the key is an address or only a label. This
        /// settles it in the direction of an address, which is what the save contract always said it
        /// was.
        ///
        /// A plugin that only ever has one state of a type never needs this and should keep using
        /// the typed lookup, which is shorter and cannot be misspelled.
        /// </remarks>
        public T GetState<T>(string _key) where T : InstanceState
        {
            if (string.IsNullOrEmpty(_key)) return null;

            for (int i = 0; i < _state.Count; i++)
            {
                if (_state[i] is T match && match.StateKey == _key) return match;
            }
            return null;
        }

        /// <summary>
        /// The state with a given key, adding one if this instance has none.
        /// </summary>
        /// <remarks>
        /// The created state must already declare the key being asked for. A type whose key does not
        /// match is a mistake rather than something to paper over, because the result would be a
        /// state that can be created and never found again.
        /// </remarks>
        public T GetOrAddState<T>(string _key) where T : InstanceState, new()
        {
            var existing = GetState<T>(_key);
            if (existing != null) return existing;

            var created = new T();
            if (created.StateKey != _key)
            {
                Debug.LogError("[GameInstance] " + typeof(T).Name + " declares the key '" +
                               created.StateKey + "' but was asked for '" + _key +
                               "'. Nothing was added, because it could never have been found again.");
                return null;
            }

            _state.Add(created);
            return created;
        }

        /// <summary>Attaches state. Ignored when null.</summary>
        public void AddState(InstanceState _stateToAdd)
        {
            if (_stateToAdd == null || _state.Contains(_stateToAdd)) return;
            _state.Add(_stateToAdd);
        }

        /// <summary>Detaches state.</summary>
        public bool RemoveState(InstanceState _stateToRemove) => _state.Remove(_stateToRemove);

        /// <summary>
        /// Mints an instance id if this one has none, and clears any cached resolution.
        /// </summary>
        /// <remarks>
        /// Needed after deserialization, and after copying an instance. <b>A copy must be given a new
        /// id</b> — two objects sharing an instance id is the same class of failure as two definitions
        /// sharing a uid, and it is why the editor's Duplicate mints a fresh uid rather than copying
        /// the asset as-is.
        /// </remarks>
        public void EnsureIdentity()
        {
            if (string.IsNullOrEmpty(_instanceId)) _instanceId = Guid.NewGuid().ToString("N");
            _resolveAttempted = false;
            _resolved = null;
        }

        /// <summary>
        /// Copies this instance, giving the copy a new identity but the same definition and a
        /// shallow-copied state list.
        /// </summary>
        /// <remarks>
        /// The state objects themselves are NOT cloned, because the core cannot know how — a plugin's
        /// state may hold anything. A plugin whose state must be deep-copied should do that itself.
        /// Called out rather than silently shallow-copying, because shared mutable state between two
        /// instances is a bug that is very hard to see.
        /// </remarks>
        public GameInstance ShallowCopy()
        {
            var copy = new GameInstance
            {
                _definitionUid = _definitionUid,
                _instanceId = Guid.NewGuid().ToString("N"),
                _state = new List<InstanceState>(_state)
            };
            return copy;
        }

        /// <summary>A short description for logs and inspectors.</summary>
        public override string ToString()
        {
            var definition = Definition;
            var name = definition != null ? definition.DisplayName : "<unresolved " + _definitionUid + ">";
            var id = string.IsNullOrEmpty(_instanceId) ? "<no id>" : _instanceId.Substring(0, 8);
            return name + " [" + id + "]";
        }
    }
}
