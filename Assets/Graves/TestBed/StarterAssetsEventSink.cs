using UnityEngine;

namespace Graves.TestBed
{
    /// <summary>
    /// Absorbs the animation events baked into Unity's Starter Assets locomotion clips.
    /// </summary>
    /// <remarks>
    /// Those clips carry <c>OnFootstep</c> and <c>OnLand</c> events expecting the
    /// <c>ThirdPersonController</c> script, which this project deliberately does not import - it would
    /// drag in Cinemachine and their whole input rig for a test that needs neither. With no receiver,
    /// Unity logs an error per event, which is roughly two per second of walking and drowns out
    /// anything worth reading.
    ///
    /// This is test scaffolding for imported content, not part of the framework or of any plugin. It
    /// belongs to the clips, and it leaves with them.
    /// </remarks>
    public class StarterAssetsEventSink : MonoBehaviour
    {
        /// <summary>Called by the walk and run clips. Deliberately does nothing.</summary>
        public void OnFootstep(AnimationEvent _event) { }

        /// <summary>Called by the landing clips. Deliberately does nothing.</summary>
        public void OnLand(AnimationEvent _event) { }
    }
}
