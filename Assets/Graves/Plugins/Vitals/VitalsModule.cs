using Graves.Core;
using UnityEngine;
using Graves.Core;

namespace Graves.Plugins.Vitals
{
    /// <summary>
    /// Gives an entity health: how much it has, and whether it starts full.
    /// </summary>
    /// <remarks>
    /// This is the module that makes a thing killable, and it is the first one whose state genuinely
    /// has to survive a save. An entity without it cannot be hurt at all, which is the right
    /// behaviour for scenery and for anything the game does not want the player attacking.
    /// </remarks>
    [CreateAssetMenu(fileName = "VitalsModule", menuName = "Graves/Modules/Vitals")]
    public class VitalsModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("Maximum health. The second field adds to it for each level beyond the first, " +
                 "which needs something on the entity that reports a level - the progression " +
                 "module supplies one. With no level source the second field does nothing.")]
        private LevelValue _maxHealth = LevelValue.Flat(100f);

        [SerializeField]
        [Tooltip("Spawn at full health. Turn this off for something that should appear already " +
                 "wounded, and set its health from elsewhere.")]
        private bool _startFull = true;

        /// <summary>Maximum health before any bonuses.</summary>
        public LevelValue MaxHealth => _maxHealth;

        /// <inheritdoc />
        /// <remarks>
        /// Order matters here and is worth stating. Health is configured first so that a maximum
        /// exists, then the saved value is applied on top. Restoring before configuring would clamp
        /// a saved value against a maximum of zero and quietly kill everything that loads.
        /// </remarks>
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var host = _entity.gameObject;

            var health = host.GetComponent<Health>();
            if (health == null) health = host.AddComponent<Health>();

            health.Configure(_maxHealth, _startFull);

            var instance = _entity.Instance;
            if (instance == null)
            {
                // The core creates the instance before any module attaches, so this cannot happen
                // through the normal path. Reported rather than worked around, because health that
                // silently stops persisting is exactly the kind of loss nobody notices until a save
                // comes back wrong.
                Debug.LogError("[Vitals] '" + _entity.name + "' has no instance, so its health " +
                               "cannot be saved or restored.", _entity);
                return;
            }

            // Bound after configuring, so a saved value is clamped against the real maximum. The
            // state becomes health's storage from here on, which is what makes a load work without
            // anything being told a load happened.
            health.Bind(instance.GetOrAddState<HealthState>());
        }
    }
}
