using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Opiskull.UnitTestGenerator
{
    public static class NewLineExtensions
    {
        public static SyntaxNode ApplyFormat(this SyntaxNode syntax)
        {
            syntax = syntax.NormalizeWhitespace();
            return new NewLineRewriter().Visit(syntax);
        }

        public static T AddTrailingNewLine<T>(this T syntax, bool add = true) where T : SyntaxNode
        {
            return add ? syntax.WithAdditionalAnnotations(TrailingNewLineAnnotation) : syntax;
        }

        public static T AddLeadingNewLine<T>(this T syntax, bool add = true) where T : SyntaxNode
        {
            return add ? syntax.WithAdditionalAnnotations(LeadingNewLineAnnotation) : syntax;
        }

        public static string LeadingNewLine = "LeadingNewLine";
        public static string TrailingNewLine = "TrailingNewLine";

        public static SyntaxAnnotation LeadingNewLineAnnotation = new SyntaxAnnotation(LeadingNewLine);
        public static SyntaxAnnotation TrailingNewLineAnnotation = new SyntaxAnnotation(TrailingNewLine);
    }
}
