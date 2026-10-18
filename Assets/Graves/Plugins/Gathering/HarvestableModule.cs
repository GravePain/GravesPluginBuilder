using Graves.Core;
using Graves.Plugins.Inventory;
using UnityEngine;

namespace Graves.Plugins.Gathering
{
    /// <summary>
    /// Says that this kind of entity can be harvested, and what comes out.
    /// </summary>
    /// <remarks>
    /// Every number a designer would want to change lives here rather than on the component: what it
    /// yields, how much per pull, how many pulls before it is bare, and how long until it comes back.
    /// The component holds none of them, which is what stops a node in a scene quietly disagreeing
    /// with the definition it was made from.
    /// </remarks>
    [CreateAssetMenu(fileName = "HarvestableModule", menuName = "Graves/Modules/Harvestable")]
    public class HarvestableModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("What comes out. Required - a node with no yield is a rock you cannot mine.")]
        private ItemDefinition _yield;

        [SerializeField]
        [Tooltip("How many items each harvest gives.")]
        [Min(1)] private int _yieldPerHarvest = 1;

        [SerializeField]
        [Tooltip("How many harvests before it is stripped bare.")]
        [Min(1)] private int _harvests = 3;

        [SerializeField]
        [Tooltip("Seconds before a stripped node refills. Zero means it never comes back.")]
        [Min(0f)] private float _regrowSeconds = 30f;

        /// <summary>What comes out.</summary>
        public ItemDefinition Yield => _yield;

        /// <summary>How many items per harvest.</summary>
        public int YieldPerHarvest => Mathf.Max(1, _yieldPerHarvest);

        /// <summary>How many harvests it holds when full.</summary>
        public int Harvests => Mathf.Max(1, _harvests);

        /// <summary>Seconds before a stripped node refills. Zero means never.</summary>
        public float RegrowSeconds => Mathf.Max(0f, _regrowSeconds);

        /// <inheritdoc />
        /// <remarks>
        /// The missing-yield case is reported rather than tolerated. A node that silently gives
        /// nothing is indistinguishable, in play, from one that is bugged - and a designer looking at
        /// a filled-in inspector has no reason to suspect the field they left empty.
        /// </remarks>
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null || _entity.Instance == null) return;

            if (_yield == null)
            {
                Debug.LogError("[Gathering] '" + _entity.name + "' is harvestable but yields nothing. " +
                               "Set the yield item on its Harvestable module.", _entity);
                return;
            }

            var host = _entity.gameObject;
            var node = host.GetComponent<Harvestable>();
            if (node == null) node = host.AddComponent<Harvestable>();

            node.Bind(this, _entity.Instance.GetOrAddState<HarvestState>(HarvestState.Key));
        }
    }
}
