using UnityEngine;
using UnityEngine.InputSystem;

namespace Graves.Plugins.Input
{
    /// <summary>
    /// Reads a keyboard, mouse and gamepad, and reports them as device-neutral actions.
    /// </summary>
    /// <remarks>
    /// <b>Bindings are fixed here and that is a deliberate limit.</b> Rebinding belongs to a settings
    /// screen that does not exist, and building it now would fix the shape of how actions are
    /// declared before anything real has read one. See
    /// <c>Agent Memory/decisions/D-002-input-scope.md</c>.
    ///
    /// <b>Every device is optional.</b> A machine with no gamepad is normal, and a machine with no
    /// keyboard happens on a handheld or when the editor loses focus. Each is checked for null
    /// rather than assumed, because the alternative is an exception every frame in a build nobody
    /// can attach a debugger to.
    /// </remarks>
    [DisallowMultipleComponent]
    public class KeyboardMouseInput : InputSource
    {
        [SerializeField]
        [Tooltip("Scales look movement before it is reported. Tuning for feel belongs to whatever " +
                 "consumes Look, but a sane default keeps it usable.")]
        [Min(0f)] private float _lookSensitivity = 1f;

        [SerializeField]
        [Tooltip("How far a fully deflected stick turns the view in one second, expressed in the " +
                 "same units a mouse reports. Only affects gamepads.")]
        [Min(0f)] private float _stickLookRate = 600f;

        private InputState _state;
        private int _readOnFrame = -1;

        /// <inheritdoc />
        /// <remarks>
        /// Sampled once per frame and cached. The one-frame flags are the reason: two readers asking
        /// whether jump was pressed must get the same answer, and a bug where the second reader
        /// loses the press depends on component order and appears only sometimes.
        /// </remarks>
        public override InputState Read()
        {
            if (_readOnFrame == Time.frameCount) return _state;

            _readOnFrame = Time.frameCount;
            _state = Sample();
            return _state;
        }

        private InputState Sample()
        {
            var state = InputState.None;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) state.Move.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) state.Move.y -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) state.Move.x += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) state.Move.x -= 1f;

                if (keyboard.spaceKey.wasPressedThisFrame) state.Jump = true;
                if (keyboard.qKey.wasPressedThisFrame) state.Skill = true;
                if (keyboard.eKey.wasPressedThisFrame) state.Use = true;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                // Added rather than replaced, so a stick and the keys can both be live without one
                // silently winning. A stick at rest contributes nothing.
                state.Move += gamepad.leftStick.ReadValue();

                // Scaled by frame time, and a mouse is not. This is the one place the two devices
                // genuinely differ: a mouse reports how far it MOVED since the last frame, which is
                // already a per-frame amount, while a stick reports how far it is HELD, which is a
                // rate and means nothing until multiplied by elapsed time. Treating them alike makes
                // stick look speed depend on frame rate while the mouse stays correct, and the
                // resulting bug reads as "the gamepad feels wrong on my other machine".
                state.Look += gamepad.rightStick.ReadValue() * (_stickLookRate * Time.deltaTime);

                if (gamepad.buttonSouth.wasPressedThisFrame) state.Jump = true;
                if (gamepad.buttonWest.wasPressedThisFrame) state.Attack = true;
                if (gamepad.buttonNorth.wasPressedThisFrame) state.Skill = true;
                if (gamepad.buttonWest.wasPressedThisFrame) state.Use = true;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                state.Look += mouse.delta.ReadValue();
                if (mouse.leftButton.wasPressedThisFrame) state.Attack = true;
            }

            // Clamped rather than normalised. Normalising would turn a half-pushed stick into a
            // full-speed run, and the magnitude is what carries throttle downstream.
            state.Move = Vector2.ClampMagnitude(state.Move, 1f);
            state.Look *= _lookSensitivity;

            return state;
        }
    }
}
