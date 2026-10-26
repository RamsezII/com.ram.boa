using _ARK_;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace _BOA_
{
    public sealed partial class BoaManager : ArkComponent2
    {
        [AutoStaticsCleanup] public static BoaManager instance;

        //----------------------------------------------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void OnAfterSceneLoad()
        {
            Util.InstantiateOrCreate<BoaManager>();
        }

        //----------------------------------------------------------------------------------------------------------

        protected override void Awake()
        {
            instance = this;

            base.Awake();

            CompileScripts();

            NUCLEOR.delegates.OnApplicationFocus += CompileScripts;
        }

        //----------------------------------------------------------------------------------------------------------

        protected override void OnDestroy()
        {
            NUCLEOR.delegates.OnApplicationFocus -= CompileScripts;

            base.OnDestroy();
        }
    }
}
