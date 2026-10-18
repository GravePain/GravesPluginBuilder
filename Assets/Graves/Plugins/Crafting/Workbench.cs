using Graves.Core;
using Graves.Plugins.Inventory;
using UnityEngine;

namespace Graves.Plugins.Crafting
{
    /// <summary>
    /// Turning materials into a thing, against one inventory.
    /// </summary>
    /// <remarks>
    /// <b>Static, and taking the inventory as an argument.</b> Crafting is an operation on a bag
    /// rather than a property of a creature, and writing it this way means a chest, a shop, a
    /// campfire or a monster can craft with no component and no ceremony. What decides that a
    /// creature MAY craft is a separate question, answered by the crafting module.
    ///
    /// <b>Nothing here takes time.</b> A progress bar, a station to stand at, a chance of failure and
    /// a cost in coin are all real, all common, and all decisions a particular game makes. This is
    /// the part underneath them: the exchange itself, which every version of crafting performs and
    /// none of them should have to reimplement.
    /// </remarks>
    public static class Workbench
    {
        /// <summary>
        /// Whether this recipe could be made right now, and if not, why not in words a player could
        /// be shown.
        /// </summary>
        /// <remarks>
        /// The reason is returned rather than logged, so a button can grey itself out and say what is
        /// missing. A crafting button that simply does nothing is indistinguishable from a broken one.
        /// </remarks>
        /// <summary>
        /// Falls back to the crafter's own bag when no inventory was named.
        /// </summary>
        /// <remarks>
        /// <b>This exists because the obvious call was a trap.</b> The inventories are a <c>params</c>
        /// argument, so <c>CanCraft(recipe, player, out reason)</c> compiles, reads correctly, and
        /// always fails with "No inventory" - and the author has no reason to suspect the argument
        /// they did not write. The first game built on this framework hit it within an hour.
        ///
        /// Naming inventories explicitly is still supported and is what a shared chest or a party
        /// pool needs. Not naming any now means the obvious thing: craft from what the crafter is
        /// carrying.
        ///
        /// Asked for by key rather than by type, because a purse is an inventory too and the typed
        /// lookup would hand back whichever the module order happened to put first.
        /// </remarks>
        private static InventoryState[] Resolve(GameObject _crafter, InventoryState[] _given)
        {
            if (_given != null && _given.Length > 0) return _given;
            if (_crafter == null) return System.Array.Empty<InventoryState>();

            var entity = _crafter.GetComponentInParent<GameEntity>();
            var instance = entity != null ? entity.Instance : null;
            var bag = instance != null ? instance.GetState<InventoryState>(InventoryState.Key) : null;

            return bag != null ? new[] { bag } : System.Array.Empty<InventoryState>();
        }

