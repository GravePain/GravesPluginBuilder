using Graves.Plugins.Vitals;
using UnityEngine;

namespace Graves.Plugins.UI.Vitals
{
    /// <summary>
    /// Shows one entity health on screen.
    /// </summary>
    /// <remarks>
    /// <b>It reads rather than listens, and that is the finding.</b> Health raises events when it is
    /// damaged and healed, and this subscribes to none of them - because the number that has to be
    /// drawn is not just current health but current OVER maximum, and maximum changes silently when
    /// equipment is worn, a level is gained or a buff lands. An event for every one of those is a
    /// list somebody has to remember to extend; asking for the fraction is a list of nothing.
    ///
    /// The cost is one property read a frame against a value that is computed anyway. The benefit is
    /// that a bar cannot go stale, which is the failure mode that matters: a health bar showing the
    /// wrong number is worse than one that costs a microsecond.
    /// </remarks>
    [DisallowMultipleComponent]
    public class HealthBarBinding : MonoBehaviour
    {
        private Health _health;
        private Bar _bar;
        private Color _healthy = new Color(0.75f, 0.2f, 0.2f, 0.95f);
        private Color _hurt = new Color(0.9f, 0.55f, 0.1f, 0.95f);

        /// <summary>Wires this to the health it shows and the bar it draws into.</summary>
        public void Initialise(Health _target, Bar _display)
        {
            _health = _target;
            _bar = _display;
        }

        private void LateUpdate()
        {
            if (_health == null || _bar == null) return;

            float fraction = _health.Fraction;
            _bar.SetFraction(fraction);
            _bar.SetLabel(_health.Current + " / " + _health.Max);

            // Colour is the cheapest way to say "you are in trouble" without a number being read.
            _bar.SetColour(fraction <= 0.3f ? _hurt : _healthy);
        }

        private void OnDestroy()
        {
            if (_bar != null) Destroy(_bar.gameObject);
        }
    }
}
