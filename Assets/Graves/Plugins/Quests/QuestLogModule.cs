using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Quests
{
    /// <summary>
    /// Gives an entity a quest log, and optionally some quests to start with.
    /// </summary>
    /// <remarks>
    /// <b>Starting quests are a stand-in and are labelled as one.</b> Quests should arrive from a
    /// person who asks for something, and nothing in this framework can hold a conversation yet.
    /// Handing them out at spawn is honest about being temporary: when dialogue exists it calls
    /// <see cref="QuestLog.Accept"/>, which is the same door this uses, and this field becomes a
    /// convenience for testing rather than the only way in.
    ///
    /// <b>Handing out a quest again is not an error and does not reset it.</b> This runs on every
    /// spawn, which includes every load, and the log refuses a quest it already holds.
    /// </remarks>
    [CreateAssetMenu(fileName = "QuestLogModule", menuName = "Graves/Modules/Quest Log")]
    public class QuestLogModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("Quests this entity begins with. A stand-in until quests can be offered by " +
                 "somebody. Handing one out twice does nothing.")]
        private QuestDefinition[] _startingQuests = new QuestDefinition[0];

        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var instance = _entity.Instance;
            if (instance == null)
            {
                Debug.LogError("[Quests] '" + _entity.name + "' has no instance, so its quest " +
                               "progress could never be saved.", _entity);
                return;
            }

            var host = _entity.gameObject;

            var log = host.GetComponent<QuestLog>();
            if (log == null) log = host.AddComponent<QuestLog>();

            // Bound before any quest is offered, because accepting writes into the state and a log
            // with nowhere to write refuses the quest outright.
            log.Bind(_entity, instance.GetOrAddState<QuestLogState>());

            for (int i = 0; i < _startingQuests.Length; i++)
            {
                var quest = _startingQuests[i];
                if (quest == null) continue;

                log.Accept(quest);
            }
        }
    }
}
