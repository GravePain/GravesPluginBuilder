using Graves.Core;
using Graves.Plugins.Vitals;
using UnityEngine;
using Graves.Core;

namespace Graves.Plugins.Combat
{
    /// <summary>
    /// Gives an entity the ability to hurt things: how hard, how far, how wide, and how often.
    /// </summary>
    /// <remarks>
    /// Installs the swing and nothing that triggers it. What decides to attack - a person pressing a
    /// button, or a brain deciding the player is close enough - lives elsewhere, so the same combat
    /// values serve a player and a monster without either knowing about the other.
    /// </remarks>
    [CreateAssetMenu(fileName = "CombatModule", menuName = "Graves/Modules/Combat")]
    public class CombatModule : DefinitionModule
    {
        [Header("Damage")]
        [SerializeField]
        [Tooltip("Damage per hit. The second field adds to it for each level beyond the first, " +
                 "which needs something on the entity that reports a level - the progression " +
                 "module supplies one. With no level source the second field does nothing.")]
        private LevelValue _damage = LevelValue.Flat(10f);

        [Header("Reach")]
        [SerializeField]
        [Tooltip("How far the swing reaches, in metres.")]
        [Min(0f)] private float _range = 2f;

        [SerializeField]
        [Tooltip("Total width of the swing in degrees, centred on facing. Ninety is a generous " +
                 "sweep; twenty is a thrust that demands aim.")]
        [Range(0f, 360f)] private float _arc = 120f;

        [SerializeField]
        [Tooltip("Hit everything inside the arc rather than only the first thing found. A cleave " +
                 "rather than a stab.")]
        private bool _hitEverythingInArc;

        [Header("Reach")]
        [SerializeField]
        [Tooltip("Which physics layers a swing may hit. Everything by default, because this " +
                 "framework ships no layers of its own. Narrow it the moment the game has something " +
                 "a sword should not connect with - a projectile, a ragdoll bone, a trigger volume.")]
        private LayerMask _hits = ~0;

        [Header("Timing")]
        [SerializeField]
        [Tooltip("Seconds between swings. The cooldown starts when the swing does, so missing " +
                 "still costs time.")]
        [Min(0f)] private float _cooldown = 0.6f;

        /// <summary>Damage per hit before any bonuses.</summary>
        public LevelValue Damage => _damage;

        /// <inheritdoc />
        public override void AttachTo(GameEntity _entity)
        {
            if (_entity == null) return;

            var host = _entity.gameObject;

            var attacker = host.GetComponent<Attacker>();
            if (attacker == null) attacker = host.AddComponent<Attacker>();

            attacker.Configure(_damage, _range, _arc, _cooldown, _hitEverythingInArc, _hits);

            // Not an error. A thing that can hit but cannot be hit is a legitimate design - a trap,
            // a turret, something invulnerable - and refusing to attach would be the framework
            // making a game's decision. Said once so it is never a surprise.
            if (host.GetComponent<Health>() == null)
            {
                Debug.Log("[Combat] '" + _entity.name + "' can attack but has no vitals, so nothing " +
                          "can hurt it back.", _entity);
            }
        }
    }
}
