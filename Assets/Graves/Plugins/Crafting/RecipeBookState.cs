using System;
using System.Collections.Generic;
using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Crafting
{
    /// <summary>
    /// Which recipes this particular thing knows how to make.
    /// </summary>
    /// <remarks>
    /// <b>Recipes are stored by uid, never by reference.</b> A save pointing at an asset is the
    /// failure this framework was founded on. A uid resolves through the registry, and a recipe whose
    /// definition has been deleted comes back unresolvable and can be reported rather than silently
    /// becoming a different recipe.
    ///
    /// <b>Knowing a recipe is separate from being able to make it.</b> This says what is in your
    /// head; materials, money and standing at a forge are asked separately and answered by the
    /// workbench. A crafting screen shows what you know and greys out what you cannot currently do,
    /// which is two different questions and needs both answers.
    /// </remarks>
    [Serializable]
    public class RecipeBookState : InstanceState
    {
        [SerializeField]
        [Tooltip("The permanent ids of every recipe this thing has learned.")]
        private List<string> _known = new List<string>();

        /// <summary>Every recipe known, by uid.</summary>
        public IReadOnlyList<string> Known => _known;

        /// <summary>The key a recipe book is stored under.</summary>
        public const string Key = "graves.recipes";

        /// <inheritdoc />
        public override string StateKey => Key;

        /// <summary>True when this recipe is known.</summary>
        public bool Knows(string _recipeUid)
            => !string.IsNullOrEmpty(_recipeUid) && _known.Contains(_recipeUid);

        /// <summary>Learns a recipe. Returns false when it was already known.</summary>
        /// <remarks>
        /// Learning something twice is not an error and not a duplicate - it is a player finding a
        /// second copy of a book they have already read, which happens constantly and should do
        /// nothing rather than complain.
        /// </remarks>
        public bool Learn(string _recipeUid)
        {
            if (string.IsNullOrEmpty(_recipeUid) || _known.Contains(_recipeUid)) return false;

            _known.Add(_recipeUid);
            return true;
        }

        /// <summary>Forgets a recipe. Returns true when it was known.</summary>
        public bool Forget(string _recipeUid) => _known.Remove(_recipeUid);

        /// <summary>Reports any known recipe whose definition no longer exists.</summary>
        public List<string> Validate(DefinitionRegistry _registry)
        {
            var problems = new List<string>();
            if (_registry == null) return problems;

            for (int i = 0; i < _known.Count; i++)
            {
                if (_registry.Resolve(_known[i]) == null)
                    problems.Add("Knows a recipe whose definition no longer exists (" + _known[i] + ").");
            }
            return problems;
        }
    }
}
