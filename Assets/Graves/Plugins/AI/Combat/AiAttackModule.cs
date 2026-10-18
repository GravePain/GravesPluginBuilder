using Graves.Core;
using Graves.Plugins.Combat;
using UnityEngine;

namespace Graves.Plugins.AI.Combat
{
    /// <summary>
    /// Lets a brain-driven entity attack what it catches.
    /// </summary>
    /// <remarks>
    /// Separate from the brain so a thing can chase without fighting. A follower, a herded animal
    /// and an escort all want the chase and none of them wants the swing, and this is what keeps the
    /// combat plugin out of their build.
    /// </remarks>
    [CreateAssetMenu(fileName = "AiAttackModule", menuName = "Graves/Modules/AI Attack")]
    public class AiAttackModule : DefinitionModule
    {
        [SerializeField]
        [Tooltip("How close the target must be before swinging. Keep this at or just under the " +
                 "combat module's range, and above the brain's stopping distance.")]
        [Min(0f)] private float _reach = 2f;

        [SerializeField]
        [Tooltip("How far off dead-ahead the target may be before the swing is held back, in " +
                 "degrees. Small values make an attacker turn to face before striking.")]
        [Range(0f, 180f)] private float _facingTolerance = 45f;

        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var host = _entity.gameObject;

            var brain = host.GetComponent<ChaseBrain>();
            if (brain == null)
            {
                Debug.LogError("[AI] '" + _entity.name + "' is set to attack what it chases but has " +
                               "no brain, so it never chooses anything to chase. Add an AI brain " +
                               "module.", _entity);
                return;
            }

            var attacker = host.GetComponent<Attacker>();
            if (attacker == null)
            {
                Debug.LogError("[AI] '" + _entity.name + "' is set to attack but has no combat " +
                               "module, so there is nothing to swing.", _entity);
                return;
            }

            // Installed before the driver, so the first swing is already filtered. A single frame of
            // unfiltered swinging is enough for a pack that spawned shoulder to shoulder.
            if (host.GetComponent<HuntsTargetsOnly>() == null) host.AddComponent<HuntsTargetsOnly>();
            attacker.RefreshSources();

            var driver = host.GetComponent<AiAttackDriver>();
            if (driver == null) driver = host.AddComponent<AiAttackDriver>();

            driver.Initialise(brain, attacker, _reach, _facingTolerance);
        }
    }
}
