using Graves.Core;
using Graves.Plugins.Combat;
using UnityEngine;

namespace Graves.Plugins.AI.Combat
{
    /// <summary>
    /// Swings whenever what it is chasing has come within reach.
    /// </summary>
    /// <remarks>
    /// <b>The machine counterpart of a person clicking the mouse</b>, and just as thin. It decides
    /// only when to ask; whether the swing is allowed, what it reaches and what it hurts all stay
    /// with the attacker, so a monster and a player are held to identical rules by identical code.
    /// </remarks>
    [DisallowMultipleComponent]
    public class AiAttackDriver : MonoBehaviour
    {
        private ChaseBrain _brain;
        private Attacker _attacker;
        private float _reach = 2f;
        private float _facingTolerance = 45f;

        /// <summary>Wires the driver to the brain that picks targets and the arm that swings.</summary>
        public void Initialise(ChaseBrain _source, Attacker _weapon, float _attackReach, float _tolerance)
        {
            _brain = _source;
            _attacker = _weapon;
            _reach = Mathf.Max(0f, _attackReach);
            _facingTolerance = Mathf.Clamp(_tolerance, 0f, 180f);
        }

        private void Update()
        {
            if (_brain == null || _attacker == null) return;
            if (!_attacker.IsReady) return;

            var quarry = _brain.Quarry;
            if (quarry == null) return;

            var offset = quarry.transform.position - transform.position;
            offset.y = 0f;

            if (offset.sqrMagnitude > _reach * _reach) return;

            // Checked separately from the attacker's own arc, because this is about whether the
            // chaser has finished turning. Without it a monster arriving from behind swings at the
            // moment it gets close and the blow lands nowhere, which reads as an enemy that misses
            // for no reason.
            if (offset.sqrMagnitude > 0.000001f &&
                Vector3.Angle(transform.forward, offset.normalized) > _facingTolerance)
            {
                return;
            }

            _attacker.TryAttack();
        }
    }
}
