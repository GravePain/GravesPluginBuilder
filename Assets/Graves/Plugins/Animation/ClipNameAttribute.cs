using UnityEngine;

namespace Graves.Plugins.Animation
{
    /// <summary>
    /// Marks a string field as naming an authored animation clip, so it can be chosen from a list
    /// rather than typed.
    /// </summary>
    /// <remarks>
    /// <b>A typed clip name is a silent no-op waiting to happen.</b> The runner matches on the name,
    /// so a typo produces a skill that fires, spends its cooldown, deals its damage and animates
    /// nothing. Nothing errors, because nothing is wrong: it asked for a clip that does not exist and
    /// was told so, once, in a warning nobody was watching for.
    ///
    /// Graves hit the same class of failure in a shipped game, with an animator trigger matched by
    /// exact string, and his fix is the one worth copying: present the field as a dropdown off the
    /// vocabulary, so <b>the mistake becomes unreachable rather than merely detectable</b>. A value
    /// already outside the list is shown and flagged, never silently snapped to something valid,
    /// because quietly changing authored data is worse than showing that it is wrong.
    ///
    /// The attribute lives in the runtime assembly and does nothing there. Only the editor reads it,
    /// which is how a plugin gets better authoring without an editor dependency reaching into a
    /// build.
    /// </remarks>
    public class ClipNameAttribute : PropertyAttribute
    {
    }
}
