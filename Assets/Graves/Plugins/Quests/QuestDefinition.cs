using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Quests
{
    /// <summary>
    /// Something asked of a player: kill ten of a thing, and be paid for it.
    /// </summary>
    /// <remarks>
    /// <b>One kind of objective, and that is deliberate rather than a first step.</b> Fetch this,
    /// speak to them, reach there, survive that - every one of those is a real objective and none of
    /// them can be designed honestly before something needs it. Counting kills is the objective this
    /// game asked for, and it is built. When a second kind arrives, the shape of the first will be
    /// known rather than guessed, which is a better position than a general objective system nothing
    /// has exercised.
    ///
    /// <b>The reward is not here.</b> A quest paying items needs an inventory and one paying
    /// experience needs progression, and a quest plugin that knew about both would force both on
    /// every game with quests. Rewards are modules implementing an interface, living in whichever
    /// assembly owns the thing being given.
    /// </remarks>
    [DefinitionCategory("Quests", Order = 70)]
    [CreateAssetMenu(fileName = "Quest", menuName = "Graves/Quest Definition")]
    public class QuestDefinition : GameDefinition
    {
        [SerializeField]
        [Tooltip("What the player is told to do.")]
        [TextArea(1, 4)] private string _description;

        [SerializeField]
        [Tooltip("What must be killed.")]
        private EntityDefinition _target;

        [SerializeField]
        [Tooltip("How many.")]
        [Min(1)] private int _required = 10;

        [SerializeField]
        [Tooltip("Pay the reward the moment the count is reached. Turn this off for a quest that " +
                 "must be handed in somewhere.")]
        private bool _payOnCompletion = true;

        /// <summary>What the player is told to do.</summary>
        public string Description => _description;

        /// <summary>What must be killed. Null is a configuration error and is reported on accepting.</summary>
        public EntityDefinition Target => _target;

        /// <summary>The permanent id of the target, or null.</summary>
        public string TargetUid => _target != null ? _target.Uid : null;

        /// <summary>How many must be killed.</summary>
        public int Required => Mathf.Max(1, _required);

        /// <summary>Whether reaching the count pays the reward on the spot.</summary>
        /// <remarks>
        /// True by default, because nothing exists yet to hand a quest in to and a quest that
        /// completes and pays nothing is indistinguishable from one that is broken. When dialogue
        /// arrives it turns this off and calls the same single payment path.
        /// </remarks>
        public bool PayOnCompletion => _payOnCompletion;
    }
}
