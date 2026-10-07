#if HAS_TERMINAL
using _TERM_;
using UnityEngine;

namespace _BOA_
{
    partial class Shell
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CMD_Term()
        {
            TermServer.root_namespace.AddCommand(
                name: "Boa",
                execution: static context =>
                {
                    bool cmd_b = context.Reader.TryRead(out string cmd);
                    return CmdExecution.Function(context =>
                    {
                        using var shell = new Shell();
                        var result = shell.Execute(cmd);
                        return result.ToString();
                    });
                }
            );
        }
    }
}
#endif