using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace _BOA_
{
    public sealed partial class Shell : IDisposable
    {
        public static void AddAssemblies(params Assembly[] assemblies) => Shell.assemblies = Shell.assemblies.AddReferences(assemblies);
        [AutoStaticsCleanup] static ScriptOptions assemblies = ScriptOptions.Default;

        ScriptState<object> state;

        //----------------------------------------------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitAssemblies()
        {
            AddAssemblies(
                typeof(System.Linq.Enumerable).Assembly,
                typeof(System.Dynamic.ExpandoObject).Assembly,
                Assembly.Load("Microsoft.CSharp"),
                typeof(UnityEngine.Object).Assembly
            );
        }

        //----------------------------------------------------------------------------------------------------------

        public object Execute(string text)
        {
            if (state is null)
                state = CSharpScript.RunAsync<object>(text, options: assemblies).GetAwaiter().GetResult();
            else
                state = state.ContinueWithAsync<object>(text, options: assemblies).GetAwaiter().GetResult();
            return state.ReturnValue;
        }

        public void Reset() => state = null;

        //----------------------------------------------------------------------------------------------------------

        public void Dispose()
        {
            Reset();
        }
    }
}