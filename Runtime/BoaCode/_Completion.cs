using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace _BOA_
{
    public readonly struct CodeCompletionEdit
    {
        public readonly string Text;
        public readonly int Cursor;

        public CodeCompletionEdit(string text, int cursor)
        {
            Text = text;
            Cursor = cursor;
        }
    }

    public sealed class CodeCompletionResult : IDisposable
    {
        public readonly int Start, End;
        public readonly string[] Suggestions;
        readonly AdhocWorkspace workspace;
        readonly Document document;
        readonly CompletionService service;
        readonly CompletionItem[] items;
        readonly string source;

        internal CodeCompletionResult(AdhocWorkspace workspace, Document document, CompletionService service, CompletionItem[] items, string source, int start, int end)
        {
            this.workspace = workspace;
            this.document = document;
            this.service = service;
            this.items = items;
            this.source = source;
            Start = start;
            End = end;
            Suggestions = items.Select(item => item.DisplayTextPrefix + item.DisplayText + item.DisplayTextSuffix).ToArray();
        }

        public Task<CodeCompletionEdit> GetChangeAsync(string suggestion, CancellationToken cancellationToken = default)
        {
            int index = Array.IndexOf(Suggestions, suggestion);
            if (index < 0)
                throw new ArgumentException("Unknown completion item.", nameof(suggestion));
            return Task.Run(async () =>
            {
                var change = await service.GetChangeAsync(document, items[index], cancellationToken: cancellationToken).ConfigureAwait(false);
                var edit = change.TextChange;
                string text = source[..edit.Span.Start] + edit.NewText + source[edit.Span.End..];
                return new CodeCompletionEdit(text, change.NewPosition ?? edit.Span.Start + (edit.NewText?.Length ?? 0));
            }, cancellationToken);
        }

        public void Dispose() => workspace.Dispose();
    }

    public static partial class BoaCode
    {
        [NoAutoStaticsCleanup] static readonly Lazy<CompositionHost> completionComposition = new(() => new ContainerConfiguration().WithAssemblies(new[] { typeof(Workspace).Assembly, typeof(CSharpFormattingOptions).Assembly, typeof(CompletionService).Assembly, Assembly.Load("Microsoft.CodeAnalysis.CSharp.Features") }).CreateContainer());
        [NoAutoStaticsCleanup] static readonly Lazy<MefHostServices> completionHost = new(() => MefHostServices.Create(completionComposition.Value));

        public static Task<CodeCompletionResult> CompleteCodeAsync(string code, int cursor, string filePath = null, string scriptsDirectory = null, CancellationToken cancellationToken = default, bool explicitInvocation = false)
        {
            if (code == null)
                throw new ArgumentNullException(nameof(code));
            if (cursor < 0 || cursor > code.Length)
                throw new ArgumentOutOfRangeException(nameof(cursor));

            var options = BoaManager.assemblies ?? ScriptOptions.Default.AddReferences(MetadataReference.CreateFromFile(typeof(UnityEngine.Vector3).Assembly.GetLoadedAssemblyPath()), MetadataReference.CreateFromFile(typeof(Boa.Init).Assembly.GetLoadedAssemblyPath()));
            return Task.Run(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var workspace = new AdhocWorkspace(completionHost.Value);
                try
                {
                    var document = CreateCodeDocument(workspace, code, filePath, scriptsDirectory, options, cancellationToken);
                    var service = CompletionService.GetService(document) ?? throw new InvalidOperationException("C# completion service is unavailable.");
                    // ScriptView requests a fresh list as the prefix changes, without a persistent Roslyn completion session.
                    var trigger = !explicitInvocation && cursor > 0 && code[cursor - 1] is ' ' or ':' ? CompletionTrigger.CreateInsertionTrigger(code[cursor - 1]) : CompletionTrigger.Invoke;
                    var list = await service.GetCompletionsAsync(document, cursor, trigger, cancellationToken: cancellationToken).ConfigureAwait(false);
                    int start = list?.Span.Start ?? cursor, end = list?.Span.End ?? cursor;
                    string prefix = code[start..cursor].TrimStart('@');
                    var items = list == null ? Array.Empty<CompletionItem>() : list.ItemsList.Where(item => item.FilterText.TrimStart('@').StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).OrderBy(item => item.FilterText.TrimStart('@').StartsWith(prefix, StringComparison.Ordinal) ? 0 : 1).ThenBy(item => item.SortText, StringComparer.OrdinalIgnoreCase).GroupBy(item => item.DisplayTextPrefix + item.DisplayText + item.DisplayTextSuffix, StringComparer.Ordinal).Select(group => group.First()).Take(100).ToArray();
                    return new CodeCompletionResult(workspace, document, service, items, code, start, end);
                }
                catch
                {
                    workspace.Dispose();
                    throw;
                }
            }, cancellationToken);
        }

        static Document CreateCodeDocument(AdhocWorkspace workspace, string code, string filePath, string scriptsDirectory, ScriptOptions options, CancellationToken cancellationToken)
        {
            var references = CSharpScript.Create<object>(string.Empty, options).GetCompilation().References;
            var project = workspace.AddProject("BOA.Code", LanguageNames.CSharp).WithMetadataReferences(references).WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            string documentPath = Path.GetFullPath(filePath ?? "Untitled.cs");
            string formatting = "root = true\n[*]\nindent_style = tab\nindent_size = 4\ntab_width = 4\nend_of_line = " + (code.Contains("\r\n") ? "crlf" : "lf");
            project = project.AddAnalyzerConfigDocument(".editorconfig", SourceText.From(formatting), filePath: Path.Combine(Path.GetDirectoryName(documentPath), ".editorconfig")).Project;

            if (!string.IsNullOrEmpty(scriptsDirectory) && Directory.Exists(scriptsDirectory))
                foreach (string path in BoaManager.EnumerateScriptFiles(scriptsDirectory))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!string.IsNullOrEmpty(filePath) && string.Equals(Path.GetFullPath(path), documentPath, Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                        continue;
                    project = project.AddDocument(Path.GetFileName(path), SourceText.From(File.ReadAllText(path)), filePath: path).Project;
                }

            return project.AddDocument(Path.GetFileName(documentPath), SourceText.From(code), filePath: documentPath);
        }
    }
}
