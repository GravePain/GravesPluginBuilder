using UnityEngine;

namespace Graves.Plugins.Locomotion
{
    /// <summary>
    /// Turns a <see cref="MoveIntent"/> into actual movement. One entity has exactly one, and what
    /// kind it is decides how the character physically behaves.
    /// </summary>
    /// <remarks>
    /// <b>Why this is an abstract component rather than an interface.</b> Anything that wants the
    /// motor finds it with <c>GetComponent&lt;Motor&gt;()</c> at the moment it attaches, and gets
    /// whichever implementation the definition installed. That lookup is the whole swap mechanism.
    ///
    /// <b>Nothing may hold an authored reference to a motor.</b> This is the load-bearing rule and
    /// it comes from the reference material rather than from taste. The reference framework abstracts movement
    /// properly - fourteen abstract members, consumers holding the abstract type - and swapping it
    /// is still a manual procedure, because every consumer holds a serialised reference to the
    /// component instance. Replace the component and each of those references is left pointing at a
    /// destroyed object, so the documented swap is a list of drag operations. The abstraction was
    /// never the problem; the wiring was. See the second agent's extraction note.
    ///
    /// So: resolve at attach time, never serialise. A consumer that stores a motor in an authored
    /// field has reintroduced the defect this class exists to avoid.
    ///
    /// <b>The motor owns no stats.</b> Speed arrives through <see cref="SetSpeed"/> because it
    /// belongs to the entity and will one day depend on level, equipment and status effects. A motor
    /// that reads a speed stat is a motor that has to know what a stat is.
    /// </remarks>
    [DisallowMultipleComponent]
    public abstract class Motor : MonoBehaviour
    {
        /// <summary>Current world-space velocity. Useful to animation and to anything judging speed.</summary>
        public abstract Vector3 Velocity { get; }

        /// <summary>True when the character is supported by something.</summary>
        /// <remarks>
        /// A motor with no concept of falling should return true rather than inventing an answer -
        /// "always supported" is honest for click-to-move on a navigation mesh.
        /// </remarks>
        public abstract bool IsGrounded { get; }

        /// <summary>
        /// Accepts what is wanted this frame. Called by whatever is driving; the motor acts on it in
        /// its own loop.
        /// </summary>
        /// <remarks>
        /// Storing the intent rather than acting immediately keeps the physics in one place and at
        /// one rate, and means a brain may set intent as often or as rarely as suits it without
        /// changing how the character moves.
        /// </remarks>
        public abstract void SetIntent(MoveIntent _intent);

        /// <summary>Sets the speed a full-magnitude intent should produce, in metres per second.</summary>
        public abstract void SetSpeed(float _metresPerSecond);

        /// <summary>
        /// Places the character somewhere immediately, without travelling through the space between.
        /// </summary>
        /// <remarks>
        /// Exists as a method rather than leaving callers to set the transform, because several
        /// movement implementations cannot be moved that way - a character controller must be
        /// disabled across the move, and an agent on a navigation mesh needs its own warp call or it
        /// snaps back. A save restoring a position is the first caller, and it must not need to know
        /// which kind of motor it is talking to.
        /// </remarks>
        public abstract void Warp(Vector3 _position);

        /// <summary>Clears all motion and any stored intent. The character stands still.</summary>
        public abstract void Stop();
    }
}
