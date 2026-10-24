using Unity.Scripting.LifecycleManagement;

namespace Boa
{
    public static partial class Debug
    {
        internal interface IDebug
        {
            void Log(object message);
        }

        [AutoStaticsCleanup] internal static IDebug console;
        public static void Log(object message)
        {
            console.Log(message);
        }
    }
}