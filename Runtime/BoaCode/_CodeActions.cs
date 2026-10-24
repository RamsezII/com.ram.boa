using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace _BOA_
{
    public sealed class CodeActionsResult : IDisposable
    {
        public readonly string[] Suggestions;
        readonly AdhocWorkspace workspace;
        readonly Document document;
        readonly CodeAction[] actions;
        readonly int cursor;

        internal CodeActionsResult(AdhocWorkspace workspace, Document document, CodeAction[] actions, int cursor)
        {
            this.workspace = workspace;
            this.document = document;
            this.actions = actions;
            this.cursor = cursor;
            Suggestions = actions.Select(action => action.Title).ToArray();
        }

        public Task<CodeCompletionEdit> GetChangeAsync(int index, CancellationToken cancellationToken = default)
        {
            if (index < 0 || index >= actions.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            return Task.Run(async () =>
            {
                var operations = await actions[index].GetOperationsAsync(cancellationToken).ConfigureAwait(false);
                var operation = operations.OfType<ApplyChangesOperation>().SingleOrDefault() ?? throw new InvalidOperationException("The correction does not produce a document edit.");
                var changed = operation.ChangedSolution.GetDocument(document.Id) ?? throw new InvalidOperationException("The correction removed the current document.");
                string text = (await changed.GetTextAsync(cancellationToken).ConfigureAwait(false)).ToString();
                int position = cursor;
                foreach (var edit in (await changed.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false)).OrderBy(edit => edit.Span.Start))
                {
                    if (edit.Span.Start > cursor)
                        break;
                    int replaced = Math.Min(cursor, edit.Span.End) - edit.Span.Start;
                    position += (edit.NewText?.Length ?? 0) - replaced;
                }
                return new CodeCompletionEdit(text, Math.Max(0, Math.Min(position, text.Length)));
            }, cancellationToken);
        }

        public void Dispose() => workspace.Dispose();
    }

    public static partial class BoaCode
    {
        [NoAutoStaticsCleanup] static readonly Lazy<CodeFixProvider[]> importFixProviders = new(() => completionComposition.Value.GetExports<Lazy<CodeFixProvider, IDictionary<string, object>>>().Where(export => export.Metadata.TryGetValue("Name", out var name) && name is "AddImport" or "FullyQualify").OrderBy(export => (string)export.Metadata["Name"] == "AddImport" ? 0 : 1).Select(export => export.Value).ToArray());

        public static Task<CodeActionsResult> GetCodeActionsAsync(string code, int cursor, string filePath = null, string scriptsDirectory = null, CancellationToken cancellationToken = default)
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
                    var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                    var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    var actions = new List<CodeAction>();
                    foreach (var diagnostic in compilation.GetDiagnostics(cancellationToken))
                    {
                        var span = diagnostic.Location.SourceSpan;
                        if (diagnostic.Location.SourceTree != tree || cursor < span.Start || cursor > span.End)
                            continue;
                        foreach (var provider in importFixProviders.Value)
                            if (provider.FixableDiagnosticIds.Contains(diagnostic.Id))
                                await provider.RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic, (action, diagnostics) => AddAction(action), cancellationToken)).ConfigureAwait(false);
                    }

                    return new CodeActionsResult(workspace, document, actions.GroupBy(action => action.Title, StringComparer.Ordinal).Select(group => group.First()).ToArray(), cursor);

                    void AddAction(CodeAction action)
                    {
                        if (action.NestedActions.Length == 0)
                            actions.Add(action);
                        else
                            foreach (var nested in action.NestedActions)
                                AddAction(nested);
                    }
                }
                catch
                {
                    workspace.Dispose();
                    throw;
                }
            }, cancellationToken);
        }
    }
}
