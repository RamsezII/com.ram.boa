#if ENABLE_INPUT_SYSTEM
using Boa;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace _BOA_.DevApi
{
    public static partial class Util_boa_dev
    {
        public static ButtonState ToButtonState(this InputAction action) => new(action.WasPerformedThisFrame(), action.IsPressed(), action.WasReleasedThisFrame());
        public static ButtonState ToButtonState(this ButtonControl control) => new(control.wasPressedThisFrame, control.isPressed, control.wasReleasedThisFrame);

        public static ButtonState GetButton_Gamepad(GamepadButton code) => Gamepad.current?[code].ToButtonState() ?? default;
        public static ButtonState GetButton_Keyboard(Key code) => Keyboard.current?[code].ToButtonState() ?? default;
        public static ButtonState GetButton_Mouse(MouseButton code) => Mouse.current?.GetButton(code).ToButtonState() ?? default;
    }
}
#endif
