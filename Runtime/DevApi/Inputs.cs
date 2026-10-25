#if ENABLE_INPUT_SYSTEM
using Boa;
using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace _BOA_.DevApi
{
    public static partial class Util_boa_dev
    {
        public static ButtonState ToButtonState(this InputAction action) => new(action.WasPerformedThisFrame(), action.IsPressed(), action.WasReleasedThisFrame());
        public static ButtonState ToButtonState(this ButtonControl control) => new(control.wasPressedThisFrame, control.isPressed, control.wasReleasedThisFrame);

        public static ButtonState GetButton_Gamepad<T>(T code) where T : Enum => Gamepad.current?[(GamepadButton)(int)(object)code].ToButtonState() ?? default;
        public static ButtonState GetButton_Keyboard<T>(T code) where T : Enum => Keyboard.current?[(Key)(int)(object)code].ToButtonState() ?? default;
        public static ButtonState GetButton_Mouse<T>(T code) where T : Enum => Mouse.current?.GetButton((MouseButton)(int)(object)code).ToButtonState() ?? default;
    }
}
#endif