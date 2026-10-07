using _ARK_;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

                var options = new CSharpParseOptions(kind: SourceCodeKind.Script);
                var tree = CSharpSyntaxTree.ParseText(text, options);
                var root = tree.GetRoot();

                var sb = new StringBuilder();
                int cursor = 0;

                foreach (var token in root.DescendantTokens())
                    if (token.Span.Length > 0)
                    {
                        if (token.SpanStart > cursor)
                            sb.Append(text[cursor..token.SpanStart].SetColor(theme.fallback_default));
                        cursor = token.Span.End;
                        sb.Append(token.Text.SetColor(GetTokenColor(token, theme)));
                    }

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