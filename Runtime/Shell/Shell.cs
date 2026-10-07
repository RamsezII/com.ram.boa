using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace _BOA_
{
    public sealed partial class Shell
    {

        //----------------------------------------------------------------------------------------------------------

        public object Execute(string text)
        {
            return CSharpScript.EvaluateAsync<object>(text);
        }
    }
}