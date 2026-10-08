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
        [AutoStaticsCleanup] static ScriptOptions assemblies = ScriptOptions.Default;
        public static void AddAssemblies(params Assembly[] assemblies) => Shell.assemblies = Shell.assemblies.AddReferences(assemblies);

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

        public object Execute(string text) => CSharpScript.EvaluateAsync<object>(text, options: assemblies).GetAwaiter().GetResult();

        //----------------------------------------------------------------------------------------------------------

        public void Dispose()
        {
        }
    }
}