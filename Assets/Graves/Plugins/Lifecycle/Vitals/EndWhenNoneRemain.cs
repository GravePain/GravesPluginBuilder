using Graves.Core;
using Graves.Plugins.Vitals;
using UnityEngine;

namespace Graves.Plugins.Lifecycle.Vitals
{
    /// <summary>
    /// Ends the loop when nothing of a given kind is left alive.
    /// </summary>
    /// <remarks>
    /// <b>The second exit, and the one nobody had noticed until somebody played it.</b> Graves killed
    /// everything and reported winning by accident: the loop did not end, it simply ran out of things
    /// to do, which is a different and worse experience than being told you had won.
    ///
    /// <b>It waits for at least one to have existed.</b> Otherwise a game is won on its first frame,
    /// before any spawner has produced anything, which is the obvious implementation and is wrong in
    /// exactly the case that matters.
    ///
    /// <b>It scans rather than counting subscriptions.</b> Entities are created by spawners, killed,
    /// removed and restored from saves, and every one of those paths would have to remember to keep a
    /// tally. A scan cannot be forgotten. It runs on the session's own interval, a few times a second
    /// rather than every frame, which is the right trade for a question nobody needs answered
    /// instantly.
    /// </remarks>
    [DisallowMultipleComponent]
    public class EndWhenNoneRemain : MonoBehaviour, ISessionEnding
    {
        private EntityDefinition _kind;
        private string _description = "Nothing left standing.";
        private bool _anyEverExisted;

        /// <summary>Wires this to the kind of thing whose extinction ends the game.</summary>
        public void Initialise(EntityDefinition _target, string _reason)
        {
            _kind = _target;
            if (!string.IsNullOrWhiteSpace(_reason)) _description = _reason;

            Session.Instance.Register(this);
        }

        /// <summary>How many of that kind are alive right now.</summary>
        public int LivingCount()
        {
            if (_kind == null) return 0;

            int alive = 0;
            var entities = FindObjectsByType<GameEntity>(FindObjectsSortMode.None);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (entity == null || entity.Definition == null) continue;

                // By uid rather than by reference, because an entity restored from a save resolves
                // its definition through the registry and need not be the same object.
                if (entity.Definition.Uid != _kind.Uid) continue;

                var health = entity.GetComponent<Health>();
                if (health != null && health.IsDead) continue;

                alive++;
            }

            return alive;
        }

        /// <inheritdoc />
        public bool HasEnded(out SessionOutcome _outcome, out string _reason)
        {
            _outcome = SessionOutcome.Won;
            _reason = _description;

            if (_kind == null) return false;

            int alive = LivingCount();
            if (alive > 0) { _anyEverExisted = true; return false; }

            return _anyEverExisted;
        }

        private void OnDestroy()
        {
            if (Session.Instance != null) Session.Instance.Unregister(this);
        }
    }
}
