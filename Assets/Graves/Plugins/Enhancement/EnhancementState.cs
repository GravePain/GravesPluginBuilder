using System;
using Graves.Core;

namespace Graves.Plugins.Enhancement
{
    /// <summary>
    /// How far this particular object has been improved. Plus one, plus two, and so on.
    /// </summary>
    /// <remarks>
    /// <b>The moment an object carries this it stops being interchangeable.</b> That is the whole
    /// meaning of it: a plus-one arrow and a plain arrow are different things, and the inventory's
    /// stacking rule refuses to merge anything carrying state for exactly this reason.
    ///
    /// <b>Level is stored, not the resulting numbers.</b> A saved bonus is a number frozen at the
    /// moment it was earned; a saved level is re-evaluated against whatever the game currently says a
    /// plus-three is worth. A rebalance should reach every sword already in the world, and it only
    /// can if the sword remembers its level rather than its damage.
    /// </remarks>
    [Serializable]
    public class EnhancementState : InstanceState
    {
        /// <summary>How many times this has been improved. Zero is untouched.</summary>
        public int Level;

        /// <summary>
        /// A stable name for this state in a save file, written as a literal so renaming this class
        /// cannot orphan a save.
        /// </summary>
        public override string StateKey => "graves.enhancement";
    }
}
