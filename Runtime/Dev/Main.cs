using _BOA_;
using UnityEngine;

public static partial class Boa
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        BoaManager.AddAssemblies(typeof(Boa).Assembly);
    }

    //--------------------------------------------------------------------------------------------------------------

    public static void Log(object message, Object context = default)
    {
#if HAS_SGUI
        SguiLoggerOverlay.Log(message, context);
#else
        Debug.Log(message, context);
#endif
    }
}