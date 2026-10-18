using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.AI
{
    /// <summary>
    /// Says that hostile things should come after this entity.
    /// </summary>
    /// <remarks>
    /// Put this on whatever the game wants hunted. On a player definition it makes monsters
    /// dangerous; on a caravan it makes an escort mission.
    /// </remarks>
    [CreateAssetMenu(fileName = "AiTargetModule", menuName = "Graves/Modules/AI Target")]
    public class AiTargetModule : DefinitionModule
    {
        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var host = _entity.gameObject;
            if (host.GetComponent<AiTarget>() == null) host.AddComponent<AiTarget>();
        }
    }
}
