using System;
using System.Collections.Generic;
using UnityEngine;

namespace Graves.Core
{
    /// <summary>
    /// Marker for anything that can be announced on <see cref="GameEvents"/>.
    /// </summary>
    /// <remarks>
    /// Deliberately empty. A framework this one was measured against put a timestamp, a source, a
    /// priority and a cancellation flag on every event; its own bus read none of them. Members on a
    /// base type that nothing consumes are a contract every author has to satisfy and nobody
    /// benefits from — an event carries what it is about, and nothing else.
    /// </remarks>
    public interface IGameEvent
    {
    }

    /// <summary>
    /// Announces that something happened, to anyone listening, without the announcer knowing who.
    /// </summary>
    /// <remarks>
    /// <b>This was refused three times and the refusals were right.</b> The question each time was
    /// "do two of our plugins need to talk?", and each time the answer was no: what looked like a bus
    /// turned out to want a local typed event or a bridge assembly joining two plugins so neither
    /// depends on the other. Twenty-odd plugins later, that is still true internally.
    ///
    /// <b>What changed is the question.</b> A bridge requires editing the framework. It is the right
    /// answer for someone who can do that and no answer at all for someone who cannot — and the
    /// evidence that this matters is not a prediction:
    ///
    /// Community plugins written for a commercial framework by people with no ability to change it
    /// advertise "fully event-driven, zero coupling to any specific gameplay system" as their
    /// headline feature, three of them independently. A different asset's extension ecosystem runs on
    /// global events and its extensions are one file each. In both cases a bus is what let a stranger
    /// extend something they could not edit.
    ///
    /// <b>What this deliberately does not have, and why.</b> The bus it was modelled against ships a
    /// priority system, an event queue, and a consumption protocol. A search of that entire project
    /// found nothing that ever called <c>ProcessQueuedEvents</c> — the queue accumulates forever and
    /// nothing it holds is ever delivered. Its handler interface returns a <c>bool</c> that the
    /// dispatcher discards, so an author who returns false to stop propagation is not told that
    /// nothing happened. Machinery built ahead of a need, then never wired, is worse than no
    /// machinery: it reads as a supported feature.
    ///
    /// So: no queue, no priority, no consumption. Each can be added by whatever first genuinely needs
    /// it, and will then have a caller on the day it appears.
    ///
    /// <b>Publishing is by runtime type, always.</b> The model dispatched immediate events by the
    /// compile-time type and queued ones by the runtime type, so the same event object reached
    /// different handlers depending on how it was sent. There is one path here and it takes the
    /// object's own type.
    /// </remarks>
    public static class GameEvents
    {
        private static readonly Dictionary<Type, List<Subscription>> Handlers =
            new Dictionary<Type, List<Subscription>>();

        /// <summary>
        /// One listener: the delegate as handed in, and a way to call it without reflection.
        /// </summary>
        /// <remarks>
        /// Both halves are needed. The original is what unsubscribing compares against, because that
        /// is what the caller has. The wrapper is what publishing invokes: a handler subscribed to a
        /// base type is an <c>Action&lt;Base&gt;</c> being handed a derived event, which
        /// <c>DynamicInvoke</c> would resolve by reflection on every single publish. Closing over the
        /// cast once, at subscribe time, makes it an ordinary call.
        /// </remarks>
        private sealed class Subscription
        {
            public readonly Delegate Original;
            public readonly Action<IGameEvent> Invoke;

            public Subscription(Delegate _original, Action<IGameEvent> _invoke)
            {
                Original = _original;
                Invoke = _invoke;
            }
        }

        /// <summary>How many handlers are registered, across every event type. For diagnostics.</summary>
        public static int HandlerCount
        {
            get
            {
                int total = 0;
                foreach (var pair in Handlers) total += pair.Value.Count;
                return total;
            }
        }

