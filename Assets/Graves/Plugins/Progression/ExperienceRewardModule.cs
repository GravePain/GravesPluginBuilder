using Graves.Core;
using UnityEngine;
using Graves.Core;

namespace Graves.Plugins.Progression
{
    /// <summary>
    /// Says what killing this entity is worth.
    /// </summary>
    /// <remarks>
    /// Separate from the progression module because the two are independent. A monster pays
    /// experience and has none of its own; a player has levels and is worth nothing to kill. Only a
    /// thing that both levels up and rewards its killer carries both, which is a duelling opponent
    /// and not the common case.
    /// </remarks>
    [CreateAssetMenu(fileName = "ExperienceRewardModule", menuName = "Graves/Modules/Experience Reward")]
    public class ExperienceRewardModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("Experience paid to whatever kills this. The second field raises the payout for " +
                 "each level this thing has, so a veteran of the same kind is worth more.")]
        private LevelValue _reward = new LevelValue { Base = 50f, PerLevel = 25f };

        /// <summary>What killing this pays, before the victim's level is applied.</summary>
        public LevelValue Reward => _reward;

        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var host = _entity.gameObject;

            var mortal = host.GetComponent<IMortal>();
            if (mortal == null)
            {
                Debug.LogError("[Progression] '" + _entity.name + "' is worth experience but has no " +
                               "vitals, so it can never be killed and can never pay out.", _entity);
                return;
            }

            var reward = host.GetComponent<ExperienceReward>();
            if (reward == null) reward = host.AddComponent<ExperienceReward>();

            reward.Initialise(mortal, _reward);
        }
    }
}
