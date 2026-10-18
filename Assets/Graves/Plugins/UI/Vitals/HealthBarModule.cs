using Graves.Core;
using Graves.Plugins.Vitals;
using UnityEngine;

namespace Graves.Plugins.UI.Vitals
{
    /// <summary>
    /// Puts this entity health on the screen.
    /// </summary>
    /// <remarks>
    /// <b>Authored on the entity whose health is shown, which answers the question a heads-up display
    /// usually struggles with.</b> A screen showing "the player health" has to find the player, and
    /// every way of doing that is a guess - a tag, a singleton, a search for whoever has an input
    /// source. Here the entity says so itself, and a game that wants two health bars for two
    /// characters puts the module on both.
    ///
    /// <b>No messaging was needed to build this, and that was not the prediction.</b> Before any of
    /// this framework existed it was recorded that a health bar reacting to damage without the
    /// interface referencing vitals would be what finally earned an event bus. It did not. The
    /// binding holds the health component it was handed and reads a fraction; nothing is broadcast,
    /// nothing subscribes, and the interface plugin references vitals openly rather than pretending
    /// not to.
    /// </remarks>
    [CreateAssetMenu(fileName = "HealthBarModule", menuName = "Graves/Modules/Health Bar")]
    public class HealthBarModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("Where on screen, from the bottom-left corner of the screen in pixels.")]
        private Vector2 _offset = new Vector2(24f, -24f);

        [SerializeField]
        [Tooltip("Size of the bar in pixels.")]
        private Vector2 _size = new Vector2(320f, 26f);

        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var health = _entity.GetComponent<Health>();
            if (health == null)
            {
                Debug.LogError("[UI] " + _entity.name + " has a health bar but no vitals, so there " +
                               "is nothing to show.", _entity);
                return;
            }

            // Anchored to the top-left, which is where a player expects their own health and where it
            // cannot be pushed off screen by a resize.
            var bar = Bar.Create(_entity.name + " Health", new Vector2(0f, 1f), _offset, _size,
                                 new Color(0.75f, 0.2f, 0.2f, 0.95f));

            var binding = _entity.GetComponent<HealthBarBinding>();
            if (binding == null) binding = _entity.gameObject.AddComponent<HealthBarBinding>();

            binding.Initialise(health, bar);
        }
    }
}
