using UnityEngine;

partial class Boa
{
    public static void Log_console(object message, Object context = default) => Debug.Log(message, context);
#if HAS_SGUI
    public static void Log_overlay(object message, Object context = default) => SguiLoggerOverlay.Log(message, context);
#endif
}