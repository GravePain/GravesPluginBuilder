using System.Collections.Generic;
using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Crafting.Stations
{
    /// <summary>
    /// Makes an entity a place where work gets done.
    /// </summary>
    /// <remarks>
    /// Put this on a forge, an anvil, a cauldron, or on a travelling blacksmith who is a station that
    /// walks. Nothing about a station requires it to stand still, which is why this is a module on an
    /// entity rather than a fixed marker in a scene.
    /// </remarks>
    [CreateAssetMenu(fileName = "CraftingStationModule", menuName = "Graves/Modules/Crafting Station")]
    public class CraftingStationModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("The kinds of work that can be done here. A forge that also repairs declares both.")]
        private List<CraftingStationDefinition> _kinds = new List<CraftingStationDefinition>();

        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var station = _entity.GetComponent<CraftingStation>();
            if (station == null) station = _entity.gameObject.AddComponent<CraftingStation>();

            station.Configure(_kinds);

            if (_kinds.Count == 0)
            {
                Debug.LogWarning("[Crafting] " + _entity.name + " is a crafting station that supports " +
                                 "no kind of work, so no recipe will ever accept it.", _entity);
            }
        }
    }
}
