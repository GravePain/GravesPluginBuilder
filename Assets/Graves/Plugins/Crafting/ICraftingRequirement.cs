using UnityEngine;

namespace Graves.Plugins.Crafting
{
    /// <summary>
    /// Something that must be true before a recipe may be made, beyond having the materials.
    /// </summary>
    /// <remarks>
    /// <b>The seam that keeps crafting ignorant of the world.</b> Standing at a forge, knowing the
    /// recipe, being a high enough level, having a licence from the guild - every one of those is a
    /// real gate and none of them belongs in the code that exchanges materials for a result.
    /// A requirement is a module on the recipe, so a plugin can invent a new kind without this
    /// assembly knowing it exists.
    ///
    /// <b>It explains itself.</b> The reason comes back as words rather than a log line, so a button
    /// can grey itself out and say "you must be at a forge". A crafting button that simply does
    /// nothing is indistinguishable from a broken one.
    /// </remarks>
    public interface ICraftingRequirement
    {
        /// <summary>True when this requirement is satisfied. When false, says why in plain words.</summary>
        bool IsMet(RecipeDefinition _recipe, GameObject _crafter, out string _reason);
    }
}
