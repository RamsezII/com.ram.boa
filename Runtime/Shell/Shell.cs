using Microsoft.CodeAnalysis.CSharp.Scripting;
using System;

namespace _BOA_
{
    public sealed partial class Shell : IDisposable
    {

        //----------------------------------------------------------------------------------------------------------

        public object Execute(string text) => CSharpScript.EvaluateAsync<object>(text).GetAwaiter().GetResult();

        //----------------------------------------------------------------------------------------------------------

        public void Dispose()
        {

        }
    }
}