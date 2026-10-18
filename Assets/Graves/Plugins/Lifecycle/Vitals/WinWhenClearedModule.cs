using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Lifecycle.Vitals
{
    /// <summary>
    /// Says that the game is won when nothing of a given kind is left alive.
    /// </summary>
    /// <remarks>
    /// Authored on whatever is doing the winning rather than on the thing being wiped out, because
    /// the condition belongs to the player and the enemies should not have to know they are the
    /// objective.
    /// </remarks>
    [CreateAssetMenu(fileName = "WinWhenCleared", menuName = "Graves/Modules/Win When Cleared")]
    public class WinWhenClearedModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("The kind of thing that must be gone.")]
        private EntityDefinition _kind;

        [SerializeField]
        [Tooltip("Shown when the game ends this way.")]
        private string _reason = "Nothing left standing.";

        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            if (_kind == null)
            {
                Debug.LogError("[Session] " + _entity.name + " wins when something is cleared, but " +
                               "names nothing. The game can never be won.", _entity);
                return;
            }

            var ending = _entity.GetComponent<EndWhenNoneRemain>();
            if (ending == null) ending = _entity.gameObject.AddComponent<EndWhenNoneRemain>();

            ending.Initialise(_kind, _reason);
        }
    }
}
