using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Crafting.Stations
{
    /// <summary>
    /// A kind of place work gets done: a forge, an alchemy bench, a tanning rack.
    /// </summary>
    /// <remarks>
    /// <b>A kind of station, not a particular one.</b> A recipe should require "a forge", not "that
    /// forge over there" - a game will have several, in different towns, built from different props,
    /// and every one of them should serve. So the recipe names a kind and any entity declaring that
    /// kind satisfies it.
    ///
    /// <b>Authored rather than an enum</b>, for the reason equipment slots are: a framework deciding
    /// that games have forges and alchemy benches would be deciding somebody's game. It has a
    /// permanent uid, so renaming "Forge" to "Smithy" invalidates no recipe and no save.
    ///
    /// <b>One station may be several kinds.</b> A blacksmith's forge that also repairs and also
    /// smelts is one prop in the world declaring three kinds, rather than three props stacked in the
    /// same spot.
    /// </remarks>
    [DefinitionCategory("Crafting Stations", Order = 60)]
    [CreateAssetMenu(fileName = "CraftingStation", menuName = "Graves/Crafting Station Definition")]
    public class CraftingStationDefinition : GameDefinition
    {
    }
}
