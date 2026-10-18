using Graves.Core;
using Graves.Plugins.Vitals;
using UnityEngine;

namespace Graves.Plugins.Lifecycle.Vitals
{
    /// <summary>
    /// Ends the loop when this entity is killed.
    /// </summary>
    /// <remarks>
    /// <b>The first of the two exits, and the one that was already half-answered.</b> Respawn made
    /// death legible without making it an ending, which was right for a test bed and wrong for a
    /// game where dying means something.
    ///
    /// <b>Respawn and this are alternative answers to the same exit, and installing both is a
    /// contradiction.</b> One says death continues the run and the other says it stops it. Reported
    /// rather than silently resolved, because whichever won would be an accident of ordering and the
    /// designer would be left arguing with a coin toss.
    /// </remarks>
    [DisallowMultipleComponent]
    public class EndWhenThisDies : MonoBehaviour, ISessionEnding
    {
        private Health _health;
        private string _description = "You died.";

        /// <summary>Wires this to the health whose end ends the game.</summary>
        public void Initialise(Health _target, string _reason)
        {
            _health = _target;
            if (!string.IsNullOrWhiteSpace(_reason)) _description = _reason;

            Session.Instance.Register(this);
        }

        /// <inheritdoc />
        public bool HasEnded(out SessionOutcome _outcome, out string _reason)
        {
            _outcome = SessionOutcome.Lost;
            _reason = _description;

            return _health != null && _health.IsDead;
        }

        private void OnDestroy()
        {
            // The session outlives this, so leaving a destroyed condition registered would have it
            // asked forever - and a Unity object that has been destroyed answers questions oddly
            // rather than refusing to.
            if (Session.Instance != null) Session.Instance.Unregister(this);
        }
    }
}
