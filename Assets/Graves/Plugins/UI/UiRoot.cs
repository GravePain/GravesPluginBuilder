using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Graves.Plugins.UI
{
    /// <summary>
    /// The canvas everything else hangs off, made on demand and shared.
    /// </summary>
    /// <remarks>
    /// <b>Built in code rather than shipped as a prefab.</b> A prefab would be an asset the plugin
    /// carries, and the moment a game wants a different canvas scale or a different sort order it
    /// has to edit that asset - which means the plugin is no longer replaceable without losing the
    /// edit. Everything here is a handful of components with no authored content at all.
    ///
    /// <b>One root, found rather than assigned.</b> Nothing holds a reference to it and nothing
    /// drags it into a field. That is the same rule the motor and the input source follow, and it is
    /// what lets a HUD element exist without knowing whether a canvas has been made yet.
    ///
    /// <b>It makes an event system if none exists.</b> Without one, nothing in uGUI receives a click
    /// and every button silently does nothing, which is a confusing first ten minutes for anybody
    /// who has not met it before.
    /// </remarks>
    [DisallowMultipleComponent]
    public class UiRoot : MonoBehaviour
    {
        private static UiRoot _instance;

        /// <summary>The canvas to parent interface elements to, made on first use.</summary>
        public static UiRoot Instance
        {
            get
            {
                if (_instance != null) return _instance;

                _instance = FindFirstObjectByType<UiRoot>();
                if (_instance != null) return _instance;

                var host = new GameObject("Graves UI");
                _instance = host.AddComponent<UiRoot>();
                _instance.Build();
                return _instance;
            }
        }

        /// <summary>Where screen-space elements are parented.</summary>
        public RectTransform Screen { get; private set; }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // Two roots means two canvases, and elements would split between them with no rule
                // about which is in front. Reported rather than silently merged.
                Debug.LogWarning("[UI] A second UI root appeared on '" + name + "'. The first one is " +
                                 "still in charge.", this);
                return;
            }

            _instance = this;
            if (Screen == null) Build();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>
        /// Makes the canvas, once.
        /// </summary>
        /// <remarks>
        /// <b>Guarded, because adding the component builds it too.</b> Adding a MonoBehaviour runs its
        /// Awake immediately, so the code that created this object had already built it by the time
        /// it asked to. The second pass then failed to add a canvas that was already there, got null
        /// back, and threw - inside Awake, where an exception is swallowed and leaves an entity
        /// half-attached with no obvious cause.
        ///
        /// Idempotent rather than carefully sequenced, because the ordering that makes it safe is not
        /// visible from either end.
        /// </remarks>
        private void Build()
        {
            if (Screen != null) return;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Below anything a game adds later, so a plugin's HUD never covers a game's own screens
            // by accident. A game that wants this in front says so; a plugin should not assume.
            canvas.sortingOrder = 0;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            Screen = GetComponent<RectTransform>();

            if (EventSystem.current == null)
            {
                var events = new GameObject("Event System");
                events.AddComponent<EventSystem>();
                events.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                events.transform.SetParent(transform, false);
            }
        }
    }
}
