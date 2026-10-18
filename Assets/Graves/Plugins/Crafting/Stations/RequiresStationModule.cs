using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Crafting.Stations
{
    /// <summary>
    /// A recipe that can only be made at a particular kind of station.
    /// </summary>
    /// <remarks>
    /// <b>This assembly is what a game installs when crafting happens somewhere.</b> Delete it and
    /// crafting still works and recipes are still made; they simply stop caring where you stand. A
    /// game where you craft from your pack never inherits the concept of a place.
    ///
    /// <b>A recipe with no station assigned fails rather than passing.</b> An unfinished requirement
    /// that silently allows everything is worse than one that blocks: the first ships, the second
    /// gets fixed. The message says which of the two it is.
    /// </remarks>
    [CreateAssetMenu(fileName = "RequiresStationModule", menuName = "Graves/Modules/Recipe Requires Station")]
    public class RequiresStationModule : DefinitionModule, ICraftingRequirement
    {
        [SerializeField]
        [Tooltip("The kind of station this must be made at.")]
        private CraftingStationDefinition _station;

        [SerializeField]
        [Tooltip("How close the crafter must be, in metres.")]
        [Min(0.1f)] private float _range = 4f;

        [SerializeField]
        [Tooltip("Shown when the requirement is not met. Left blank, a sentence is made from the " +
                 "station name.")]
        private string _message;

        /// <inheritdoc />
        public bool IsMet(RecipeDefinition _recipe, GameObject _crafter, out string _reason)
        {
            if (_station == null)
            {
                _reason = "This recipe names no station, so it can never be made.";
                return false;
            }

            if (_crafter == null)
            {
                // A chest or a script crafting with nobody standing anywhere. Reported plainly rather
                // than allowed, because a station requirement that only applies to players is a rule
                // that will be discovered by an exploit.
                _reason = "Nobody is at a " + _station.DisplayName + ".";
                return false;
            }

            if (CraftingStation.NearestOfKind(_crafter.transform.position, _station, _range) != null)
            {
                _reason = null;
                return true;
            }

            _reason = string.IsNullOrWhiteSpace(_message)
                    ? "You must be at a " + _station.DisplayName + "."
                    : _message;
            return false;
        }
    }
}
