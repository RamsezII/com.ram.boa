using UnityEngine;

namespace _BOA_.Dev
{
    static partial class Main
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitAssembly()
        {
            Shell.AddAssemblies(
                typeof(Main).Assembly
            );

#if HAS_SGUI
            Shell.AddAssemblies(
                typeof(SguiLoggerOverlay).Assembly
            );
#endif
        }
    }
}