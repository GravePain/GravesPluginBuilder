using Graves.Plugins.Combat;
using UnityEngine;

namespace Graves.Plugins.AI.Combat
{
    /// <summary>
    /// Stops a hunter hurting anything that is not something worth hunting.
    /// </summary>
    /// <remarks>
    /// <b>Written because two grunts killed each other on the way to the player.</b> Both converged
    /// on the same spot, both swung, and each caught the other in an arc aimed at somebody else.
    /// They were dead before they arrived, which turns a fight into a suicide pact and would have
    /// been very confusing to watch without knowing why.
    ///
    /// <b>The rule is crude on purpose: a hunter may only hurt what carries an AI target marker.</b>
    /// That makes every hunter harmless to every other hunter, which is wrong for a game wanting
    /// monsters that feud, and exactly right for a game that has no notion of sides yet. When
    /// factions are built this class is what they replace.
    /// </remarks>
    [DisallowMultipleComponent]
    public class HuntsTargetsOnly : MonoBehaviour, IDamageFilter
    {
        /// <inheritdoc />
        public bool MayDamage(Graves.Plugins.Vitals.Health _victim)
        {
            if (_victim == null) return false;
            return _victim.GetComponent<AiTarget>() != null;
        }
    }
}
