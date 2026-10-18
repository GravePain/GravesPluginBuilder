using Framework.Combat;
using Framework.Core;
using Graves.Core;
using Graves.Plugins.Vitals;
using UnityEngine;

namespace Graves.Plugins.CombatBridge
{
    /// <summary>
    /// Carries resolved hits from an external combat framework into this entity's health.
    /// </summary>
    /// <remarks>
    /// <b>This is the whole of the adoption.</b> The combat framework does not know this framework
    /// exists and never will; nothing in it was edited, and nothing here reaches into it past a
    /// public member. Everything that joins the two lives in this assembly, so when one side is
    /// replaced only this dies.
    ///
    /// <b>Who owns the number.</b> Both sides keep health, and that is a fact of adopting something
    /// that already worked rather than a design anyone chose. The rule here is: the combat
    /// framework's HealthSystem is the truth during a fight, and this entity's Health is the record
    /// that survives one. They stay in step because both are driven from the same resolved hit,
    /// never from each other - a mirror that copies a mirror drifts, and the drift shows up as a
    /// health bar that disagrees with the damage numbers.
    ///
    /// <b>What this deliberately does not do.</b> It does not push a loaded value back into
    /// HealthSystem. There is no public way to set that field, and the only door standing open -
    /// applying the missing damage - would fire the framework's own damage and death events on load,
    /// so a character restored at low health would play a hit reaction, and one restored at zero
    /// would die again on the loading screen. Picking a lock is how the first plugin here got past a
    /// private field, and it was wrong then too. <b>The gap is real and it is reported once, loudly,
    /// rather than papered over</b>: until the combat framework offers a way to set current health,
    /// a saved fight resumes with the framework's side at full.
    /// </remarks>
    [DisallowMultipleComponent]
    public class CombatBridge : MonoBehaviour
    {
        private Health _health;
        private CharacterBrain _brain;

        /// <summary>
        /// The part of a hit smaller than one point, kept rather than dropped.
        /// </summary>
        /// <remarks>
        /// The combat framework deals fractional damage; health here is whole numbers, because a save
        /// has to round-trip exactly. Rounding each hit on its own loses up to half a point every
        /// time, and over a long fight a character survives blows that killed them - the numbers on
        /// screen add up to more than the health bar spent. Carrying the remainder into the next hit
        /// makes the total exact and costs one float.
        /// </remarks>
        private float _remainder;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _brain = GetComponent<CharacterBrain>();

            if (_health == null)
            {
                Debug.LogError("[CombatBridge] '" + name + "' has no Health, so resolved hits have " +
                               "nowhere to land. Add the Vitals module to its definition.", this);
                enabled = false;
                return;
            }

            if (_brain == null)
            {
                Debug.LogError("[CombatBridge] '" + name + "' has no CharacterBrain, so the combat " +
                               "framework can never name it as a target and this bridge would sit " +
                               "silent forever.", this);
                enabled = false;
                return;
            }

            if (_health.Current < _health.Max)
            {
                Debug.LogWarning("[CombatBridge] '" + name + "' was restored at " + _health.Current +
                                 " of " + _health.Max + ", but the combat framework's HealthSystem " +
                                 "starts every character at full and offers no way to set it. Its " +
                                 "side of this fight begins fresh. See CombatBridge's remarks.", this);
            }
        }

        // Subscribed here rather than in Awake so an entity that is pooled or disabled between
        // fights is not still receiving hits while switched off.
        private void OnEnable() => DamageResolver.OnHitResolved += OnHitResolved;

        private void OnDisable() => DamageResolver.OnHitResolved -= OnHitResolved;

        /// <summary>
        /// Translates one resolved hit into a whole number of points, for this entity only.
        /// </summary>
        /// <remarks>
        /// The event is static and global, so every bridge in the scene hears every hit and the
        /// first thing this does is establish that the hit was aimed here. That check is not
        /// defensive: without it, one landed blow damages every living thing in the level, and the
        /// symptom - everything dying at once - looks nothing like its cause.
        ///
        /// <b>Poise is ignored on purpose.</b> The combat framework already spent it, staggered the
        /// character and moved its state machine. Mirroring it into a second system would give the
        /// entity two opinions about whether it is staggered, and no way to reconcile them.
        /// </remarks>
        private void OnHitResolved(HitEvent _hit, float _damage, float _poiseDamage)
        {
            if (_hit.Target != _brain) return;
            if (_damage <= 0f) return;

            float total = _damage + _remainder;
            int points = Mathf.FloorToInt(total);
            _remainder = total - points;

            if (points <= 0) return;

            var dealer = _hit.Attacker != null ? _hit.Attacker.gameObject : null;
            _health.TakeDamage(points, dealer);
        }
    }
}
