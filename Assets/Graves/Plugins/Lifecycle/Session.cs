using System;
using System.Collections.Generic;
using UnityEngine;

namespace Graves.Plugins.Lifecycle
{
    /// <summary>
    /// Whether the loop is still running, and if not, how it ended.
    /// </summary>
    /// <remarks>
    /// <b>Three values, and deliberately not a state machine.</b> A framework shipping Boot, Menu,
    /// Loading, Playing and Paused would be deciding what phases a game has, and plenty of games have
    /// none of those. What every game with a loop does have is the question of whether the loop is
    /// still going.
    ///
    /// Won and Lost rather than one Ended, because the two want different things to happen next and
    /// anything reacting would otherwise have to ask a second question immediately.
    /// </remarks>
    public enum SessionOutcome
    {
        /// <summary>The loop is running.</summary>
        Running = 0,

        /// <summary>The loop ended well.</summary>
        Won = 1,

        /// <summary>The loop ended badly.</summary>
        Lost = 2
    }

    /// <summary>
    /// Something that can end the loop.
    /// </summary>
    /// <remarks>
    /// <b>This is the seam the framework was missing.</b> Its own journal recorded that the loop has
    /// two exits, that you die or the world runs out, and that nothing owned either. Respawn existed
    /// as the cheapest legible answer to the first and was described as exactly that rather than as
    /// the right one.
    ///
    /// An ending is a component answering one question, so what ends a game is authored per game and
    /// this assembly never learns what health or an enemy is.
    ///
    /// <b>It is asked, not told.</b> A condition that had to announce itself would need every
    /// possible cause to remember to, and the one that forgot would produce a game that cannot be
    /// won. Asking has no such list.
    /// </remarks>
    public interface ISessionEnding
    {
        /// <summary>True when this has ended the loop, with how it ended and why in words.</summary>
        bool HasEnded(out SessionOutcome _outcome, out string _reason);
    }

    /// <summary>
    /// The loop itself: whether it is running, and what ended it.
    /// </summary>
    /// <remarks>
    /// <b>The layer this framework did not have.</b> Its own categorisation puts bootstrap and
    /// lifecycle first, and everything built until now sat above it. Entities could die and worlds
    /// could empty, and nothing anywhere could say the game was over. Respawn and a spawner were the
    /// two cheapest ways to stop those questions being asked.
    ///
    /// <b>A plugin rather than core, and it was the closest decision so far.</b> The argument for
    /// core is that the categorisation calls lifecycle a bookend and the core owns bookends. The
    /// argument against is stronger: what ends a game is the most game-specific decision there is,
    /// and the core owns identity, structure, extension and integrity, none of which this is. A core
    /// that knew about winning would be a framework with an opinion about what a game is.
    ///
    /// What would move it is something needing to ask whether the game is running WITHOUT depending
    /// on this plugin. Nothing has needed that. When something does, that is the argument, and it
    /// should be made then rather than pre-empted now.
    ///
    /// <b>It ends once.</b> A loop that has been lost cannot then be won by a second condition
    /// noticing something a frame later, and a game that shows two endings has no ending at all.
    /// </remarks>
    [DisallowMultipleComponent]
    public class Session : MonoBehaviour
    {
        private static Session _instance;

        private readonly List<ISessionEnding> _endings = new List<ISessionEnding>();
        private float _nextCheck;
        private float _interval = 0.25f;

        /// <summary>The session, made on first use.</summary>
        public static Session Instance
        {
            get
            {
                if (_instance != null) return _instance;

                _instance = FindFirstObjectByType<Session>();
                if (_instance != null) return _instance;

                var host = new GameObject("Graves Session");
                _instance = host.AddComponent<Session>();
                return _instance;
            }
        }

        /// <summary>How the loop currently stands.</summary>
        public SessionOutcome Outcome { get; private set; } = SessionOutcome.Running;

        /// <summary>Why it ended, or null while it runs.</summary>
        public string Reason { get; private set; }

        /// <summary>True while the loop is running.</summary>
        public bool IsRunning => Outcome == SessionOutcome.Running;

        /// <summary>How many conditions could currently end the loop.</summary>
        public int EndingCount => _endings.Count;

        /// <summary>Raised once, when the loop ends. Carries the outcome and the reason.</summary>
        public event Action<SessionOutcome, string> Ended;

        /// <summary>Raised when a new loop begins.</summary>
        public event Action Restarted;

        /// <summary>Adds a condition that can end the loop.</summary>
        /// <remarks>
        /// Registered rather than searched for, so a condition on something that spawns halfway
        /// through a game counts from the moment it exists.
        /// </remarks>
        public void Register(ISessionEnding _ending)
        {
            if (_ending == null || _endings.Contains(_ending)) return;
            _endings.Add(_ending);
        }

        /// <summary>Removes a condition.</summary>
        public void Unregister(ISessionEnding _ending) => _endings.Remove(_ending);

        /// <summary>Ends the loop, if it has not already ended.</summary>
        /// <remarks>
        /// Public so a game can end its own loop for a reason no condition covers: a cutscene, a
        /// quest, somebody quitting deliberately. The first ending wins, whatever caused it.
        /// </remarks>
        public void End(SessionOutcome _outcome, string _reason)
        {
            if (_outcome == SessionOutcome.Running || !IsRunning) return;

            Outcome = _outcome;
            Reason = _reason;

            Debug.Log("[Session] The game is over: " + _outcome + " - " +
                      (string.IsNullOrEmpty(_reason) ? "no reason given" : _reason));
            Ended?.Invoke(_outcome, _reason);
        }

        /// <summary>Puts the loop back to running.</summary>
        /// <remarks>
        /// It does not put the WORLD back. Restoring positions, health, spawners and inventories is
        /// what a save is for, and a lifecycle plugin quietly resetting a world would be guessing at
        /// what should survive.
        /// </remarks>
        public void Restart()
        {
            Outcome = SessionOutcome.Running;
            Reason = null;
            Restarted?.Invoke();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[Session] A second session appeared on " + name +
                                 ". The first one is still in charge.", this);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            if (!IsRunning || Time.time < _nextCheck) return;
            _nextCheck = Time.time + _interval;

            for (int i = 0; i < _endings.Count; i++)
            {
                var ending = _endings[i];
                if (ending == null) continue;
                if (!ending.HasEnded(out var outcome, out var reason)) continue;

                End(outcome, reason);
                return;
            }
        }
    }
}
