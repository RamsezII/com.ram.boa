using _ARK_;
using _UTIL_;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Assemblies;

namespace _BOA_
{
    partial class BoaManager
    {
        [AutoStaticsCleanup] internal static ScriptOptions assemblies;
        [AutoStaticsCleanup] internal static Assembly scriptsAssembly;
        [AutoStaticsCleanup] public static Action beforeScriptsReload, afterScriptsReload;
        [AutoStaticsCleanup] static int mainThreadId;
        [NoAutoStaticsCleanup] static CompilationRequest activeCompilation;
        [NoAutoStaticsCleanup] static ScriptLoadContext scriptsLoadContext;

        /// <summary>Observe on Unity's main thread; only BoaManager should modify this value. Cleared on shutdown.</summary>
        [NoAutoStaticsCleanup] public static readonly ValueNotifier<bool> isCompiling = new();

        /// <summary>True until compilation and the main-thread reload have finished, including any queued recompile.</summary>
        public static bool IsCompiling => activeCompilation != null;

        sealed class CompilationRequest
        {
            public readonly CancellationTokenSource Cancellation = new();
            public readonly TaskCompletionSource<bool> Completion;
            public Task<CompilationOutput> Worker;
            public bool RecompileRequested;

            public CompilationRequest(TaskCompletionSource<bool> completion) => Completion = completion;
        }

        sealed class CompilationOutput
        {
            public string AssemblyName;
            public byte[] AssemblyBytes;
            public Diagnostic[] Diagnostics;
            public string ProjectWarning;
        }

        [AutoStaticsCleanup, HField]
        public static bool
            log_compilationWarnings = true,
            log_compilationDiagnostics_warnings = true;

        //----------------------------------------------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitAssemblies()
        {
            UnloadScripts();
            var playerApiAssemblies = CurrentAssemblies.GetLoadedAssemblies().Where(assembly => assembly.IsDefined(typeof(Boa.PlayerApiAssemblyAttribute), false));
            var frameworkAssemblies = new[] { Assembly.Load("netstandard"), typeof(Vector3).Assembly };
            var references = frameworkAssemblies.Concat(playerApiAssemblies).Distinct().Select(assembly => MetadataReference.CreateFromFile(assembly.GetLoadedAssemblyPath()));
            assemblies = ScriptOptions.Default.AddReferences(references);

            mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        [OnCodeUnloading, OnExitingPlayMode]
        static void UnloadScripts()
        {
            mainThreadId = 0;
            CancelCompilation();
            isCompiling.Clear();

            if (scriptsLoadContext != null)
                Notify(beforeScriptsReload);

            var previous = scriptsLoadContext;
            scriptsLoadContext = null;
            scriptsAssembly = null;
            previous?.Unload();
        }

        //----------------------------------------------------------------------------------------------------------

        public static IEnumerable<Type> GetUserTypes<T>() where T : class => scriptsAssembly?.GetTypes().Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters && typeof(T).IsAssignableFrom(type)) ?? Enumerable.Empty<Type>();

        public static Type GetUserType<T>(string name) where T : class
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var type = scriptsAssembly?.GetType(name);
            return type != null && type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters && typeof(T).IsAssignableFrom(type) ? type : null;
        }

        public static void CompileScripts() => _ = CompileScriptsAsync();

        /// <summary>Call on Unity's main thread. Concurrent requests share a task and queue one fresh compile; false means failure or shutdown.</summary>
        public static Task<bool> CompileScriptsAsync()
        {
            if (mainThreadId == 0)
                return Task.FromResult(false);

            if (Thread.CurrentThread.ManagedThreadId != mainThreadId)
                throw new InvalidOperationException("Request BOA compilation from Unity's main thread.");

            if (!instance || !Application.isPlaying)
                return Task.FromResult(false);

            if (activeCompilation != null)
            {
                activeCompilation.RecompileRequested = true;
                return activeCompilation.Completion.Task;
            }

            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            try
            {
                activeCompilation = StartCompilation(completion);
                NotifyCompilationState();
                Debug.Log("Start compiling".ToSubLog());
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                completion.TrySetResult(false);
            }

            return completion.Task;
        }

        static CompilationRequest StartCompilation(TaskCompletionSource<bool> completion)
        {
            // Snapshot Unity-owned state before scheduling; the worker only receives managed data.
            string directory = NUCLEOR.DFScripts.FullName;
            ScriptOptions options = assemblies;
            var request = new CompilationRequest(completion);
            var token = request.Cancellation.Token;

            try
            {
                request.Worker = Task.Run(() => BuildScripts(directory, options, token), token);
                return request;
            }
            catch
            {
                request.Cancellation.Dispose();
                throw;
            }
        }

