using UnityEngine;

namespace _BOA_.Dev
{
    static partial class Main
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitAssembly()
        {
            BoaManager.AddAssemblies(
                typeof(Main).Assembly
            );

#if HAS_SGUI
            BoaManager.AddAssemblies(
                typeof(SguiLoggerOverlay).Assembly
            );
#endif
        }
    }
}