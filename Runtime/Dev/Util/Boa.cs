using UnityEngine;

public static partial class Boa
{
    public static void Log(object message, Object context = default)
    {
#if HAS_SGUI
        SguiLoggerOverlay.Log(message, context);
#else
        Debug.Log(message, context);
#endif
    }
}