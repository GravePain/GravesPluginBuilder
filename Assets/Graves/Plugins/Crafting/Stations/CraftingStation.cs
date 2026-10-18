using System.Collections.Generic;
using UnityEngine;

namespace Graves.Plugins.Crafting.Stations
{
    /// <summary>
    /// A place in the world where work of certain kinds can be done.
    /// </summary>
    /// <remarks>
    /// <b>Stations keep their own register.</b> A recipe asking "is there a forge near me" would
    /// otherwise search the scene, and a crafting window that asks every frame while the mouse moves
    /// over a list of recipes turns that into a real cost. A list maintained on enable and disable
    /// is free to read.
    ///
    /// <b>Proximity, not interaction.</b> Being near a forge is enough to use it. Whether a game
    /// wants you to press a key, open a screen, or be facing the right way is an interaction system,
    /// which does not exist here and would be a different plugin when it does. Distance is the
    /// smallest honest version and it is testable without a person.
    /// </remarks>
    [DisallowMultipleComponent]
    public class CraftingStation : MonoBehaviour
    {
        private static readonly List<CraftingStation> Active = new List<CraftingStation>();

        private readonly List<CraftingStationDefinition> _kinds = new List<CraftingStationDefinition>();

        /// <summary>Every station currently in the scene and enabled.</summary>
        public static IReadOnlyList<CraftingStation> All => Active;

        /// <summary>The kinds of work this station supports.</summary>
        public IReadOnlyList<CraftingStationDefinition> Kinds => _kinds;

        /// <summary>Declares which kinds of work happen here.</summary>
        /// <remarks>
        /// <b>Registration happens here as well as in OnEnable, and that is not belt and braces.</b>
        /// Unity does not guarantee OnEnable has run by the time a script that just created a station
        /// looks for one - it is deferred, so a station built and consulted in the same call is
        /// invisible. That was found by exactly that sequence: a forge placed three metres away, and
        /// a recipe insisting there was no forge.
        ///
        /// Configuring a station is the moment it becomes a real place, so it is the honest moment to
        /// be findable. The enable and disable callbacks still matter for a station switched off and
        /// on again later.
        /// </remarks>
        public void Configure(IEnumerable<CraftingStationDefinition> _provides)
        {
            _kinds.Clear();

            if (_provides != null)
            {
                foreach (var kind in _provides)
                {
                    if (kind == null || _kinds.Contains(kind)) continue;
                    _kinds.Add(kind);
                }
            }

            if (isActiveAndEnabled && !Active.Contains(this)) Active.Add(this);
        }

        /// <summary>True when this station supports a given kind of work.</summary>
        public bool Supports(CraftingStationDefinition _kind)
        {
            if (_kind == null) return false;

            for (int i = 0; i < _kinds.Count; i++)
            {
                // Compared by uid rather than by reference, so a station configured from a copy of a
                // definition still matches. Identity is the uid everywhere else in this framework and
                // there is no reason for this to be the exception.
                if (_kinds[i] != null && _kinds[i].Uid == _kind.Uid) return true;
            }
            return false;
        }

        /// <summary>
        /// The nearest station of a kind within range of a point, or null when there is none.
        /// </summary>
        /// <remarks>
        /// Distance is measured on the horizontal plane. A forge on the floor above is not a forge
        /// you are standing at, and a player on a bridge over a smithy should not be able to use it.
        /// </remarks>
        public static CraftingStation NearestOfKind(Vector3 _position, CraftingStationDefinition _kind,
                                                    float _range)
        {
            if (_kind == null || _range <= 0f) return null;

            float limit = _range * _range;
            CraftingStation best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < Active.Count; i++)
            {
                var station = Active[i];
                if (station == null || !station.Supports(_kind)) continue;

                var offset = station.transform.position - _position;
                offset.y = 0f;

                float distance = offset.sqrMagnitude;
                if (distance > limit || distance >= bestDistance) continue;

                bestDistance = distance;
                best = station;
            }

            return best;
        }

        private void OnEnable()
        {
            if (!Active.Contains(this)) Active.Add(this);
        }

        private void OnDisable() => Active.Remove(this);

        // Destroyed without ever being disabled - editor tooling does this constantly - would
        // otherwise leave a dead entry that every lookup has to step over.
        private void OnDestroy() => Active.Remove(this);
    }
}
