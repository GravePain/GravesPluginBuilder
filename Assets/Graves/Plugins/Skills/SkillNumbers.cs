using Graves.Core;

namespace Graves.Plugins.Skills
{
    /// <summary>
    /// Finds the module carrying a skill's numbers, preferring the bound item over the skill itself.
    /// </summary>
    /// <remarks>
    /// <b>One resolution order, in one place.</b> Every kind of number a skill has - its cooldown,
    /// its damage, whatever a plugin adds next - answers the same question: does an item say, and if
    /// not, does the skill say. Written once so a new kind of number cannot accidentally answer it
    /// differently.
    ///
    /// <b>The item wins when both exist.</b> That is the point of binding one: the item is the thing
    /// that gets upgraded, enchanted and rolled, so its numbers must be able to override the design's
    /// defaults. A skill authoring its own numbers and then being bound to an item has not lost
    /// anything - those numbers become the fallback for anyone casting it without the item.
    ///
    /// <b>Null is a real answer and callers must handle it.</b> A skill whose numbers are nowhere is
    /// not a skill with zero damage; it is a skill nobody finished. The difference matters, because
    /// zero is a value somebody might have chosen.
    /// </remarks>
    public static class SkillNumbers
    {
        /// <summary>
        /// The numbers module of the given kind for this skill, or null when neither the bound item
        /// nor the skill carries one.
        /// </summary>
        public static T For<T>(SkillDefinition _skill) where T : DefinitionModule
        {
            if (_skill == null) return null;

            var item = _skill.NumbersFrom;
            if (item != null)
            {
                var fromItem = item.GetModule<T>();
                if (fromItem != null) return fromItem;
            }

            return _skill.GetModule<T>();
        }

        /// <summary>Where a skill's numbers of a given kind actually came from, for a message.</summary>
        /// <remarks>
        /// Used in the one place it matters: telling an author that a skill has no numbers at all,
        /// and whether an item was consulted. "No timing module on the skill or on Rusty Sword" is a
        /// different problem from "no timing module and no item bound".
        /// </remarks>
        public static string SourceOf<T>(SkillDefinition _skill) where T : DefinitionModule
        {
            if (_skill == null) return "no skill";

            var item = _skill.NumbersFrom;
            if (item != null && item.GetModule<T>() != null) return "'" + item.DisplayName + "'";
            if (_skill.GetModule<T>() != null) return "the skill itself";

            return item != null
                 ? "neither the skill nor '" + item.DisplayName + "'"
                 : "the skill, and no item is bound";
        }
    }
}
