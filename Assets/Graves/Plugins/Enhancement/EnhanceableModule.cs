using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Enhancement
{
    /// <summary>
    /// Marks an item as improvable, and says how far.
    /// </summary>
    /// <remarks>
    /// An item without one cannot be enhanced at all, which is the right default: most things in a
    /// world are not improvable, and a framework that let anything be upgraded would be deciding a
    /// game's economy for it.
    ///
    /// <b>It carries no cost.</b> What enhancement demands - materials, currency, a forge, a risk of
    /// destroying the item - is an economy, and an economy is a game's decision. This says only
    /// whether the thing can be improved and how far, which is a property of the thing.
    /// </remarks>
    [CreateAssetMenu(fileName = "EnhanceableModule", menuName = "Graves/Modules/Enhanceable")]
    public class EnhanceableModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("How many times this may be improved. Beyond this the item is at its maximum.")]
        [Min(1)] private int _maxLevel = 10;

        /// <summary>The highest enhancement level this item may reach.</summary>
        public int MaxLevel => Mathf.Max(1, _maxLevel);
    }
}
