using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Equipment.Stats
{
    /// <summary>
    /// What wearing an item does to the wearer's numbers.
    /// </summary>
    /// <remarks>
    /// Attached to an item definition, alongside the module that makes it equippable. The two are
    /// separate because plenty of equipment changes nothing - a hat, a torch, a guild tabard - and
    /// plenty of things that change numbers are not worn.
    ///
    /// <b>The bonuses are flat, not level-scaled.</b> A sword that grows stronger as its owner levels
    /// makes the same weapon a different weapon in two hands, which is surprising and hard to reason
    /// about when balancing. The reference does the same, and the way to express a better sword is a
    /// better sword.
    /// </remarks>
    [CreateAssetMenu(fileName = "StatBonusModule", menuName = "Graves/Modules/Stat Bonus")]
    public class StatBonusModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("Added to the wearer's maximum health. Negative values are allowed - heavy armour " +
                 "that costs vitality is a real design.")]
        private int _health;

        [SerializeField]
        [Tooltip("Added to the wearer's damage per hit.")]
        private int _damage;

        [SerializeField]
        [Tooltip("Added as a percentage of the wearer's base maximum health, before flat bonuses. " +
                 "Ten means ten per cent.")]
        private float _healthPercent;

        /// <summary>Flat maximum health granted.</summary>
        public int Health => _health;

        /// <summary>Flat damage granted.</summary>
        public int Damage => _damage;

        /// <summary>Proportional health granted, as a percentage of the base.</summary>
        public float HealthPercent => _healthPercent;
    }
}
