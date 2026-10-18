using UnityEngine;
using UnityEngine.UI;

namespace Graves.Plugins.UI
{
    /// <summary>
    /// A filled bar with a label. Health, experience, a cast timer, a loading progress.
    /// </summary>
    /// <remarks>
    /// <b>It knows nothing about what it is showing.</b> A fraction between zero and one and some
    /// text, and that is the whole contract. Health, experience and anything else are bound to it
    /// from their own assemblies, so this plugin never learns what a hit point is.
    ///
    /// <b>Made in code, with no sprite.</b> A default white image tinted by colour needs no asset
    /// and no import settings, and it means the plugin ships nothing a game has to keep. A game
    /// wanting a decorated bar replaces the images on the objects this makes, or replaces this.
    /// </remarks>
    [DisallowMultipleComponent]
    public class Bar : MonoBehaviour
    {
        private Image _fill;
        private Text _label;
        private RectTransform _rect;

        /// <summary>The rect, for callers that want to place or resize this.</summary>
        public RectTransform Rect => _rect;

        /// <summary>
        /// Builds a bar under the UI root and returns it.
        /// </summary>
        /// <remarks>
        /// Anchored from the corner given rather than centred, because a HUD is built from screen
        /// edges and a bar that moves when the window resizes is worse than no bar.
        /// </remarks>
        public static Bar Create(string _name, Vector2 _anchor, Vector2 _offset, Vector2 _size,
                                 Color _colour)
        {
            var root = UiRoot.Instance;

            var host = new GameObject(_name, typeof(RectTransform));
            host.transform.SetParent(root.Screen, false);

            var bar = host.AddComponent<Bar>();
            bar._rect = host.GetComponent<RectTransform>();
            bar._rect.anchorMin = _anchor;
            bar._rect.anchorMax = _anchor;
            bar._rect.pivot = _anchor;
            bar._rect.anchoredPosition = _offset;
            bar._rect.sizeDelta = _size;

            var backing = new GameObject("Background", typeof(RectTransform));
            backing.transform.SetParent(host.transform, false);
            var backingImage = backing.AddComponent<Image>();
            backingImage.color = new Color(0f, 0f, 0f, 0.55f);
            Stretch(backing.GetComponent<RectTransform>());

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(host.transform, false);
            bar._fill = fill.AddComponent<Image>();
            bar._fill.color = _colour;
            bar._fill.type = Image.Type.Filled;
            bar._fill.fillMethod = Image.FillMethod.Horizontal;
            bar._fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            Stretch(fill.GetComponent<RectTransform>());

            var label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(host.transform, false);
            bar._label = label.AddComponent<Text>();
            bar._label.alignment = TextAnchor.MiddleCenter;
            bar._label.color = Color.white;
            bar._label.fontSize = 16;

            // The built-in font, because shipping one would be an asset and a licence. It is ugly and
            // it is present in every Unity install, which is the right trade for a framework.
            bar._label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Stretch(label.GetComponent<RectTransform>());

            // Nothing on a bar should eat a click. Without this, a HUD element sitting over the world
            // silently swallows input and the game stops responding where the bar happens to be.
            backingImage.raycastTarget = false;
            bar._fill.raycastTarget = false;
            bar._label.raycastTarget = false;

            return bar;
        }

        /// <summary>Sets how full the bar is, from zero to one.</summary>
        public void SetFraction(float _fraction)
        {
            if (_fill == null) return;
            _fill.fillAmount = Mathf.Clamp01(_fraction);
        }

        /// <summary>Sets the text across the bar. Empty hides it.</summary>
        public void SetLabel(string _text)
        {
            if (_label == null) return;
            _label.text = _text ?? string.Empty;
        }

        /// <summary>Changes the fill colour, for a bar that warns as it empties.</summary>
        public void SetColour(Color _colour)
        {
            if (_fill != null) _fill.color = _colour;
        }

        private static void Stretch(RectTransform _rect)
        {
            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.one;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
