using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Equipment.Enhancement
{
    /// <summary>
    /// What each level of enhancement is worth on this item.
    /// </summary>
    /// <remarks>
    /// <b>Authored per item rather than as a global rate.</b> A hidden "every level adds ten per
    /// cent" would make every item in the game scale identically, and would live nowhere a designer
    /// could see it. Saying it on the item means a plus-five dagger and a plus-five greatsword can
    /// differ, which is the entire reason to have items.
    ///
    /// <b>Separate from the stat bonus module, deliberately.</b> One says what the item is worth;
    /// this says what improving it is worth. An item can carry either without the other - a ring that
    /// grants health and can never be improved, or a plain sword whose only value is that it can be.
    /// </remarks>
    [CreateAssetMenu(fileName = "EnhancementScalingModule", menuName = "Graves/Modules/Enhancement Scaling")]
    public class EnhancementScalingModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("Maximum health added for each level of enhancement on this item.")]
        private int _healthPerLevel;

        [SerializeField]
        [Tooltip("Damage added for each level of enhancement on this item.")]
        private int _damagePerLevel = 1;

        /// <summary>Extra maximum health per level.</summary>
        public int HealthPerLevel => _healthPerLevel;

        /// <summary>Extra damage per level.</summary>
        public int DamagePerLevel => _damagePerLevel;
    }
}