        static CompilationOutput BuildScripts(string directory, ScriptOptions options, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var dscripts = Directory.CreateDirectory(directory);
            var comparer = Path.DirectorySeparatorChar == '\\' ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            var files = new SortedSet<string>(comparer);

            foreach (string file in EnumerateScriptFiles(dscripts.FullName, token))
                files.Add(file);

            var trees = new List<SyntaxTree>(files.Count);
            foreach (string file in files)
            {
                token.ThrowIfCancellationRequested();
                trees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file, cancellationToken: token));
            }

            token.ThrowIfCancellationRequested();
            var compilation = CSharpCompilation.Create(
                assemblyName: $"MACHINECODE_{Guid.NewGuid():N}",
                syntaxTrees: trees,
                references: CSharpScript.Create<object>(code: string.Empty, options: options).GetCompilation().References,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, concurrentBuild: false)
            );
            var output = new CompilationOutput { AssemblyName = compilation.AssemblyName };

            token.ThrowIfCancellationRequested();
            try
            {
                WriteScriptsProject(dscripts, compilation);
            }
            catch (Exception exception)
            {
                output.ProjectWarning = $"Could not generate the BOA scripts project: {exception.Message}";
            }

            using var stream = new MemoryStream();
            var result = compilation.Emit(stream, cancellationToken: token);
            output.Diagnostics = result.Diagnostics.ToArray();
            if (result.Success)
                output.AssemblyBytes = stream.ToArray();

            token.ThrowIfCancellationRequested();
            return output;
        }

        // Polled by the manager: no async continuation can revive Unity objects after Play Mode ends.
        static void UpdateCompilation()
        {
            var request = activeCompilation;
            if (request == null || !request.Worker.IsCompleted)
                return;

            bool success = false;
            try
            {
                var output = request.Worker.GetAwaiter().GetResult();
                if (!request.RecompileRequested)
                {
                    LogCompilation(output);
                    if (output.AssemblyBytes != null)
                        success = ReloadScripts(request, output);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                request.Cancellation.Dispose();
                if (ReferenceEquals(activeCompilation, request))
                {
                    if (request.RecompileRequested)
                        try
                        {
                            activeCompilation = StartCompilation(request.Completion);
                        }
                        catch (Exception exception)
                        {
                            Debug.LogException(exception);
                            FinishCompilation(request, false);
                        }
                    else
                        FinishCompilation(request, success);
                }
            }
        }

        static bool ReloadScripts(CompilationRequest request, CompilationOutput output)
        {
            ScriptLoadContext nextContext = null;
            try
            {
                nextContext = new ScriptLoadContext(output.AssemblyName);
                using var stream = new MemoryStream(output.AssemblyBytes, writable: false);
                var nextAssembly = nextContext.Load(stream);
                nextAssembly.GetTypes();

                Notify(beforeScriptsReload);
                if (!ReferenceEquals(activeCompilation, request))
                    return false;

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
            finally
            {
                nextContext?.Unload();
            }
        }

        static void FinishCompilation(CompilationRequest request, bool success)
        {
            activeCompilation = null;
            request.Completion.TrySetResult(success);
            NotifyCompilationState();
            Debug.Log("Done compiling".ToSubLog());
        }

        static void CancelCompilation()
        {
            var request = activeCompilation;
            if (request == null)
                return;

            activeCompilation = null;
            request.Cancellation.Cancel();
            request.Completion.TrySetResult(false);

            // Observe faults and release cancellation resources even when Unity stops ticking.
            _ = request.Worker.ContinueWith(task =>
            {
                _ = task.Exception;
                request.Cancellation.Dispose();
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            NotifyCompilationState();
        }

        static void NotifyCompilationState()
        {
            try
            {
                isCompiling.Value = IsCompiling;
            }
            catch (Exception exception)
            {
                // A UI listener must not prevent compilation completion or cancellation cleanup.
                Debug.LogException(exception);
            }
        }

        static void LogCompilation(CompilationOutput output)
        {
            if (log_compilationWarnings)
                if (output.ProjectWarning != null)
                    Debug.LogWarning(output.ProjectWarning);

            foreach (var diagnostic in output.Diagnostics)
                switch (diagnostic.Severity)
                {
                    case DiagnosticSeverity.Info:
                        Debug.Log(diagnostic.ToString());
                        break;

                    case DiagnosticSeverity.Warning:
                        if (log_compilationDiagnostics_warnings)
                            Debug.LogWarning(diagnostic.ToString());
                        break;

                    case DiagnosticSeverity.Error:
                        Debug.LogError(diagnostic.ToString());
                        break;

                    default:
                        Debug.Log(diagnostic.ToSubLog());
                        break;
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
