using UnityEngine;

namespace _BOA_.DevApi
{
    public partial class Debug : Boa.Debug.IDebug
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Boa.Debug.console = new Debug();
        }

        //----------------------------------------------------------------------------------------------------------

        void Boa.Debug.IDebug.Log(object message)
        {
#if HAS_SGUI
            SguiLoggerOverlay.Log(message);
#else
            Debug.Log(message);
#endif
        }
    }
}