        /// <summary>
        /// Empties the bus. Runs automatically when play begins.
        /// </summary>
        /// <remarks>
        /// <b>This is not tidiness, it is the bug that a static bus always has.</b> With Unity's
        /// "Enter Play Mode without domain reload" enabled, statics survive between play sessions —
        /// so handlers registered by objects that were destroyed at the end of the last run are still
        /// in this dictionary at the start of the next one. They then throw on every publish, get
        /// caught, and log forever, with the first run's objects named in errors during the second.
        ///
        /// <c>SubsystemRegistration</c> runs before any scene loads, on every entry to play mode,
        /// reload or not. It is the only hook that closes this.
        /// </remarks>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear() => Handlers.Clear();

        /// <summary>
        /// Starts listening for events of one type, and for anything derived from it.
        /// </summary>
        /// <remarks>
        /// <b>Unsubscribe in <c>OnDisable</c>, not <c>OnDestroy</c>.</b> A disabled object that keeps
        /// receiving is the source of the strangest class of bug in an event-driven game: something
        /// switched off reacting to the world.
        ///
        /// A method group unsubscribes correctly — delegate equality compares the target and the
        /// method, not the delegate object. An anonymous lambda does not, because the second one is a
        /// different delegate. Subscribing a lambda is therefore subscribing forever, and the leak
        /// looks like a handler firing twice.
        /// </remarks>
        public static void Subscribe<T>(Action<T> _handler) where T : IGameEvent
        {
            if (_handler == null) return;

            var type = typeof(T);
            if (!Handlers.TryGetValue(type, out var list))
            {
                list = new List<Subscription>();
                Handlers.Add(type, list);
            }

            list.Add(new Subscription(_handler, _event => _handler((T)_event)));
        }

        /// <summary>Stops listening. Safe to call when not subscribed.</summary>
        public static void Unsubscribe<T>(Action<T> _handler) where T : IGameEvent
        {
            if (_handler == null) return;

            var type = typeof(T);
            if (!Handlers.TryGetValue(type, out var list)) return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Original.Equals(_handler)) list.RemoveAt(i);
            }

            if (list.Count == 0) Handlers.Remove(type);
        }

        /// <summary>
        /// Announces an event to everything listening for its type, its base types, or an interface
        /// it implements.
        /// </summary>
        /// <remarks>
        /// <b>Handlers are copied before any of them runs.</b> A handler that subscribes or
        /// unsubscribes while reacting is ordinary — a death handler that spawns loot, a loot handler
        /// that registers itself — and iterating the live list would throw halfway through, leaving
        /// the remaining handlers unnotified for a reason that has nothing to do with them.
        ///
        /// <b>One handler throwing must not silence the rest.</b> An exception is reported with the
        /// event type and the offending target, and the next handler still runs. The alternative is
        /// that an unrelated plugin's bug stops the player's death from being noticed.
        ///
        /// <b>Order is not defined and must not be relied on.</b> If two handlers must run in a
        /// particular sequence, they are one handler that has not been written yet.
        /// </remarks>
        public static void Publish<T>(T _event) where T : IGameEvent
        {
            if (_event == null) return;

            List<Subscription> matched = null;

            // The object's own type, never the compile-time one, so publishing through a base-typed
            // variable reaches exactly the same handlers as publishing through a concrete one.
            var type = _event.GetType();

            for (var current = type; current != null; current = current.BaseType)
            {
                if (!Handlers.TryGetValue(current, out var list)) continue;
                matched ??= new List<Subscription>();
                matched.AddRange(list);
            }

            foreach (var contract in type.GetInterfaces())
            {
                if (!Handlers.TryGetValue(contract, out var list)) continue;
                matched ??= new List<Subscription>();
                matched.AddRange(list);
            }

            if (matched == null) return;

            for (int i = 0; i < matched.Count; i++)
            {
                try
                {
                    matched[i].Invoke(_event);
                }
                catch (Exception error)
                {
                    var cause = error.InnerException ?? error;
                    var target = matched[i].Original.Target as UnityEngine.Object;
                    Debug.LogError("[GameEvents] A handler for '" + type.Name + "' threw " +
                                   cause.GetType().Name + ": " + cause.Message +
                                   ". Other handlers still ran.", target);
                }
            }
        }
    }
}
