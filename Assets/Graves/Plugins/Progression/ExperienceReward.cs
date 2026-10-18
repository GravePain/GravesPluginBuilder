using UnityEngine;
using Graves.Core;

namespace Graves.Plugins.Progression
{
    /// <summary>
    /// Hands experience to whatever killed this.
    /// </summary>
    /// <remarks>
    /// <b>The reward lives on the victim, not on the killer.</b> What a thing is worth is a property
    /// of that thing, so a goblin says what killing it pays and every killer gets the same answer.
    /// Putting it on the killer would mean every attacker carrying a table of what everything is
    /// worth, which is the same table copied everywhere and wrong in one of them.
    ///
    /// <b>It closes the loop.</b> The player acts, the world responds, the state persists - and this
    /// is the fourth beat, the one that makes it worth doing again. Without it a kill costs time and
    /// health and returns nothing, and the correct play is to walk past everything.
    /// </remarks>
    [DisallowMultipleComponent]
    public class ExperienceReward : MonoBehaviour
    {
        private IMortal _mortal;
        private LevelValue _reward = LevelValue.Flat(50f);
        private bool _paid;

        /// <summary>What this thing is currently worth to kill.</summary>
        public int Worth
        {
            get
            {
                var level = GetComponent<Level>();
                int at = level != null ? level.CurrentLevel : 1;
                return Mathf.Max(0, _reward.IntAt(at));
            }
        }

        /// <summary>Wires the reward to the thing whose death pays it out.</summary>
        public void Initialise(IMortal _own, LevelValue _amount)
        {
            if (_mortal != null) _mortal.Died -= OnDied;

            _mortal = _own;
            _reward = _amount;

            if (_mortal != null) _mortal.Died += OnDied;
        }

        private void OnDied(GameObject _killer)
        {
            // Paid once. Death already fires once, and this second guard exists because a reward paid
            // twice is the kind of bug that is only noticed as "levelling feels fast" months later.
            if (_paid) return;
            _paid = true;

            if (_killer == null) return;

            // The killer's own progression, if it has any. A monster killing a monster pays nothing
            // to anybody, which is correct and needs no special case.
            var theirLevel = _killer.GetComponentInParent<Level>();
            if (theirLevel == null) return;

            int amount = Worth;
            if (amount <= 0) return;

            theirLevel.Grant(amount);
        }

        private void OnDestroy()
        {
            if (_mortal != null) _mortal.Died -= OnDied;
        }
    }
}
