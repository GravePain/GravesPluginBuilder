using System.Collections.Generic;
using UnityEngine;

namespace Graves.Core
{
    /// <summary>
    /// The catalogue of every definition in the project, and the only supported way to resolve a uid
    /// back to the thing it names.
    /// </summary>
    /// <remarks>
    /// <b>Uid is the identity, and there is no numeric id.</b> An earlier sketch of this class paired
    /// each uid with a small integer for compactness. That was removed before it was written: an
    /// integer alias is a second identity to keep in step, and on a previous project recycled numeric
    /// ids were the exact mechanism by which an old save resolved to the WRONG item - silently,
    /// carrying the player's upgrades onto something else. A uid is self-describing, needs no
    /// registry to interpret, and cannot be handed to a different definition later. If compact ids are
    /// ever genuinely needed they can be added as an alias without disturbing this, because the uid
    /// stays the truth.
    ///
    /// A deleted definition leaves a hole. That is the intended behaviour: a save referring to it
    /// fails to resolve and can be reported, rather than quietly resolving to whatever was authored
    /// next.
    /// </remarks>
    [CreateAssetMenu(fileName = "DefinitionRegistry", menuName = "Graves/Definition Registry")]
    public class DefinitionRegistry : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Every definition in the project. Rebuilt by the authoring window; not hand-edited.")]
        private List<GameDefinition> _definitions = new List<GameDefinition>();

        private Dictionary<string, GameDefinition> _byUid;

        /// <summary>Every registered definition.</summary>
        public IReadOnlyList<GameDefinition> Definitions => _definitions;

        /// <summary>
        /// Resolves a uid, or returns null when nothing in the project claims it.
        /// </summary>
        /// <remarks>
        /// A null return is meaningful: it means the definition was deleted or never registered, and
        /// callers should surface that rather than substitute something. See the class remarks.
        /// </remarks>
        public GameDefinition Resolve(string _uid)
        {
            if (string.IsNullOrEmpty(_uid)) return null;
            EnsureIndex();
            return _byUid.TryGetValue(_uid, out var found) ? found : null;
        }

        /// <summary>Resolves a uid and casts, returning null when absent or of another type.</summary>
        public T Resolve<T>(string _uid) where T : GameDefinition => Resolve(_uid) as T;

        /// <summary>
        /// Replaces the catalogue wholesale. Called by the authoring window after a rescan; the index
        /// is dropped so the next lookup rebuilds it.
        /// </summary>
        public void SetContents(IEnumerable<GameDefinition> _definitionsFound)
        {
            _definitions.Clear();
            if (_definitionsFound != null)
            {
                foreach (var definition in _definitionsFound)
                {
                    if (definition != null) _definitions.Add(definition);
                }
            }
            _byUid = null;
        }

        /// <summary>Forces the lookup index to rebuild on next use.</summary>
        public void Invalidate() => _byUid = null;

        /// <summary>
        /// Reports every integrity problem in the catalogue: missing assets, definitions with no uid,
        /// and - the one that matters - two definitions claiming the same uid.
        /// </summary>
        /// <returns>One human-readable line per problem. Empty when the catalogue is sound.</returns>
        public List<string> Validate()
        {
            var problems = new List<string>();
            var seen = new Dictionary<string, GameDefinition>();

            for (int i = 0; i < _definitions.Count; i++)
            {
                var definition = _definitions[i];

                if (definition == null)
                {
                    problems.Add("Entry " + i + " is a missing asset. It was deleted without the " +
                                 "registry being rebuilt.");
                    continue;
                }

                if (string.IsNullOrEmpty(definition.Uid))
                {
                    problems.Add("'" + definition.name + "' has no uid. It cannot be saved or " +
                                 "referenced until one is minted.");
                    continue;
                }

                if (seen.TryGetValue(definition.Uid, out var other))
                {
                    problems.Add("'" + definition.name + "' and '" + other.name + "' share the uid '" +
                                 definition.Uid + "'. One was almost certainly duplicated on disk " +
                                 "rather than through the authoring window.");
                    continue;
                }

                seen.Add(definition.Uid, definition);

                // A missing module means an uninstalled plugin, and it is REPORTED rather than
                // cleaned up. The authored data is still in the file, intact, and stays recoverable
                // for exactly as long as nobody writes the definition back out - a save while the
                // module is unresolvable replaces the reference with nothing and makes the loss
                // permanent. Verified 2026-09-05 by removing a plugin: the asset was untouched on
                // disk, but the editor held the broken binding for the rest of the session.
                for (int m = 0; m < definition.Modules.Count; m++)
                {
                    if (definition.Modules[m] != null) continue;

                    problems.Add("'" + definition.name + "' has an unresolvable module at index " + m +
                                 ". Its plugin is not installed. The authored data is still in the " +
                                 "asset file - install the plugin and restart the editor to get it " +
                                 "back. Do NOT re-save this definition first, or the reference is " +
                                 "lost for good.");
                }
            }

            return problems;
        }

        private void EnsureIndex()
        {
            if (_byUid != null) return;

            _byUid = new Dictionary<string, GameDefinition>(_definitions.Count);
            for (int i = 0; i < _definitions.Count; i++)
            {
                var definition = _definitions[i];
                if (definition == null || string.IsNullOrEmpty(definition.Uid)) continue;
                _byUid[definition.Uid] = definition;
            }
        }
    }
}
