using _ARK_;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            string source = text;
            Color commentColor = theme.comments;

            foreach (var token in root.DescendantTokens())
            {
                foreach (var trivia in token.LeadingTrivia)
                    AppendComment(trivia);

                if (token.Span.Length > 0)
                    AppendColored(token.SpanStart, token.Span.End, GetTokenColor(token, theme));

                foreach (var trivia in token.TrailingTrivia)
                    AppendComment(trivia);
            }

            if (cursor < text.Length)
                sb.Append(text[cursor..]);

            return sb.ToString();

            void AppendColored(int start, int end, Color color)
            {
                if (start > cursor)
                    sb.Append(source[cursor..start]);
                sb.Append(source[start..end].SetColor(color));
                cursor = end;
            }

            void AppendComment(SyntaxTrivia trivia)
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                    AppendColored(trivia.SpanStart, trivia.Span.End, commentColor);
            }

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
                    SyntaxKind.InterpolatedStringStartToken => theme.strings,
                    SyntaxKind.InterpolatedVerbatimStringStartToken => theme.strings,
                    SyntaxKind.InterpolatedStringTextToken => theme.strings,
                    SyntaxKind.InterpolatedStringEndToken => theme.strings,
                    SyntaxKind.IdentifierToken => GetIdentifierColor(token, theme),
                    SyntaxKind.PlusToken or SyntaxKind.MinusToken or SyntaxKind.AsteriskToken or SyntaxKind.SlashToken or SyntaxKind.PercentToken or SyntaxKind.EqualsToken or SyntaxKind.EqualsEqualsToken or SyntaxKind.ExclamationEqualsToken or SyntaxKind.LessThanToken or SyntaxKind.GreaterThanToken or SyntaxKind.LessThanEqualsToken or SyntaxKind.GreaterThanEqualsToken or SyntaxKind.AmpersandAmpersandToken or SyntaxKind.BarBarToken or SyntaxKind.ExclamationToken or SyntaxKind.QuestionQuestionToken => theme.operators,
                    _ => theme.fallback_default,
                };
            }

            static Color GetIdentifierColor(SyntaxToken token, LintTheme theme)
            {
                var node = token.Parent;
                if (node is MethodDeclarationSyntax method && method.Identifier == token || node is LocalFunctionStatementSyntax localFunction && localFunction.Identifier == token)
                    return theme.functions;
                if (node is BaseTypeDeclarationSyntax type && type.Identifier == token || node is DelegateDeclarationSyntax delegateType && delegateType.Identifier == token)
                    return theme.types;

                if (node is SimpleNameSyntax name)
                {
                    if (name.Parent is InvocationExpressionSyntax invocation && invocation.Expression == name || name.Parent is MemberAccessExpressionSyntax member && member.Name == name && member.Parent is InvocationExpressionSyntax memberInvocation && memberInvocation.Expression == member)
                        return theme.functions;

                    SyntaxNode typeName = name;
                    while (typeName.Parent is QualifiedNameSyntax || typeName.Parent is AliasQualifiedNameSyntax || typeName.Parent is NullableTypeSyntax || typeName.Parent is ArrayTypeSyntax)
                        typeName = typeName.Parent;

                    if (typeName.Parent is VariableDeclarationSyntax variable && variable.Type == typeName || typeName.Parent is ParameterSyntax parameter && parameter.Type == typeName || typeName.Parent is ObjectCreationExpressionSyntax creation && creation.Type == typeName || typeName.Parent is MethodDeclarationSyntax declaration && declaration.ReturnType == typeName || typeName.Parent is CastExpressionSyntax cast && cast.Type == typeName || typeName.Parent is TypeArgumentListSyntax || typeName.Parent is BaseTypeSyntax)
                        return theme.types;
                }
                return theme.variables;
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
