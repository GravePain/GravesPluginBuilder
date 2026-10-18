using System;
using UnityEngine;

namespace Graves.Plugins.Locomotion
{
    /// <summary>
    /// What something wants to do this frame, expressed without any knowledge of how it will be
    /// carried out. A brain produces one of these; a <see cref="Motor"/> consumes it.
    /// </summary>
    /// <remarks>
    /// <b>This struct is the entire contract between a brain and a motor, and that is the point.</b>
    /// A player brain reading a keyboard and an AI brain following a path produce the same thing, so
    /// they drive the same legs and neither one knows what the other is. Swapping how a character is
    /// moved does not touch anything that decides where it should go.
    ///
    /// <b>Intent is desire, not instruction.</b> A motor is free to refuse: to clamp the move to a
    /// walkable surface, to ignore a jump while airborne, to slide along a wall. Nothing here is a
    /// command, which is what lets a navmesh motor and a physics motor honestly accept the same
    /// input despite obeying completely different rules.
    ///
    /// <b>Everything is world space.</b> A brain that thinks in camera space converts before it gets
    /// here, because the motor has no camera and should never acquire one.
    /// </remarks>
    [Serializable]
    public struct MoveIntent
    {
        /// <summary>
        /// Desired direction of travel in world space, on the horizontal plane. Magnitude carries
        /// throttle: zero is standing still, one is full speed, and anything between is a walk.
        /// </summary>
        /// <remarks>
        /// Magnitude rather than a separate speed value, so an analogue stick pushed halfway reads
        /// as a half-speed walk with no extra field and no interpretation by the motor.
        /// </remarks>
        public Vector3 Move;

        /// <summary>
        /// Desired facing in world space. Zero means "no opinion", and a motor holds the current
        /// facing rather than snapping anywhere.
        /// </summary>
        /// <remarks>
        /// Separate from <see cref="Move"/> because the two genuinely differ: strafing around a
        /// target moves sideways while facing it, and a click-to-move character faces its path.
        /// Collapsing them into one vector makes strafing impossible to express.
        /// </remarks>
        public Vector3 Look;

        /// <summary>
        /// True on the frame a jump is wanted. A motor decides whether it is allowed.
        /// </summary>
        /// <remarks>
        /// Deliberately a request rather than a held state. A brain should not be asking whether the
        /// character is grounded before it may want to jump - that is the motor's business, and a
        /// brain that has to know is a brain coupled to a motor.
        /// </remarks>
        public bool Jump;

        /// <summary>Wanting nothing: no movement, no facing change, no jump.</summary>
        /// <remarks>
        /// A brain that has stopped should send this rather than stop sending, so the difference
        /// between "stand still" and "nobody is driving" stays visible to the motor.
        /// </remarks>
        public static MoveIntent None => default;

        /// <summary>True when this intent asks for horizontal movement.</summary>
        public bool WantsToMove => Move.sqrMagnitude > 0.000001f;

        /// <summary>True when this intent expresses an opinion about facing.</summary>
        public bool WantsToTurn => Look.sqrMagnitude > 0.000001f;
    }
}
