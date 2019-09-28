using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Opiskull.UnitTestGenerator
{
    public static class TestGenerationExtensions
    {
        public static string GetTypeName(this TypeSyntax type)
        {
            return (type as IdentifierNameSyntax).Identifier.Text;
        }

        public static string CreateTestFileContent(this SemanticModel model)
        {
            return model.SyntaxTree.GetCompilationUnitRoot().CreateTestFileContent(model);
        }

        public static string CreateTestFileContent(this CompilationUnitSyntax compilationUnit, SemanticModel model)
        {
            var namespaceName = compilationUnit.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault().Name.ToString();
            var usings = new List<string>(compilationUnit.Usings.Select(_ => _.Name.ToString()));
            var classSyntax = compilationUnit.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();

            usings.AddRange(new[] { "Xunit", "Moq", "FluentAssertions", namespaceName });
            return CompilationUnit()
                .WithUsings(
                    List(
                        usings.Select(_ => UsingDirective(ParseName(_))).ToArray()))
                .WithMembers(
                    SingletonList<MemberDeclarationSyntax>(
                        NamespaceDeclaration(
                            ParseName(namespaceName.ToTestNamespace()))
                            .WithMembers(
                                SingletonList<MemberDeclarationSyntax>(classSyntax.CreateTestClass(model)))))
                .ApplyFormat()
                .ToString();
        }

        private static string ToMockedValue(this ITypeSymbol typeSymbol)
        {
            var typeName = typeSymbol.Name;
            if (typeName == typeof(int).Name || typeName == typeof(long).Name)
            {
                return "1";
            }
            if (typeName == typeof(string).Name)
            {
                return "\"test-value\"";
            }
            if (typeName == typeof(bool).Name)
            {
                return "true";
            }
            return "null";
        }

        private static string ToMockReturnValue(IMethodSymbol methodSymbol)
        {
            var returnType = methodSymbol.ReturnType;
            var namedType = returnType as INamedTypeSymbol;

            if (namedType.Name == typeof(Task).Name)
            {
                if (namedType.IsGenericType)
                {
                    var typeArgument = namedType.TypeArguments.FirstOrDefault();
                    if (typeArgument != null && typeArgument.IsValueType)
                    {
                        return $".ReturnsAsync({typeArgument.ToMockedValue()})";
                    }
                }
                return ".Returns(Task.CompletedTask)";
            }
            if (namedType.Name == "Void")
            {
                return "";
            }
            return $".Returns({namedType.ToMockedValue()})";
        }

        public static MethodDeclarationSyntax CreateTestMethod(this MethodDeclarationSyntax methodDeclarationSyntax, ClassDeclarationSyntax classSyntax, SemanticModel model)
        {
            var method = model.GetDeclaredSymbol(methodDeclarationSyntax);
            var methodName = method.Name;
            var className = method.ContainingType.Name;
            var arguments = method.Parameters.ToTypedArguments();

            var returnValue = !method.ReturnsVoid || ((method.ReturnType as INamedTypeSymbol).IsGenericType) ? "var result = " : "";

            var mockSetups = methodDeclarationSyntax.CreateSetupsForMocks(classSyntax, model);
            var statements = new List<StatementSyntax>(mockSetups){
                ParseStatement($"{returnValue}{(method.IsAsync ? "await" : "")} {className.ToMemberName()}.{methodName}({arguments});")
                    .AddLeadingNewLine(mockSetups.Any())
                    .AddTrailingNewLine(mockSetups.Any())
            };
            statements.AddRange(methodDeclarationSyntax.CreateVerifyAllForMocks(classSyntax));
            var methodDeclaration = (ParseMemberDeclaration($"[Fact]public {(method.IsAsync ? "async Task" : "void")} {methodName.ToTestMethodName()}()") as MethodDeclarationSyntax);
            return methodDeclaration.WithBody(Block(statements));
        }

        public static IEnumerable<StatementSyntax> CreateSetupsForMocks(this MethodDeclarationSyntax method, ClassDeclarationSyntax classSyntax, SemanticModel model)
        {
            var fields = classSyntax.Members.OfType<FieldDeclarationSyntax>();
            var expressions = method.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
            return expressions
                // Select mockable expressions
                .Where(_ => fields.Any(m => m.Declaration.Variables.First().Identifier.Text == (_.Expression as IdentifierNameSyntax).Identifier.Text))
                .Select(_ => _.CreateMockSetup(fields, model));
        }

        public static IEnumerable<StatementSyntax> CreateVerifyAllForMocks(this MethodDeclarationSyntax method, ClassDeclarationSyntax classSyntax)
        {
            var fields = classSyntax.Members.OfType<FieldDeclarationSyntax>();
            var expressions = method.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
            return expressions
                // Select mockable expressions
                .Where(_ => fields.Any(m => m.Declaration.Variables.First().Identifier.Text == (_.Expression as IdentifierNameSyntax).Identifier.Text))
                .Select(_ => _.CreateVerifyAll(fields))
                .Distinct()
                .Select(_ => ParseStatement(_));
        }

        private static string CreateVerifyAll(this MemberAccessExpressionSyntax memberAccessExpression, IEnumerable<FieldDeclarationSyntax> availableFields)
        {
            var memberName = (memberAccessExpression.Expression as IdentifierNameSyntax).Identifier.Text;
            var field = availableFields.FirstOrDefault(_ => _.Declaration.Variables.First().Identifier.Text == memberName);
            var memberType = field.Declaration.Type.GetTypeName();
            return $"{memberType.ToMemberName()}.VerifyAll();";
        }

        public static string ToTypedArguments(this IEnumerable<IParameterSymbol> parameters)
        {
            return string.Join(", ", parameters.Select(_ => ToMockedValue(_.Type)));
        }

        public static StatementSyntax CreateMockSetup(this MemberAccessExpressionSyntax memberAccessExpression, IEnumerable<FieldDeclarationSyntax> availableFields, SemanticModel model)
        {
            var memberName = (memberAccessExpression.Expression as IdentifierNameSyntax).Identifier.Text;
            var field = availableFields.FirstOrDefault(_ => _.Declaration.Variables.First().Identifier.Text == memberName);
            var memberType = field.Declaration.Type.GetTypeName();
            var methodName = memberAccessExpression.Name.Identifier.Text;

            var memberAccess = model.GetTypeInfo(memberAccessExpression.Expression);
            var symbolInfo = model.GetSymbolInfo(memberAccessExpression);
            var methodSymbol = (symbolInfo.Symbol as IMethodSymbol);
            var returnValue = ToMockReturnValue(methodSymbol);
            var arguments = methodSymbol.Parameters.ToTypedArguments();

            return ParseStatement($"{memberType.ToMemberName()}.Setup(_ => _.{methodName}({arguments})){returnValue};");
        }

        public static MemberDeclarationSyntax CreateMockAsField(this TypeSyntax type)
        {
            var typeName = type.GetTypeName();
            return ParseMemberDeclaration($"private readonly Mock<{typeName}> {typeName.ToMemberName()} = new Mock<{typeName}>();");
        }

        public static MemberDeclarationSyntax CreateConstructor(this ClassDeclarationSyntax classDeclaration)
        {
            var className = classDeclaration.Identifier.Text;
            var constructor = classDeclaration.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
            var parameters = constructor.ParameterList.Parameters;
            var arguments = parameters.Select(_ => _.Type.GetTypeName().ToMemberName() + ".Object");

            return ParseMemberDeclaration($"public {className.ToTestClass()}() {{ {className.ToMemberName()} = new {className}({string.Join(", ", arguments)}); }}")
                .AddLeadingNewLine();
        }

        public static MemberDeclarationSyntax CreateMemberOfClass(this ClassDeclarationSyntax classType)
        {
            var className = classType.Identifier.Text;
            return ParseMemberDeclaration($"private readonly {className} {className.ToMemberName()};");
        }

        public static ClassDeclarationSyntax CreateTestClass(this ClassDeclarationSyntax classSyntax, SemanticModel semanticModel)
        {
            var memberList = new List<MemberDeclarationSyntax>();
            var constructor = classSyntax.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
            var parameters = constructor.ParameterList.Parameters;

            memberList.AddRange(parameters.Select(_ => _.Type.CreateMockAsField()));
            memberList.Add(classSyntax.CreateMemberOfClass());
            memberList.Add(classSyntax.CreateConstructor());

            var methods = classSyntax.Members.OfType<MethodDeclarationSyntax>().Where(_ => _.Modifiers.Any(SyntaxKind.PublicKeyword));
            memberList.AddRange(methods.Select(_ => CreateTestMethod(_, classSyntax, semanticModel)));

            var className = classSyntax.Identifier.Text;

            return ClassDeclaration(className.ToTestClass())
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)))
                .WithMembers(List(memberList));
        }
    }
}
