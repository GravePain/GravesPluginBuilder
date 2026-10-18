using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Graves.Plugins.Animation.Editor
{
    /// <summary>
    /// Draws a clip-name field as a dropdown of every clip authored anywhere in the project.
    /// </summary>
    /// <remarks>
    /// <b>The vocabulary is project-wide, not per-definition, and that is not a shortcut.</b> A skill
    /// naming a clip is authored on the skill, while the clips themselves are authored on whatever
    /// entity performs it - so the two are never siblings and a local lookup would find nothing. The
    /// honest scope is every name any animation module offers, which is the same shape as the
    /// vocabulary lists in Graves' own project: a set of legal words, checked centrally.
    ///
    /// <b>An unknown value is shown and flagged, never corrected.</b> Snapping an authored string to
    /// the nearest legal one would quietly change data somebody wrote on purpose, and would hide the
    /// case this exists to catch. A name that is not in the list appears in the list, marked, and the
    /// field says so underneath.
    ///
    /// <b>This is also the first editor assembly a plugin here has needed.</b> The plugin guide has
    /// promised since before any of this was built that a plugin may add a property drawer and it
    /// will be picked up. Nothing had tested that. It works.
    /// </remarks>
    [CustomPropertyDrawer(typeof(ClipNameAttribute))]
    public class ClipNameDrawer : PropertyDrawer
    {
        /// <summary>
        /// The entry shown when the field is empty.
        /// </summary>
        /// <remarks>
        /// A magic string, and a clip legitimately named "(none)" would be indistinguishable from an
        /// empty field and would silently clear the value when chosen. Left as it is: naming a clip
        /// that is a known edge case, and the alternative - a nullable wrapper or a separate toggle -
        /// costs more in the common case than the mistake costs in the rare one. Recorded so the next
        /// reader knows it was seen rather than missed.
        /// </remarks>
        private const string Unset = "(none)";

        /// <inheritdoc />
        public override VisualElement CreatePropertyGUI(SerializedProperty _property)
        {
            if (_property.propertyType != SerializedPropertyType.String)
            {
                return new Label(_property.displayName + ": a clip name must be a string.");
            }

            var container = new VisualElement();

            var names = GatherClipNames();
            var current = _property.stringValue ?? string.Empty;

            // Matched the way the RUNNER matches, which is case-insensitively. Gathering the
            // vocabulary with one comparer and then testing membership with another is how a warning
            // ends up firing on something that works perfectly - and a warning that cries wolf is
            // worse than no warning at all, because the next real one gets ignored.
            var canonical = Canonical(names, current);
            bool unknown = !string.IsNullOrEmpty(current) && canonical == null;

            if (unknown) names.Insert(0, current);
            if (!names.Contains(Unset)) names.Insert(0, Unset);

            // The vocabulary entry this value resolves to, rather than the value as typed. Nothing is
            // written back: the asset keeps exactly the string it was authored with, and only an
            // actual choice by a person changes it. A drawer that corrects data while merely being
            // looked at is the usual way this goes wrong.
            var shown = string.IsNullOrEmpty(current) ? Unset : (canonical ?? current);

            int index = names.IndexOf(shown);
            if (index < 0) index = 0;

            var field = new PopupField<string>(_property.displayName, names, index);
            field.tooltip = "Every clip name authored on any animation module in the project. Typing " +
                            "this by hand is how a skill ends up firing and animating nothing.";

            var warning = new Label(unknown
                ? "No animation module offers a clip called '" + current + "'. It is kept as authored, " +
                  "and it will do nothing until a clip is given that name."
                : string.Empty)
            {
                style =
                {
                    display = unknown ? DisplayStyle.Flex : DisplayStyle.None,
                    color = new Color(0.95f, 0.6f, 0.3f),
                    whiteSpace = WhiteSpace.Normal,
                    paddingLeft = 4f, paddingBottom = 4f, fontSize = 11f
                }
            };

            field.RegisterValueChangedCallback(change =>
            {
                var chosen = change.newValue == Unset ? string.Empty : change.newValue;
                _property.stringValue = chosen;
                _property.serializedObject.ApplyModifiedProperties();

                bool stillUnknown = !string.IsNullOrEmpty(chosen) &&
                                    Canonical(GatherClipNames(), chosen) == null;
                warning.style.display = stillUnknown ? DisplayStyle.Flex : DisplayStyle.None;
            });

            container.Add(field);
            container.Add(warning);
            return container;
        }

        /// <summary>
        /// The vocabulary entry a value resolves to, or null when nothing offers it.
        /// </summary>
        /// <remarks>
        /// Case-insensitive, because <c>AnimationModule.Find</c> is. Two names differing only in case
        /// are the same name to the runtime, so they must be the same name here too.
        /// </remarks>
        private static string Canonical(List<string> _names, string _value)
        {
            if (string.IsNullOrEmpty(_value)) return null;

            for (int i = 0; i < _names.Count; i++)
            {
                if (string.Equals(_names[i], _value, System.StringComparison.OrdinalIgnoreCase))
                    return _names[i];
            }
            return null;
        }

        /// <summary>
        /// Every distinct clip name authored on any animation module in the project, sorted.
        /// </summary>
        /// <remarks>
        /// Read from disk each time the field is drawn rather than cached. Drawing happens when a
        /// person selects something, which is rare and slow anyway, and a cache here would go stale
        /// the moment somebody added a clip in another window - which is exactly when the list needs
        /// to be right.
        ///
        /// <b>Names differing only in case collapse to one entry, deliberately.</b> The runner
        /// matches case-insensitively, so "Attack" and "attack" ARE one name at runtime, and
        /// offering both would suggest a choice that does not exist. The cost is that the dropdown
        /// shows whichever spelling was found first and hides the other; since picking either
        /// produces identical behaviour, that is a display detail rather than a lost option.
        /// </remarks>
        private static List<string> GatherClipNames()
        {
            var names = new SortedSet<string>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(AnimationModule)))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (!(asset is AnimationModule module)) continue;

                    foreach (var clip in module.Clips)
                    {
                        if (clip == null || string.IsNullOrWhiteSpace(clip.Name)) continue;
                        names.Add(clip.Name);
                    }
                }
            }

            return new List<string>(names);
        }
    }
}
