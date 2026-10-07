#if HAS_TERMINAL
using UnityEngine;

namespace _BOA_
{
    partial class Shell
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CMD_Terminal()
        {
            _TERMINAL_.Shell.root_commands.AddCommand(new(
                owner: null,
                name: "Boa",
                onCmd_line: static async line =>
                {
                    bool cmd_b = line.TryRead(out string cmd);
                    if (line.IsExec)
                    {
                        using var shell = new Shell();
                        var result = shell.Execute(cmd);
                        Debug.Log(result);
                    }
                }
            ));
        }
    }
}
#endif