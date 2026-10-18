using System.Collections.Generic;
using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Equipment
{
    /// <summary>
    /// Gives an entity places to wear things.
    /// </summary>
    /// <remarks>
    /// An entity with this module and no slots authored can wear nothing, which is the correct
    /// reading of an empty list rather than a reason to invent a default set. What slots a creature
    /// has is a design decision, and a framework guessing "head, chest, hands" would be making it.
    /// </remarks>
    [CreateAssetMenu(fileName = "EquipmentModule", menuName = "Graves/Modules/Equipment")]
    public class EquipmentModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("The places this entity can wear things. Order is for the authoring window only.")]
        private List<EquipmentSlotDefinition> _slots = new List<EquipmentSlotDefinition>();

        /// <summary>The slots this entity has.</summary>
        public IReadOnlyList<EquipmentSlotDefinition> Slots => _slots;

        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var host = _entity.gameObject;

            var equipment = host.GetComponent<Equipment>();
            if (equipment == null) equipment = host.AddComponent<Equipment>();

            equipment.Configure(_slots);

            var instance = _entity.Instance;
            if (instance == null)
            {
                Debug.LogError("[Equipment] '" + _entity.name + "' has no instance, so what it wears " +
                               "cannot be saved or restored.", _entity);
                return;
            }

            equipment.Bind(instance.GetOrAddState<EquipmentState>());

            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] == null)
                {
                    Debug.LogWarning("[Equipment] '" + _entity.name + "' has an empty slot entry at " +
                                     "index " + i + ". Its definition was probably deleted.", _entity);
                }
            }
        }
    }
}
