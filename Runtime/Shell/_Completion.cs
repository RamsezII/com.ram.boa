using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace _BOA_
{
    public readonly struct CodeCompletionResult
    {
        public readonly int Start, End;
        public readonly string[] Suggestions;

        public CodeCompletionResult(int start, int end, string[] suggestions)
        {
            Start = start;
            End = end;
            Suggestions = suggestions;
        }
    }

    partial class Shell
    {
        public static Task<CodeCompletionResult> CompleteCodeAsync(string code, int cursor, string filePath = null, string scriptsDirectory = null, CancellationToken cancellationToken = default)
        {
            if (code == null)
                throw new ArgumentNullException(nameof(code));
            if (cursor < 0 || cursor > code.Length)
                throw new ArgumentOutOfRangeException(nameof(cursor));

            var options = BoaManager.assemblies ?? ScriptOptions.Default.AddReferences(typeof(UnityEngine.Vector3).Assembly, typeof(Boa.Main).Assembly);
            return Task.Run(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                int start = cursor, end = cursor;
                while (start > 0 && SyntaxFacts.IsIdentifierPartCharacter(code[start - 1]))
                    --start;
                if (start > 0 && code[start - 1] == '@')
                    --start;
                while (end < code.Length && SyntaxFacts.IsIdentifierPartCharacter(code[end]))
                    ++end;
                string prefix = code[start..cursor].TrimStart('@');

                var host = MefHostServices.Create(new[] { typeof(Workspace).Assembly, typeof(CSharpFormattingOptions).Assembly });
                using var workspace = new AdhocWorkspace(host);
                var references = CSharpScript.Create<object>(string.Empty, options).GetCompilation().References;
                var project = workspace.AddProject("BOA.Completion", LanguageNames.CSharp).WithMetadataReferences(references).WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                if (!string.IsNullOrEmpty(scriptsDirectory) && Directory.Exists(scriptsDirectory))
                    foreach (string path in Directory.EnumerateFiles(scriptsDirectory, "*.cs", SearchOption.AllDirectories))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!string.IsNullOrEmpty(filePath) && string.Equals(Path.GetFullPath(path), Path.GetFullPath(filePath), Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                            continue;
                        project = project.AddDocument(Path.GetFileName(path), SourceText.From(File.ReadAllText(path)), filePath: path).Project;
                    }

                var document = project.AddDocument(Path.GetFileName(filePath ?? "Untitled.cs"), SourceText.From(code), filePath: filePath);
                var symbols = await Recommender.GetRecommendedSymbolsAtPositionAsync(document, cursor, cancellationToken: cancellationToken).ConfigureAwait(false);
                string[] suggestions = symbols.Where(symbol => symbol.CanBeReferencedByName && symbol.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).Select(symbol => SyntaxFacts.GetKeywordKind(symbol.Name) != SyntaxKind.None ? "@" + symbol.Name : symbol.Name).Distinct(StringComparer.Ordinal).OrderBy(name => name.TrimStart('@').StartsWith(prefix, StringComparison.Ordinal) ? 0 : 1).ThenBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
                return new CodeCompletionResult(start, end, suggestions);
            }, cancellationToken);
        }
    }
}
