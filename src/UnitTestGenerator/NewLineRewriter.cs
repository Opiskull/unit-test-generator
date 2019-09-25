using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Opiskull.UnitTestGenerator
{
    public class NewLineRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode Visit(SyntaxNode node)
        {
            if (node == null)
            {
                return null;
            }
            if (node.HasAnnotation(NewLineExtensions.TrailingNewLineAnnotation))
            {
                var trivia = node.GetTrailingTrivia().Add(SyntaxFactory.CarriageReturnLineFeed);
                node = node.WithTrailingTrivia(trivia);
            }
            if (node.HasAnnotation(NewLineExtensions.LeadingNewLineAnnotation))
            {
                var trivia = node.GetLeadingTrivia().Insert(0, SyntaxFactory.CarriageReturnLineFeed);
                node = node.WithLeadingTrivia(trivia);
            }
            return base.Visit(node);
        }
    }
}