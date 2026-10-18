using Graves.Core;
using Graves.Plugins.Inventory;
using Graves.Plugins.Quests;
using UnityEngine;

namespace Graves.Plugins.Quests.Inventory
{
    /// <summary>
    /// A quest reward paid in items: a hundred gold, three potions, a sword.
    /// </summary>
    /// <remarks>
    /// <b>Gold needs no special case here, and that is the currency decision paying off.</b> Money is
    /// an item and a purse is an inventory with no capacity, so "a hundred gold" and "three potions"
    /// are the same reward with different fields. A framework that had made currency its own system
    /// would need a second reward type, a second authoring surface and a second payment path, all
    /// doing structurally identical work.
    ///
    /// <b>The container is named by key rather than chosen by type.</b> A bag and a purse are both
    /// inventories, so asking by type alone would hand back whichever was added first - and paying a
    /// hundred gold into the bag would silently fill four slots the player wanted for loot.
    ///
    /// <b>This assembly does not reference the currency plugin.</b> It does not need to: the purse is
    /// a subclass of an ordinary inventory and its key is just a string. So a game with money and a
    /// game without both use this reward, and neither drags the other's plugin in.
    ///
    /// <b>It never creates the container.</b> A reward that conjured a purse onto anything that
    /// completed a quest would be inventing storage the entity was never authored to have, and the
    /// real mistake - a quest paying gold to something with no purse - would be hidden by it.
    /// </remarks>
    [CreateAssetMenu(fileName = "ItemRewardModule", menuName = "Graves/Modules/Quest Reward - Items")]
    public class ItemRewardModule : QuestRewardModule
    {
        [SerializeField]
        [Tooltip("What is handed over.")]
        private ItemDefinition _item;

        [SerializeField]
        [Tooltip("How many.")]
        [Min(1)] private int _count = 1;

        [SerializeField]
        [Tooltip("Which container it goes into. 'inventory' is the ordinary bag; 'graves.purse' is " +
                 "the purse, which has no capacity and is where money belongs.")]
        private string _container = InventoryState.Key;

        /// <inheritdoc />
        public override string Describe()
        {
            if (_item == null) return "(a reward with no item set)";
            return _count + " " + _item.DisplayName;
        }

        /// <inheritdoc />
        public override bool Pay(GameEntity _recipient)
        {
            if (_recipient == null) return false;

            if (_item == null)
            {
                Debug.LogError("[Quests] A quest reward has no item set, so it can never be paid.", this);
                return false;
            }

            var instance = _recipient.Instance;
            if (instance == null) return false;

            string key = string.IsNullOrWhiteSpace(_container) ? InventoryState.Key : _container;

            var container = instance.GetState<InventoryState>(key);
            if (container == null)
            {
                Debug.LogError("[Quests] '" + _recipient.name + "' was owed " + Describe() +
                               " but has no container with the key '" + key +
                               "'. The reward was not paid and the quest stays unclaimed.", _recipient);
                return false;
            }

            if (!container.HasRoomFor(_item, _count))
            {
                Debug.LogWarning("[Quests] '" + _recipient.name + "' has no room for " + Describe() +
                                 ". The quest stays unclaimed, so the reward can be collected later.",
                                 _recipient);
                return false;
            }

            // A fresh instance per payment, with its own identity. Handing out one shared instance
            // would give every player who completed the quest the same object, and enhancing one
            // would enhance all of them.
            int added = container.Add(new GameInstance(_item), _count);
            if (added >= _count) return true;

            // Reported rather than accepted as a partial payment. The room check just passed, so
            // adding less than asked for means the container disagrees with its own answer, and that
            // is a defect in the inventory rather than a full bag.
            Debug.LogError("[Quests] '" + _recipient.name + "' had room for " + Describe() +
                           " but only " + added + " went in. The quest stays unclaimed.", _recipient);
            return false;
        }
    }
}
