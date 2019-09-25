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

        public static T AddTrailingNewLine<T>(this T syntax) where T : SyntaxNode
        {
            return syntax.WithAdditionalAnnotations(TrailingNewLineAnnotation);
        }

        public static T AddLeadingNewLine<T>(this T syntax) where T : SyntaxNode
        {
            return syntax.WithAdditionalAnnotations(LeadingNewLineAnnotation);
        }

        public static string LeadingNewLine = "LeadingNewLine";
        public static string TrailingNewLine = "TrailingNewLine";

        public static SyntaxAnnotation LeadingNewLineAnnotation = new SyntaxAnnotation(LeadingNewLine);
        public static SyntaxAnnotation TrailingNewLineAnnotation = new SyntaxAnnotation(TrailingNewLine);
    }
}
