using _ARK_;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Linq;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace _BOA_
{
    partial class Shell
    {
        [NoAutoStaticsCleanup]
        static readonly CodeInterpreter static_interpreter = new(name: "BOA", extension: ".boa.txt")
        {
            linter = (in string text, in int index, in LintTheme theme, out string lint, out string error) =>
            {
                error = null;

                var script = CSharpScript.Create<object>(text);
                var compilation = script.GetCompilation();
                var tree = compilation.SyntaxTrees.Single();
                var root = tree.GetRoot();

                foreach (var diagnostic in compilation.GetDiagnostics())
                {
                    Color color = diagnostic.Severity switch
                    {
                        DiagnosticSeverity.Hidden => theme.hidden,
                        DiagnosticSeverity.Info => theme.info,
                        DiagnosticSeverity.Warning => theme.warning,
                        DiagnosticSeverity.Error => theme.error,
                        _ => throw new System.NotImplementedException(),
                    };
                    var position = diagnostic.Location.GetLineSpan().StartLinePosition;
                    error = $"{new string('\n', 1 + position.Line)}{new string(' ', position.Character)}└──> {diagnostic.Id.Bold()}: {diagnostic.GetMessage()}".SetColor(color);
                    break;
                }

                var sb = new StringBuilder();
                int cursor = 0;

                foreach (var token in root.DescendantTokens())
                    if (token.Span.Length > 0)
                    {
                        if (token.SpanStart > cursor)
                            sb.Append(text[cursor..token.SpanStart]);
                        cursor = token.Span.End;
                        sb.Append(token.Text.SetColor(GetTokenColor(token, theme)));
                    }

                if (cursor < text.Length)
                    sb.Append(text[cursor..]);

                lint = sb.ToString();

                static Color GetTokenColor(SyntaxToken token, LintTheme theme)
                {
                    var kind = token.Kind();

                    if (SyntaxFacts.IsKeywordKind(kind))
                        return theme.keywords;

                    return kind switch
                    {
                        SyntaxKind.NumericLiteralToken => theme.literal,
                        SyntaxKind.CharacterLiteralToken => theme.literal,
                        SyntaxKind.StringLiteralToken => theme.strings,
                        SyntaxKind.IdentifierToken => theme.variables,
                        _ => theme.fallback_default,
                    };
                }
            },
        };

        //----------------------------------------------------------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InitInterpreter()
        {
            CodeInterpreter.instances.Add(static_interpreter);
        }
    }
}