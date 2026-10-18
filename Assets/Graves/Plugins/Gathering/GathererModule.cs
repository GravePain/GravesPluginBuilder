using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Gathering
{
    /// <summary>Says that this kind of entity can gather from nodes.</summary>
    /// <remarks>
    /// One field, because gathering has one number a designer cares about: how far you can reach.
    /// Everything else about a harvest is a property of the node, not of the person pulling at it.
    /// </remarks>
    [CreateAssetMenu(fileName = "GathererModule", menuName = "Graves/Modules/Gatherer")]
    public class GathererModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("How close a node must be to be harvested, in metres.")]
        [Min(0f)] private float _reach = 2.5f;

        /// <summary>How far this kind of entity can reach.</summary>
        public float Reach => _reach;

        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var host = _entity.gameObject;

            var gatherer = host.GetComponent<Gatherer>();
            if (gatherer == null) gatherer = host.AddComponent<Gatherer>();

            gatherer.Configure(_reach);
        }
    }
}
