using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;

namespace _BOA_
{
    public static partial class BoaCode
    {
        public static string FormatCode(string code, ref int selectionAnchor, ref int selectionFocus)
        {
            if (code == null)
                throw new ArgumentNullException(nameof(code));

            var source = SourceText.From(code);
            string newLine = Environment.NewLine;
            foreach (var line in source.Lines)
                if (line.SpanIncludingLineBreak.Length > line.Span.Length)
                {
                    newLine = source.ToString(TextSpan.FromBounds(line.End, line.SpanIncludingLineBreak.End));
                    break;
                }

            var host = MefHostServices.Create(new[] { typeof(Workspace).Assembly, typeof(CSharpFormattingOptions).Assembly });
            using var workspace = new AdhocWorkspace(host);
            var options = workspace.Options
                .WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, true)
                .WithChangedOption(FormattingOptions.TabSize, LanguageNames.CSharp, 4)
                .WithChangedOption(FormattingOptions.IndentationSize, LanguageNames.CSharp, 4)
                .WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, newLine);

            var root = CSharpSyntaxTree.ParseText(source).GetRoot();
            var changes = Formatter.GetFormattedTextChanges(root, workspace, options).OrderBy(change => change.Span.Start).ToArray();
            string formatted = source.WithChanges(changes).ToString();

            bool forward = selectionAnchor <= selectionFocus;
            bool collapsed = selectionAnchor == selectionFocus;
            selectionAnchor = MapFormattedPosition(Math.Max(0, Math.Min(selectionAnchor, code.Length)), changes, forward);
            selectionFocus = MapFormattedPosition(Math.Max(0, Math.Min(selectionFocus, code.Length)), changes, !forward || collapsed);
            return formatted;
        }

        static int MapFormattedPosition(int position, TextChange[] changes, bool afterInsertion)
        {
            int offset = 0;
            foreach (var change in changes)
            {
                if (position < change.Span.Start)
                    break;

                if (position == change.Span.Start && change.Span.IsEmpty && !afterInsertion)
                    return position + offset;

                int newLength = change.NewText?.Length ?? 0;
                if (position < change.Span.End)
                    return change.Span.Start + offset + Math.Min(position - change.Span.Start, newLength);

                offset += newLength - change.Span.Length;
            }
            return position + offset;
        }
    }
}
