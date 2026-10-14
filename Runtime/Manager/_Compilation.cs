using _ARK_;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Assemblies;

namespace _BOA_
{
    partial class BoaManager
    {
        [AutoStaticsCleanup] internal static ScriptOptions assemblies = ScriptOptions.Default;
        [NoAutoStaticsCleanup] internal static Assembly scriptsAssembly;

        //----------------------------------------------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitAssemblies()
        {
            scriptsAssembly = null;
            AddAssemblies(
                typeof(Enumerable).Assembly,
                typeof(System.Dynamic.ExpandoObject).Assembly,
                Assembly.Load("Microsoft.CSharp"),
                typeof(UnityEngine.Object).Assembly
            );
        }


        //----------------------------------------------------------------------------------------------------------

        public static void AddAssemblies(params Assembly[] assemblies) => BoaManager.assemblies = BoaManager.assemblies.AddReferences(assemblies);

        public static bool CompileScripts()
        {
            try
            {
                var dscripts = Path.Combine(NUCLEOR.DFHome.FullName, "scripts").ForceDir();

                var comparer = Path.DirectorySeparatorChar == '\\' ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
                var files = new SortedSet<string>(comparer);

                foreach (var file in dscripts.EnumerateFiles("*.cs", SearchOption.AllDirectories))
                    files.Add(file.FullName);

                var compilation = CSharpCompilation.Create(
                    assemblyName: $"BoaScripts_{Guid.NewGuid():N}",
                    syntaxTrees: files.Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file)),
                    references: CSharpScript.Create<object>(code: string.Empty, options: assemblies).GetCompilation().References,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                );

                using var stream = new MemoryStream();
                var result = compilation.Emit(stream);

                for (int i = 0; i < result.Diagnostics.Length; i++)
                {
                    Diagnostic diagnostic = result.Diagnostics[i];
                    switch (diagnostic.Severity)
                    {
                        case DiagnosticSeverity.Info:
                            Debug.Log(diagnostic.ToString());
                            break;

                        case DiagnosticSeverity.Warning:
                            Debug.LogWarning(diagnostic.ToString());
                            break;

                        case DiagnosticSeverity.Error:
                            Debug.LogError(diagnostic.ToString());
                            break;

                        case DiagnosticSeverity.Hidden:
                        default:
                            Debug.Log(diagnostic.ToSubLog());
                            break;
                    }
                }

                if (!result.Success)
                    return false;

                scriptsAssembly = CurrentAssemblies.LoadFromBytes(rawAssembly: stream.ToArray());

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }
    }
}