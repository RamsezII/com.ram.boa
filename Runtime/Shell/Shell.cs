using Microsoft.CodeAnalysis.CSharp.Scripting;
using System;
using System.Threading.Tasks;

namespace _BOA_
{
    public sealed partial class Shell : IDisposable
    {

        //----------------------------------------------------------------------------------------------------------

        public Task<object> AExecute(string text) => CSharpScript.EvaluateAsync<object>(text);

        public object Execute(string text) => AExecute(text).GetAwaiter().GetResult();

        //----------------------------------------------------------------------------------------------------------

        public void Dispose()
        {

        }
    }
}