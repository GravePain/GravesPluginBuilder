using Graves.Core;
using Graves.Plugins.Input;
using Graves.Plugins.Inventory;
using UnityEngine;

namespace Graves.Plugins.Gathering
{
    /// <summary>
    /// Lets whoever carries this take from the nearest harvestable node in front of them.
    /// </summary>
    /// <remarks>
    /// <b>Nearest wins, and there is no aiming.</b> A cone, a raycast or a highlight would each be
    /// better and each needs a camera, a reticle or a piece of user interface to be worth having.
    /// Nearest-within-range is the smallest thing that lets a person walk up to a bush and get
    /// something, which is the loop this exists to close.
    ///
    /// <b>It reads the shared input source rather than the keyboard.</b> Two things reading the
    /// keyboard in one game is a bug that shows up as an action firing twice, or once, depending on
    /// component order. The <c>Use</c> field was added to the input struct for this - the cost D-002
    /// predicted and priced.
    /// </remarks>
    [DisallowMultipleComponent]
    public class Gatherer : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("How close a node must be to be harvested, in metres.")]
        [Min(0f)] private float _reach = 2.5f;

        private InputSource _input;
        private GameEntity _entity;

        /// <summary>The node that a use press would harvest right now, or null.</summary>
        public Harvestable Target { get; private set; }

        /// <summary>How far this can reach.</summary>
        public float Reach => _reach;

        /// <summary>
        /// Applies the authored setting. Called by the module when the entity wakes.
        /// </summary>
        /// <remarks>
        /// <b>Without this the module's field was decoration.</b> It was: the module declared a reach,
        /// tooltipped it, and never handed it over, so a designer who set five metres got the
        /// component's default of two and a half and had no way to tell. Authored configuration that
        /// does not arrive is worse than none, because the inspector says it worked.
        ///
        /// Every module that owns a number needs a call like this one. Vitals has it, inventory has
        /// it; this did not, and nothing caught it because both values were plausible.
        /// </remarks>
        public void Configure(float _range) => _reach = Mathf.Max(0f, _range);

        private void Awake()
        {
            _input = GetComponent<InputSource>();
            _entity = GetComponent<GameEntity>();

            if (_entity == null)
            {
                Debug.LogError("[Gathering] '" + name + "' can gather but is not an entity, so it " +
                               "has no inventory to gather into.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            Target = Nearest();

            if (_input == null || Target == null) return;
            if (!_input.Read().Use) return;

            var bag = _entity.Instance != null
                ? _entity.Instance.GetState<InventoryState>(InventoryState.Key)
                : null;

            if (bag == null)
            {
                Debug.LogWarning("[Gathering] '" + name + "' has nowhere to put what it gathers. " +
                                 "Add the Inventory module to its definition.", this);
                return;
            }

            Target.Harvest(gameObject, bag);
        }

        /// <summary>
        /// The closest node with something left in it, within reach.
        /// </summary>
        /// <remarks>
        /// A scene scan every frame, which is honest about what it is: fine for a clearing with a
        /// handful of bushes and wrong for a forest. When that day comes the answer is a trigger
        /// volume or a spatial index, and it will be obvious because the profiler will say so rather
        /// than because somebody predicted it.
        /// </remarks>
        private Harvestable Nearest()
        {
            Harvestable best = null;
            float bestDistance = _reach * _reach;

            var nodes = Object.FindObjectsByType<Harvestable>(FindObjectsSortMode.None);
            for (int i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                if (node == null || node.IsDepleted) continue;

                var offset = node.transform.position - transform.position;
                offset.y = 0f;

                float distance = offset.sqrMagnitude;
                if (distance > bestDistance) continue;

                bestDistance = distance;
                best = node;
            }

            return best;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.9f, 0.4f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, _reach);
        }
    }
}
