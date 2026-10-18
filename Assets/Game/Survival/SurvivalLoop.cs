using Graves.Core;
using Graves.Core.Saving;
using Graves.Plugins.Crafting;
using Graves.Plugins.Gathering;
using Graves.Plugins.Input;
using Graves.Plugins.Inventory;
using UnityEngine;

namespace Game.Survival
{
    /// <summary>
    /// The whole game: gather wood and stone, craft an axe, and have all of it survive a restart.
    /// </summary>
    /// <remarks>
    /// <b>This is a game built on the framework, not a part of the framework.</b> It lives in its own
    /// assembly outside <c>Assets/Graves</c>, references the core and four plugins, and nothing in the
    /// framework references it — which is the boundary the framework claims and this is the first
    /// thing to actually test it.
    ///
    /// <b>It is deliberately one file.</b> The point is not the game; it is finding out what the
    /// framework makes hard. One file is enough to answer that, and anything more starts hiding the
    /// answer behind its own architecture.
    ///
    /// <b>What it does not do.</b> No inventory screen, no world-space prompts, no menus, no
    /// progression. It draws four lines of text with <c>OnGUI</c>, which is the smallest possible
    /// window onto whether the loop closed.
    /// </remarks>
    public class SurvivalLoop : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The recipe that ends the loop. Craft it and the game is complete.")]
        private RecipeDefinition _goal;

        [SerializeField]
        [Tooltip("Save slot used by the F5 / F9 keys.")]
        private string _slot = "survival";

        private GameEntity _player;
        private Gatherer _gatherer;
        private InputSource _input;
        private string _message = "Walk to a sapling or an outcrop and press E.";
        private float _messageUntil;
        private bool _complete;

        private void Awake()
        {
            _gatherer = Object.FindFirstObjectByType<Gatherer>();
            if (_gatherer == null)
            {
                Debug.LogError("[Survival] No gatherer in the scene. There is nobody to play as.", this);
                enabled = false;
                return;
            }

            _player = _gatherer.GetComponent<GameEntity>();
            _input = _gatherer.GetComponent<InputSource>();
        }

        // Subscribed here rather than in Awake so the loop stops hearing about the world when it is
        // switched off - the same rule the framework's own components follow.
        private void OnEnable() => GameEvents.Subscribe<ItemGathered>(OnGathered);

        private void OnDisable() => GameEvents.Unsubscribe<ItemGathered>(OnGathered);

        /// <summary>
        /// Reports what was picked up.
        /// </summary>
        /// <remarks>
        /// <b>Through the bus, not by asking the gatherer.</b> This could have polled the inventory
        /// every frame and diffed it, which is what a game without an event system has to do. It is
        /// worth noticing that the bus earns its place here on its first day: the game listens to
        /// gathering without gathering knowing the game exists.
        /// </remarks>
        private void OnGathered(ItemGathered _what)
        {
            Say(_what.Count + " x " + _what.Item.DisplayName +
                (_what.Depleted ? " - that one is stripped bare." : "."));
        }

        private void Update()
        {
            if (_input == null || _player == null) return;

            var state = _input.Read();

            // F5 and F9 rather than the input struct, because saving is not a thing the character
            // does. Putting it in InputState would mean every game that never saves still carries a
            // field for it.
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.f5Key.wasPressedThisFrame)
                {
                    SaveSystem.Save(_slot, "Survival loop");
                    Say("Saved.");
                }

                if (keyboard.f9Key.wasPressedThisFrame)
                {
                    var result = SaveSystem.Load(_slot);
                    Say(result.Loaded
                        ? "Loaded. " + result.EntitiesRestored + " entities restored."
                        : "Nothing saved yet.");
                }

                if (keyboard.cKey.wasPressedThisFrame) TryCraft();
            }
        }

        private void TryCraft()
        {
            if (_goal == null)
            {
                Say("No recipe assigned, so there is nothing to make.");
                return;
            }

            if (!Workbench.CanCraft(_goal, _player.gameObject, out var reason))
            {
                Say(reason);
                return;
            }

            var made = Workbench.Craft(_goal, _player.gameObject);
            if (made == null)
            {
                Say("Crafting failed.");
                return;
            }

            _complete = true;
            Say("Made a " + _goal.Result.DisplayName + ". The loop is closed.");
        }

        private void Say(string _what)
        {
            _message = _what;
            _messageUntil = Time.time + 4f;
        }

        /// <summary>
        /// Four lines of text.
        /// </summary>
        /// <remarks>
        /// <c>OnGUI</c> because a real interface is a plugin nobody has written, and writing one to
        /// prove a gathering loop works would be building the thing this exercise exists to avoid.
        /// </remarks>
        private void OnGUI()
        {
            var bag = _player != null && _player.Instance != null
                ? _player.Instance.GetState<InventoryState>(InventoryState.Key)
                : null;

            var style = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            GUILayout.BeginArea(new Rect(16f, 16f, 640f, 200f));

            GUILayout.Label("WASD to move.  E to gather.  C to craft.  F5 save, F9 load.", style);

            if (bag != null)
            {
                int wood = 0, stone = 0;
                foreach (var slot in bag.Slots)
                {
                    if (slot == null || slot.IsEmpty || slot.Definition == null) continue;
                    if (slot.Definition.DisplayName == "Wood") wood += slot.Count;
                    if (slot.Definition.DisplayName == "Stone") stone += slot.Count;
                }

                GUILayout.Label("Wood " + wood + "    Stone " + stone, style);
            }

            if (_gatherer != null)
            {
                GUILayout.Label(_gatherer.Target != null
                    ? "In reach: " + _gatherer.Target.name + " (" + _gatherer.Target.Remaining + " left)"
                    : "Nothing in reach.", style);
            }

            if (Time.time < _messageUntil || _complete) GUILayout.Label(_message, style);

            GUILayout.EndArea();
        }
    }
}