        public static bool CanCraft(RecipeDefinition _recipe, GameObject _crafter, out string _reason,
                                   params InventoryState[] _from)
        {
            _reason = null;

            if (_recipe == null) { _reason = "No recipe."; return false; }

            _from = Resolve(_crafter, _from);
            if (_from.Length == 0)
            {
                _reason = "Nothing to craft from: the crafter has no inventory, and none was given.";
                return false;
            }

            if (_recipe.Result == null)
            {
                _reason = "This recipe produces nothing. Its result was probably deleted.";
                return false;
            }

            // Asked before the materials, because "you must be at a forge" is a more useful thing to
            // be told than "you need three iron" when both are true and only one is fixable here.
            var modules = _recipe.Modules;
            for (int i = 0; i < modules.Count; i++)
            {
                if (!(modules[i] is ICraftingRequirement requirement)) continue;
                if (requirement.IsMet(_recipe, _crafter, out var why)) continue;

                _reason = string.IsNullOrEmpty(why) ? "Requirement not met." : why;
                return false;
            }

            foreach (var ingredient in _recipe.Ingredients)
            {
                if (ingredient == null || ingredient.Item == null)
                {
                    _reason = "This recipe lists an ingredient that no longer exists.";
                    return false;
                }

                // Ordinary ones only. An improved or enchanted copy is somebody's work and is never
                // material, however much the recipe would technically accept it.
                int held = 0;
                for (int i = 0; i < _from.Length; i++)
                {
                    if (_from[i] != null) held += _from[i].CountUnmodifiedOf(ingredient.Item.Uid);
                }
                if (held >= ingredient.Count) continue;

                _reason = "Needs " + ingredient.Count + " " + ingredient.Item.DisplayName +
                          " and you have " + held + ".";
                return false;
            }

            // Asked before anything is consumed. Discovering afterwards that the result will not fit
            // costs the materials and cannot be undone.
            if (RoomFor(_recipe.Result, _recipe.ResultCount, _from) == null)
            {
                _reason = "No room for " + _recipe.ResultCount + " " + _recipe.Result.DisplayName + ".";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Makes the thing: consumes the materials and puts the result in the same inventory. Returns
        /// the new instance, or null when nothing was made.
        /// </summary>
        /// <remarks>
        /// <b>Everything is checked before anything is taken.</b> Consuming as it goes and failing
        /// halfway would leave a player short of materials with nothing to show, and there is no
        /// sensible way to put them back once other slots have shifted.
        ///
        /// <b>The result is a fresh instance.</b> It has never existed before, so it gets its own
        /// identity - which is also what lets it be enhanced, enchanted or named later without
        /// affecting anything else made from the same recipe.
        /// </remarks>
        public static GameInstance Craft(RecipeDefinition _recipe, GameObject _crafter,
                                        params InventoryState[] _from)
        {
            _from = Resolve(_crafter, _from);

            if (!CanCraft(_recipe, _crafter, out var reason, _from))
            {
                Debug.LogWarning("[Crafting] '" + (_recipe != null ? _recipe.DisplayName : "?") +
                                 "' was not made: " + reason);
                return null;
            }

            foreach (var ingredient in _recipe.Ingredients)
            {
                int taken = 0;
                for (int i = 0; i < _from.Length && taken < ingredient.Count; i++)
                {
                    if (_from[i] == null) continue;
                    taken += _from[i].RemoveUnmodified(ingredient.Item.Uid, ingredient.Count - taken);
                }

                if (taken == ingredient.Count) continue;

                // Cannot happen: the same counts were checked a moment ago against the same bag. If
                // it ever does, something mutated the inventory in between, and carrying on would
                // produce an item that was never paid for.
                Debug.LogError("[Crafting] '" + _recipe.DisplayName + "' consumed " + taken + " of " +
                               ingredient.Count + " " + ingredient.Item.DisplayName +
                               ". The inventory changed between the check and the exchange, and the " +
                               "materials taken so far are gone.");
                return null;
            }

            var made = new GameInstance(_recipe.Result);
            var destination = RoomFor(_recipe.Result, _recipe.ResultCount, _from);
            int placed = destination == null ? 0 : destination.Add(made, _recipe.ResultCount);

            if (placed < _recipe.ResultCount)
            {
                Debug.LogError("[Crafting] '" + _recipe.DisplayName + "' made " + _recipe.ResultCount +
                               " but only " + placed + " fit, after room was confirmed. The rest are " +
                               "lost and the materials are already spent.");
            }

            return made;
        }

        /// <summary>
        /// The first container with room for this much of something, or null when none has.
        /// </summary>
        /// <remarks>
        /// <b>Several containers, because money is an item.</b> A recipe costing three iron and ten
        /// coins is one ingredient list, and the coins live in a purse while the iron lives in a bag.
        /// Passing both means crafting never had to learn what a currency is - it takes ingredients
        /// from wherever they are, which is the whole reason for treating money as an ordinary item.
        ///
        /// The result goes to the first container that will take it, so the caller decides where
        /// output lands by the order it passes them. A bag first and a purse second puts crafted
        /// goods in the bag and never in the purse, which is almost always what is wanted.
        /// </remarks>
        private static InventoryState RoomFor(ItemDefinition _what, int _quantity,
                                              InventoryState[] _containers)
        {
            for (int i = 0; i < _containers.Length; i++)
            {
                if (_containers[i] != null && _containers[i].HasRoomFor(_what, _quantity))
                    return _containers[i];
            }
            return null;
        }
    }
}
