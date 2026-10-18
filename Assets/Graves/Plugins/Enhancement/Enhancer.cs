using Graves.Core;
using UnityEngine;

namespace Graves.Plugins.Enhancement
{
    /// <summary>
    /// Improving a particular object by one step.
    /// </summary>
    /// <remarks>
    /// <b>It works on an instance, never on a definition.</b> Enhancement belongs to one object -
    /// this sword, this arrow - and applying it to a definition would improve every copy in the world
    /// at once. That distinction is the reason instances exist.
    ///
    /// <b>No cost, no chance of failure, no ceremony.</b> A forge decides what enhancement costs and
    /// whether it can go wrong; a shipped example of one charges materials and currency, rolls
    /// against a curve, and can destroy the item. All of that is a game's economy. This is the part
    /// underneath it that every economy needs and none of them should have to reimplement.
    /// </remarks>
    public static class Enhancer
    {
        /// <summary>True when this object can be improved at least once more.</summary>
        public static bool CanEnhance(GameInstance _instance)
        {
            var module = ModuleFor(_instance);
            if (module == null) return false;

            var state = _instance.GetState<EnhancementState>();
            return state == null || state.Level < module.MaxLevel;
        }

        /// <summary>The current level of an object, which is zero when it has never been improved.</summary>
        public static int LevelOf(GameInstance _instance)
        {
            var state = _instance?.GetState<EnhancementState>();
            return state == null ? 0 : Mathf.Max(0, state.Level);
        }

        /// <summary>
        /// Improves an object by one step and returns its new level, or the current level when it
        /// cannot be improved.
        /// </summary>
        /// <remarks>
        /// <b>This is the moment an object stops being interchangeable.</b> Before the call it may sit
        /// in a stack with others; after it, the stacking rule will refuse to merge it with anything,
        /// because it now carries state. Anything enhancing one of a stack must therefore split it out
        /// first - the inventory has an operation for that, and it exists because of this.
        /// </remarks>
        public static int Enhance(GameInstance _instance)
        {
            if (_instance == null) return 0;

            var module = ModuleFor(_instance);
            if (module == null)
            {
                Debug.LogWarning("[Enhancement] '" + _instance + "' cannot be enhanced - its " +
                                 "definition carries no enhanceable module.");
                return 0;
            }

            var state = _instance.GetOrAddState<EnhancementState>();
            if (state.Level >= module.MaxLevel) return state.Level;

            state.Level++;
            return state.Level;
        }

        private static EnhanceableModule ModuleFor(GameInstance _instance)
        {
            var definition = _instance?.Definition;
            return definition == null ? null : definition.GetModule<EnhanceableModule>();
        }
    }
}
