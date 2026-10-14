using _ARK_;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace _BOA_
{
    public sealed partial class Shell : ArkShell
    {
        ScriptState<object> state;

        //----------------------------------------------------------------------------------------------------------

        public object Execute(string text)
        {
            if (state is null)
                state = CSharpScript.RunAsync<object>(text, options: BoaManager.assemblies).GetAwaiter().GetResult();
            else
                state = state.ContinueWithAsync<object>(text, options: BoaManager.assemblies).GetAwaiter().GetResult();
            return state.ReturnValue;
        }

        //----------------------------------------------------------------------------------------------------------

        public override void Clear()
        {
            base.Clear();
            state = null;
        }
    }
}