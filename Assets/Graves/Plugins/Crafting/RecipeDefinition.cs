using System;
using System.Collections.Generic;
using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Crafting
{
    /// <summary>One thing a recipe consumes, and how many of it.</summary>
    [Serializable]
    public class Ingredient
    {
        [Tooltip("What is consumed. Ordinary ones only - an enchanted or improved copy is never " +
                 "taken as a material.")]
        public ItemDefinition Item;

        [Tooltip("How many.")]
        [Min(1)] public int Count = 1;
    }

    /// <summary>
    /// Something that can be made, what it takes, and what comes out.
    /// </summary>
    /// <remarks>
    /// <b>A recipe is its own kind of definition rather than a field on the thing it makes.</b> The
    /// reference read for this puts the material list on the resulting item, which reads naturally
    /// and quietly forbids two ways of making the same thing - a smelted iron bar and a bar
    /// recovered by melting scrap are one item and should be two recipes. Making the recipe the
    /// authored thing costs an asset and buys that.
    ///
    /// It also means a recipe has a uid, so knowing one can be recorded in a save without naming an
    /// asset - the same shape skills use for what an entity has learned.
    ///
    /// <b>There is no currency, no station and no time here.</b> A cost in coin needs an economy, a
    /// requirement to stand at a forge needs a world, and a progress bar needs somebody watching. All
    /// three are decisions a game makes, and a framework that assumed any of them would be wrong for
    /// the games that do it differently. What is here is the part every version needs: what goes in,
    /// what comes out.
    /// </remarks>
    [DefinitionCategory("Recipes", Order = 50)]
    [CreateAssetMenu(fileName = "Recipe", menuName = "Graves/Recipe Definition")]
    public class RecipeDefinition : GameDefinition
    {
        [SerializeField]
        [Tooltip("What this consumes. An empty list is a recipe that makes something from nothing, " +
                 "which is legal and is almost always a mistake.")]
        private List<Ingredient> _ingredients = new List<Ingredient>();

        [SerializeField]
        [Tooltip("What is produced.")]
        private ItemDefinition _result;

        [SerializeField]
        [Tooltip("How many are produced.")]
        [Min(1)] private int _resultCount = 1;

        /// <summary>What this consumes.</summary>
        public IReadOnlyList<Ingredient> Ingredients => _ingredients;

        /// <summary>What is produced. Null is a configuration error and is reported when crafting.</summary>
        public ItemDefinition Result => _result;

        /// <summary>How many are produced.</summary>
        public int ResultCount => Mathf.Max(1, _resultCount);
    }
}
