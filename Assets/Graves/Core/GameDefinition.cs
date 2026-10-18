using System.Collections.Generic;
using UnityEngine;

namespace Graves.Core
{
    /// <summary>
    /// Base class for everything the game is authored from. A definition is pure data with a stable
    /// identity and a list of modules; it describes WHAT a thing is and never what it does.
    /// </summary>
    /// <remarks>
    /// Identity is the reason this class exists. <see cref="Uid"/> is minted once, on creation, and
    /// never changes again - not when the asset is renamed, moved, or re-authored. Everything that
    /// outlives a session (a save file, a cross-definition reference) points at the uid, so renaming
    /// "Goblin" to "Goblin Scout" cannot silently break anything.
    /// </remarks>
    public abstract class GameDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector]
        private string _uid;

        [SerializeField]
        [Tooltip("The name shown in the editor and, if a plugin chooses, in game. Free to change - " +
                 "identity lives in the uid, not here.")]
        private string _displayName;

        [SerializeField]
        [Tooltip("Data added by plugins. The core stores these without knowing what any of them are.")]
        private List<DefinitionModule> _modules = new List<DefinitionModule>();

        /// <summary>Permanent identity. Minted once and never reassigned.</summary>
        public string Uid => _uid;

        /// <summary>Human-facing name. Falls back to the asset name when left blank.</summary>
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;

        /// <summary>The modules attached to this definition, in author order.</summary>
        public IReadOnlyList<DefinitionModule> Modules => _modules;

        /// <summary>
        /// The first module of type <typeparamref name="T"/>, or null when there is none.
        /// </summary>
        public T GetModule<T>() where T : DefinitionModule
        {
            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i] is T match) return match;
            }
            return null;
        }

        /// <summary>True when a module of type <typeparamref name="T"/> is present.</summary>
        public bool HasModule<T>() where T : DefinitionModule => GetModule<T>() != null;

        /// <summary>
        /// Attaches a module. Intended for editor tooling; a module is normally a sub-asset of this
        /// definition, and adding one here does not create that sub-asset.
        /// </summary>
        public void AddModule(DefinitionModule _module)
        {
            if (_module == null || _modules.Contains(_module)) return;
            _modules.Add(_module);
        }

        /// <summary>Detaches a module. Intended for editor tooling.</summary>
        public bool RemoveModule(DefinitionModule _module) => _modules.Remove(_module);

        /// <summary>
        /// Mints the uid if this definition does not have one yet. Safe to call repeatedly - it only
        /// ever acts on a definition with no identity, so an existing uid can never be replaced.
        /// </summary>
        /// <returns>True when a uid was minted by this call.</returns>
        public bool EnsureUid()
        {
            if (!string.IsNullOrEmpty(_uid)) return false;
            _uid = System.Guid.NewGuid().ToString("N");
            return true;
        }

        private void OnValidate() => EnsureUid();
        private void Reset() => EnsureUid();
    }
}
