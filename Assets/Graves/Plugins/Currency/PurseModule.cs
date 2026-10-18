using Graves.Core;
using Graves.Plugins.Inventory;
using UnityEngine;

namespace Graves.Plugins.Currency
{
    /// <summary>
    /// Gives an entity somewhere to keep money.
    /// </summary>
    /// <remarks>
    /// Separate from the inventory module because carrying things and having money are different
    /// features. A chest holds items and has no purse; a shopkeeper may have a purse and no bag
    /// worth speaking of.
    ///
    /// <b>The purse has no capacity, and that is the entire trick.</b> Everything else about it is
    /// an ordinary inventory, which is why nothing had to be written twice.
    /// </remarks>
    [CreateAssetMenu(fileName = "PurseModule", menuName = "Graves/Modules/Purse")]
    public class PurseModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("Money this entity starts with. Granted once, the first time it exists, never on a load.")]
        private StartingItem[] _startingMoney = new StartingItem[0];

        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var instance = _entity.Instance;
            if (instance == null)
            {
                Debug.LogError("[Currency] " + _entity.name + " has no instance, so its money cannot " +
                               "be saved or restored.", _entity);
                return;
            }

            // By key, not by type. A purse IS an inventory, so asking by type could hand back the bag
            // - or hand the bag code the purse, which is worse and would silently make money take up
            // slots again.
            var purse = instance.GetOrAddState<PurseState>(PurseState.Key);
            if (purse == null) return;

            // Zero or less means unlimited everywhere in the inventory, which is what makes this a
            // purse rather than a very large bag.
            purse.Capacity = 0;

            if (purse.Seeded) return;
            purse.Seeded = true;

            for (int i = 0; i < _startingMoney.Length; i++)
            {
                var entry = _startingMoney[i];
                if (entry == null || entry.Item == null || entry.Count <= 0) continue;

                purse.Add(new GameInstance(entry.Item), entry.Count);
            }
        }
    }
}
