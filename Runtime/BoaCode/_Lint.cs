using _ARK_;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace _BOA_
{
    public static partial class BoaCode
    {

        //----------------------------------------------------------------------------------------------------------

        public static string HighlightCode(in string text, in LintTheme theme)
        {
            var root = CSharpSyntaxTree.ParseText(text, CSharpParseOptions.Default.WithKind(SourceCodeKind.Script)).GetRoot();

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

            return sb.ToString();

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
        }

        public static Task<string> GetCodeDiagnosticAsync(string text, LintTheme theme, CancellationToken cancellationToken = default)
        {
            // Snapshot Unity-owned data before starting the worker; it only handles Roslyn and strings.
            var options = BoaManager.assemblies;
            var colors = GetDiagnosticColors(theme);
            return Task.Run(() => GetCodeDiagnostic(text, options, colors, cancellationToken), cancellationToken);
        }

        static string[] GetDiagnosticColors(LintTheme theme) => new[] { ColorUtility.ToHtmlStringRGBA(theme.hidden), ColorUtility.ToHtmlStringRGBA(theme.info), ColorUtility.ToHtmlStringRGBA(theme.warning), ColorUtility.ToHtmlStringRGBA(theme.error) };

        static string GetCodeDiagnostic(string text, ScriptOptions options, string[] colors, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var compilation = CSharpScript.Create<object>(text, options: options).GetCompilation();
            foreach (var diagnostic in compilation.GetDiagnostics(cancellationToken))
            {
                var position = diagnostic.Location.GetLineSpan().StartLinePosition;
                return $"<color=#{colors[(int)diagnostic.Severity]}>{new string('\n', 1 + position.Line)}{new string(' ', position.Character)}└──> <b>{diagnostic.Id}</b>: {diagnostic.GetMessage()}</color>";
            }
            return null;
        }
    }
}
