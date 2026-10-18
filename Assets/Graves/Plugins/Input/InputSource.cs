using System;
using UnityEngine;

namespace Graves.Plugins.Input
{
    /// <summary>
    /// What a person is asking for this frame, in device-neutral terms.
    /// </summary>
    /// <remarks>
    /// <b>Every value here is raw and unjudged.</b> <see cref="Move"/> is a stick position, not a
    /// world direction - turning it into one needs a camera, and a camera is a decision that belongs
    /// to whatever is being driven rather than to the thing reading the hardware.
    ///
    /// <b>The actions are typed fields rather than names looked up in a table.</b> A name can be
    /// misspelled and fails at runtime with an empty result; a field cannot, and it appears in a
    /// compiler error and an inspector. The cost is that adding an action means editing this struct,
    /// which is a genuinely worse story for extension - taken deliberately. See
    /// <c>Agent Memory/decisions/D-002-input-scope.md</c>.
    /// </remarks>
    [Serializable]
    public struct InputState
    {
        /// <summary>
        /// Direction on a stick or the movement keys. Y is forward, X is right, each in the range
        /// minus one to one. Magnitude carries throttle.
        /// </summary>
        public Vector2 Move;

        /// <summary>Camera or aim movement this frame, in device units rather than degrees.</summary>
        public Vector2 Look;

        /// <summary>True on the frame jump was pressed. Not held.</summary>
        public bool Jump;

        /// <summary>True on the frame attack was pressed. Not held.</summary>
        public bool Attack;

        /// <summary>
        /// True on the frame the skill key was pressed. Not held.
        /// </summary>
        /// <remarks>
        /// One skill, not a hotbar. A hotbar is a row of slots, each naming a skill, and it needs a
        /// user interface to be worth anything - which is stage three work. This is the smallest
        /// thing that lets a person actually press a skill and watch it happen, and adding a field
        /// here is the honest way to extend this struct rather than the alternative of a second
        /// input source competing to produce the same intent.
        /// </remarks>
        public bool Skill;

        /// <summary>
        /// True on the frame the use key was pressed. Not held.
        /// </summary>
        /// <remarks>
        /// <b>Added by a game, which is what D-002 said would happen.</b> That decision recorded the
        /// cost of a fixed input struct in advance: "a plugin that wants an action nobody anticipated
        /// must add a field here." Gathering wanted one. The field was added, one line was added to
        /// the keyboard source, and nothing else in the framework noticed - which is the cheap
        /// outcome the decision predicted, now observed rather than assumed.
        ///
        /// The alternative was a second input source reading the keyboard alongside this one, which
        /// is the two-producers problem F-015 had just finished removing.
        /// </remarks>
        public bool Use;

        /// <summary>Nobody is asking for anything.</summary>
        public static InputState None => default;
    }

    /// <summary>
    /// Where input comes from. Anything that wants to know what the person is doing asks one of
    /// these, and never touches a device.
    /// </summary>
    /// <remarks>
    /// <b>The whole reason this type exists is the rule that nothing above it reads hardware.</b>
    /// Honour that and input can be replaced wholesale - a different device, a recording played back
    /// for a test, an AI pretending to be a player - without anything downstream noticing.
    ///
    /// It is an abstract component rather than an interface for the same reason a motor is: whatever
    /// needs it finds it with <c>GetComponent</c> when it attaches, and gets whichever implementation
    /// the entity was given. Nothing holds an authored reference to one.
    /// </remarks>
    [DisallowMultipleComponent]
    public abstract class InputSource : MonoBehaviour
    {
        /// <summary>
        /// The current state. Safe to call more than once in a frame and expected to return the same
        /// answer each time within that frame.
        /// </summary>
        /// <remarks>
        /// That guarantee matters for the one-frame flags. If two readers ask whether jump was
        /// pressed and the second gets false because the first consumed it, the resulting bug is
        /// intermittent and depends on component order, which is close to the worst kind to find.
        /// </remarks>
        public abstract InputState Read();
    }
}
