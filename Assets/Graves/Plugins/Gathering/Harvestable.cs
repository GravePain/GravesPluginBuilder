using Graves.Core;
using Graves.Plugins.Inventory;
using UnityEngine;

namespace Graves.Plugins.Gathering
{
    /// <summary>
    /// A thing in the world you can take something out of: a bush, a rock, a fallen log.
    /// </summary>
    /// <remarks>
    /// <b>It is an entity, and that is the whole reason this plugin is short.</b> Being an entity is
    /// what gives a node a definition to be authored from, an identity that survives a save, and a
    /// place to keep how much is left. A harvestable that was scene furniture instead would need all
    /// three inventing here.
    /// </remarks>
    [DisallowMultipleComponent]
    public class Harvestable : MonoBehaviour
    {
        private HarvestState _state;
        private HarvestableModule _authored;

        /// <summary>What this node yields, and how much of it.</summary>
        public ItemDefinition Yield => _authored != null ? _authored.Yield : null;

        /// <summary>Harvests left. Zero means stripped.</summary>
        public int Remaining => _state != null ? _state.Remaining : 0;

        /// <summary>True when there is nothing left to take right now.</summary>
        public bool IsDepleted => Remaining <= 0;

        /// <summary>Hands the node its authored settings and its storage.</summary>
        public void Bind(HarvestableModule _module, HarvestState _storage)
        {
            _authored = _module;
            _state = _storage;

            if (_state == null || _authored == null) return;

            // Seeded once. Without the guard a loaded save would have every stripped node full again
            // on the next load, which reads as the world resetting rather than as a save fault.
            if (_state.Seeded) return;
            _state.Seeded = true;
            _state.Remaining = _authored.Harvests;
        }

        /// <summary>
        /// Takes one harvest, if there is one to take.
        /// </summary>
        /// <remarks>
        /// <b>Nothing is removed from the node until the items are actually in the bag.</b> A full
        /// inventory must not strip a bush and drop what it held on the floor of nowhere — so the
        /// inventory is asked first, the node is decremented by what it accepted, and a gatherer who
        /// cannot carry anything leaves the node exactly as they found it.
        /// </remarks>
        /// <returns>How many items were actually taken. Zero means nothing happened.</returns>
        public int Harvest(GameObject _gatherer, InventoryState _into)
        {
            if (_state == null || _authored == null || _into == null) return 0;
            if (IsDepleted) return 0;
            if (_authored.Yield == null) return 0;

            int stored = _into.Add(new GameInstance(_authored.Yield), _authored.YieldPerHarvest);
            if (stored <= 0) return 0;

            _state.Remaining--;

            if (IsDepleted && _authored.RegrowSeconds > 0f) _state.RegrowIn = _authored.RegrowSeconds;

            GameEvents.Publish(new ItemGathered(_gatherer, _authored.Yield, stored, IsDepleted));
            return stored;
        }

        /// <summary>
        /// Counts down a stripped node's regrowth.
        /// </summary>
        /// <remarks>
        /// Counted down in the state rather than compared against a clock, because
        /// <c>Time.time</c> starts again at zero every session and a stored timestamp would make
        /// every node either regrow at once on load or never.
        /// </remarks>
        private void Update()
        {
            if (_state == null || !IsDepleted || _state.RegrowIn <= 0f) return;

            _state.RegrowIn -= Time.deltaTime;
            if (_state.RegrowIn > 0f) return;

            _state.RegrowIn = 0f;
            _state.Remaining = _authored != null ? _authored.Harvests : 0;
        }
    }
}
