#if ENABLE_INPUT_SYSTEM
using Boa;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace _BOA_.DevApi
{
    public static partial class Main
    {
        public static ButtonState ToButtonState(this InputAction action) => new(action.WasPerformedThisFrame(), action.IsPressed(), action.WasReleasedThisFrame());
        public static ButtonState ToButtonState(this ButtonControl control) => new(control.wasPressedThisFrame, control.isPressed, control.wasReleasedThisFrame);
    }
}
#endif