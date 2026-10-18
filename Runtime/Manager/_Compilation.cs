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
    public interface IBoaAssembly
    {
    }

    partial class BoaManager
    {
        [AutoStaticsCleanup] internal static ScriptOptions assemblies;
        [AutoStaticsCleanup] internal static Assembly scriptsAssembly;
        [AutoStaticsCleanup] public static Action beforeScriptsReload, afterScriptsReload;
        [AutoStaticsCleanup] static bool compiling;
        [NoAutoStaticsCleanup] static ScriptLoadContext scriptsLoadContext;

        //----------------------------------------------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitAssemblies()
        {
            UnloadScripts();
            assemblies = ScriptOptions.Default;

            AddAssemblies(Assembly.Load("netstandard"));
            AddAssemblies(Assembly.Load("UnityEngine.CoreModule"));
            AddAssemblies(typeof(Boa.Main).Assembly);

            foreach (var assembly in CurrentAssemblies.GetLoadedAssemblies())
                if (assembly.GetTypes().Any(type => typeof(IBoaAssembly).IsAssignableFrom(type)))
                    AddAssemblies(assembly);

            Boa.Debug.onLog += message =>
            {
#if HAS_SGUI
                SguiLoggerOverlay.Log(message);
#else
                Debug.Log(message);
#endif
            };
        }

        [OnCodeUnloading, OnExitingPlayMode]
        static void UnloadScripts()
        {
            if (scriptsLoadContext != null)
                Notify(beforeScriptsReload);

            var previous = scriptsLoadContext;
            scriptsLoadContext = null;
            scriptsAssembly = null;
            previous?.Unload();
        }

        //----------------------------------------------------------------------------------------------------------

        public static void AddAssemblies(params Assembly[] assemblies) => BoaManager.assemblies = BoaManager.assemblies.AddReferences(assemblies);

        public static IEnumerable<Type> GetUserTypes<T>() where T : class => scriptsAssembly?.GetTypes().Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters && typeof(T).IsAssignableFrom(type)) ?? Enumerable.Empty<Type>();

        public static Type GetUserType<T>(string name) where T : class
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var type = scriptsAssembly?.GetType(name);
            return type != null && type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters && typeof(T).IsAssignableFrom(type) ? type : null;
        }

        public static bool CompileScripts()
        {
            if (compiling)
                return false;

            compiling = true;
            Debug.Log($"Start compiling".ToSubLog());
            ScriptLoadContext nextContext = null;

            try
            {
                var dscripts = Path.Combine(NUCLEOR.DFHome.FullName, "scripts").ForceDir();

                var comparer = Path.DirectorySeparatorChar == '\\' ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
                var files = new SortedSet<string>(comparer);

                foreach (string file in EnumerateScriptFiles(dscripts.FullName))
                    files.Add(file);

                var compilation = CSharpCompilation.Create(
                    assemblyName: $"MACHINECODE_{Guid.NewGuid():N}",
                    syntaxTrees: files.Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file)),
                    references: CSharpScript.Create<object>(code: string.Empty, options: assemblies).GetCompilation().References,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                );

                try
                {
                    WriteScriptsProject(dscripts, compilation);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Could not generate the BOA scripts project: {exception.Message}");
                }

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

                nextContext = new ScriptLoadContext(compilation.AssemblyName);
                stream.Position = 0;
                var nextAssembly = nextContext.Load(stream);
                nextAssembly.GetTypes();

                Notify(beforeScriptsReload);

                var previous = scriptsLoadContext;
                scriptsLoadContext = nextContext;
                scriptsAssembly = nextAssembly;
                nextContext = null;

                try
                {
                    Notify(afterScriptsReload);
                }
                finally
                {
                    previous?.Unload();
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
            finally
            {
                nextContext?.Unload();
                compiling = false;
                Debug.Log($"Done compiling".ToSubLog());
            }
        }

        static void Notify(Action callbacks)
        {
            if (callbacks == null)
                return;

            foreach (Action callback in callbacks.GetInvocationList().Cast<Action>())
                try
                {
                    callback();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
        }
    }
}
