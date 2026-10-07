using _ARK_;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace _BOA_
{
    partial class Shell
    {
        [NoAutoStaticsCleanup]
        static readonly CodeInterpreter static_interpreter = new(name: "BOA", extension: ".boa.txt")
        {
            linter = (in string text, in int index, in LintTheme theme, out string lint, out string error) =>
            {
                lint = text;
                error = null;
            },
        };

        //----------------------------------------------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InitInterpreter()
        {
            CodeInterpreter.instances.Add(static_interpreter);
        }
    }
}