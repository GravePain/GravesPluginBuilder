using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Currency
{
    /// <summary>
    /// Marks an item as money, so it goes to the purse rather than into the bag.
    /// </summary>
    /// <remarks>
    /// The only thing this changes is which container the item belongs in. A coin still stacks,
    /// still saves, still counts, and is still consumed by a recipe exactly as any other ingredient
    /// - because it is one.
    ///
    /// <b>The stack limit should be enormous.</b> A purse has no slot limit, but a coin still obeys
    /// its own stack size, so a maximum of ninety-nine would produce a hundred entries for ten
    /// thousand gold. Set it high enough that money is always one entry.
    /// </remarks>
    [CreateAssetMenu(fileName = "CurrencyModule", menuName = "Graves/Modules/Currency")]
    public class CurrencyModule : DefinitionModule
    {
    }
}
