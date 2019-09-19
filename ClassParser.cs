using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace unit_test_generator
{
    public class ClassParser
    {
        public TestFile Parse(string classContent)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(classContent);
            var compilationUnitRoot = syntaxTree.GetCompilationUnitRoot();
            var usings = compilationUnitRoot.DescendantNodes().OfType<UsingDirectiveSyntax>();
            var namespaceSyntax = compilationUnitRoot.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            var classSyntax = namespaceSyntax.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();

            return new TestFile
            {
                ClassName = GetClassName(classSyntax),
                Dependencies = GetInjectedDependencies(classSyntax),
                AsyncMethods = GetAsyncMethodNames(classSyntax),
                NonAsyncMethods = GetNonAsyncMethodNames(classSyntax),
                Namespace = GetNamespace(namespaceSyntax),
                Usings = GetUsings(compilationUnitRoot)
            };
        }

        private string[] GetUsings(CompilationUnitSyntax syntax)
        {
            var usings = syntax.DescendantNodes().OfType<UsingDirectiveSyntax>();
            return usings.Select(_ => _.Name.ToString()).ToArray();
        }

        private string[] GetInjectedDependencies(ClassDeclarationSyntax syntax)
        {
            var constructor = syntax.DescendantNodes().OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
            var parameterTypes = constructor.DescendantNodes().OfType<ParameterListSyntax>().FirstOrDefault().Parameters.Select(_ => _.Type);
            return parameterTypes.Select(_ => (_ as IdentifierNameSyntax).Identifier.Text).ToArray();
        }

        private string[] GetAsyncMethodNames(ClassDeclarationSyntax syntax)
        {
            return syntax.DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Where(_ => _.Modifiers.Any(s => s.IsKind(SyntaxKind.PublicKeyword)))
                .Where(_ => _.Modifiers.Any(s => s.IsKind(SyntaxKind.AsyncKeyword)))
                .Select(_ => _.Identifier.Text).ToArray();
        }

        private string[] GetNonAsyncMethodNames(ClassDeclarationSyntax syntax)
        {
            return syntax.DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Where(_ => _.Modifiers.Any(s => s.IsKind(SyntaxKind.PublicKeyword)))
                .Where(_ => !_.Modifiers.Any(s => s.IsKind(SyntaxKind.AsyncKeyword)))
                .Select(_ => _.Identifier.Text).ToArray();
        }

        private string GetClassName(ClassDeclarationSyntax syntax)
        {
            return syntax.Identifier.Text;
        }

        private string GetNamespace(NamespaceDeclarationSyntax syntax)
        {
            return syntax.Name.ToString();
        }
    }
}
