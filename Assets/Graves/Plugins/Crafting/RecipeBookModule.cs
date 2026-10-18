using System.Collections.Generic;
using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Crafting
{
    /// <summary>
    /// Gives an entity a set of recipes it knows how to make.
    /// </summary>
    /// <remarks>
    /// <b>Knowing is deliberately not enforced by the workbench.</b> Handing the workbench a recipe
    /// makes it, whether or not anyone knows it - because a shop, a quest reward or a scripted
    /// cutscene all legitimately produce things nobody learned. What this adds is the player-facing
    /// half: which recipes are offered, and which are simply not there.
    ///
    /// Anything gating on knowledge should ask this and then ask the workbench, in that order. Two
    /// questions, two answers, and a crafting screen needs both to say the difference between "you
    /// have not learned this" and "you are short two iron".
    /// </remarks>
    [CreateAssetMenu(fileName = "RecipeBookModule", menuName = "Graves/Modules/Recipe Book")]
    public class RecipeBookModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("Recipes this kind of thing begins knowing.")]
        private List<RecipeDefinition> _startingRecipes = new List<RecipeDefinition>();

        /// <summary>What this kind of entity begins knowing.</summary>
        public IReadOnlyList<RecipeDefinition> StartingRecipes => _startingRecipes;

        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var instance = _entity.Instance;
            if (instance == null)
            {
                Debug.LogError("[Crafting] " + _entity.name + " has no instance, so what it knows how " +
                               "to make cannot be saved or restored.", _entity);
                return;
            }

            var book = instance.GetOrAddState<RecipeBookState>(RecipeBookState.Key);
            if (book == null) return;

            for (int i = 0; i < _startingRecipes.Count; i++)
            {
                var recipe = _startingRecipes[i];
                if (recipe == null)
                {
                    Debug.LogWarning("[Crafting] " + _entity.name + " has an empty starting recipe at " +
                                     "index " + i + ". Its definition was probably deleted.", _entity);
                    continue;
                }

                book.Learn(recipe.Uid);
            }
        }
    }
}
