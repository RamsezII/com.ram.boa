using UnityEngine;

namespace Boa
{
    public static partial class Debug
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
}