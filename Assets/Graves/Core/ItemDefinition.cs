using UnityEngine;

namespace Graves.Core
{
    /// <summary>
    /// A thing that can be owned, carried, traded or used. Like an entity, an item is data plus
    /// modules - the core gives it identity and a name, and nothing else.
    /// </summary>
    /// <remarks>
    /// There is no stack size, weight, rarity or value here on purpose. Every one of those is a design
    /// decision belonging to a game, and each would be a module a plugin supplies. The core's job is
    /// to make sure the item is still the same item six months and one save file later.
    /// </remarks>
    [DefinitionCategory("Items", Order = 20)]
    [CreateAssetMenu(fileName = "Item", menuName = "Graves/Item Definition")]
    public class ItemDefinition : GameDefinition
    {
    }
}
