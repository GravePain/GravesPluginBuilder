using Graves.Core;
using UnityEngine;
using Graves.Core;

namespace Graves.Plugins.Progression
{
    /// <summary>
    /// Gives an entity levels: where it starts, how far it can go, and what each step costs.
    /// </summary>
    /// <remarks>
    /// <b>Adding this to a definition retunes every level-scaled value it already had.</b> Health
    /// and damage were authored as a base plus a per-level bonus and have been evaluating at level
    /// one because nothing told them otherwise. They now read this, with no edit to either.
    ///
    /// That is the payoff for pulling rather than pushing, and it is worth noticing: a plugin written
    /// before progression existed gained progression by having a component added next to it.
    /// </remarks>
    [CreateAssetMenu(fileName = "ProgressionModule", menuName = "Graves/Modules/Progression")]
    public class ProgressionModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("The level this thing starts at when nothing has been saved for it.")]
        [Min(1)] private int _startingLevel = 1;

        [SerializeField]
        [Tooltip("The highest level it can reach. Experience stops being banked here.")]
        [Min(1)] private int _maxLevel = 20;

        [SerializeField]
        [Tooltip("Experience needed to leave the current level. The second field makes each level " +
                 "cost more than the last, which is what stops late levels arriving in seconds.")]
        private LevelValue _experienceCost = new LevelValue { Base = 100f, PerLevel = 50f };

        /// <summary>Where this thing starts.</summary>
        public int StartingLevel => _startingLevel;

        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var host = _entity.gameObject;

            var level = host.GetComponent<Level>();
            if (level == null) level = host.AddComponent<Level>();

            level.Configure(_experienceCost, _maxLevel, _startingLevel);

            var instance = _entity.Instance;
            if (instance == null)
            {
                Debug.LogError("[Progression] '" + _entity.name + "' has no instance, so its level " +
                               "cannot be saved or restored.", _entity);
                return;
            }

            // Bound after configuring, so a restored level is clamped against the real maximum
            // rather than against a default.
            level.Bind(instance.GetOrAddState<LevelState>(), _startingLevel);

            // Anything that reads a level and attached before this one cached the answer "there is
            // no level here", because there was not one yet. Told to look again.
            //
            // This is not tidiness. Without it, damage scaled with level and maximum health did not,
            // because combat happened to be read after progression attached and vitals was read
            // before. Half the stats responding to a level-up reads as a balance problem rather than
            // a bug, which is the worst way for it to present.
            var consumers = host.GetComponents<ILevelConsumer>();
            for (int i = 0; i < consumers.Length; i++)
            {
                if (consumers[i] != null) consumers[i].RefreshLevelSource();
            }

            // The top-up that used to sit here has moved into Health.RefreshLevelSource, called by
            // the loop above. It was never right here: it asked HealthState.Initialised whether the
            // entity was freshly spawned, and binding an uninitialised state sets that flag, so the
            // answer was always "restored" and the branch could not run. It also only worked if
            // vitals happened to attach first - the other order found no health at all.
            //
            // Health knows both things this module was guessing at: whether its state arrived from a
            // save, and what its maximum became once a level source appeared. Left to it.
        }
    }
}
