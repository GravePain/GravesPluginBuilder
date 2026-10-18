using System;

namespace Graves.Core
{
    /// <summary>
    /// Declares which sidebar category a definition type appears under in the authoring window, and
    /// what the "create new" button offers.
    /// </summary>
    /// <remarks>
    /// This is how a plugin gets editor presence without the core being edited. The window discovers
    /// every <see cref="GameDefinition"/> subclass carrying this attribute by reflection, so adding a
    /// plugin adds a category, and deleting the plugin removes it - with no core change either way.
    /// A definition type without this attribute is simply not offered in the window; it still works.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class DefinitionCategoryAttribute : Attribute
    {
        /// <summary>Sidebar heading, e.g. "Entities".</summary>
        public string Category { get; }

        /// <summary>Sort position. Lower sorts first; equal values fall back to alphabetical.</summary>
        public int Order { get; set; }

        /// <summary>Declares the sidebar category this definition type appears under.</summary>
        public DefinitionCategoryAttribute(string _category)
        {
            Category = _category;
        }
    }
}
