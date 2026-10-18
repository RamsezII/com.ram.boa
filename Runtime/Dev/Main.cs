using System;
using Unity.Scripting.LifecycleManagement;

namespace Boa
{
    public static partial class Debug
    {
        [AutoStaticsCleanup] internal static Action<object> onLog;
        public static void Log(object message)
        {
            onLog?.Invoke(message);
        }
    }
}