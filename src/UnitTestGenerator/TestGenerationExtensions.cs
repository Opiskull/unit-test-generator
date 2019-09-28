using System;
using System.Collections;
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
            var compilationUnit = model.SyntaxTree.GetCompilationUnitRoot();
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
            if (typeSymbol.TypeKind == TypeKind.Array)
            {
                var arrayType = typeSymbol as IArrayTypeSymbol;
                return $"new {arrayType.ElementType.Name}[0]";
            }
            if (typeName == typeof(IEnumerable).Name || typeName == typeof(List<>).Name)
            {
                var genericType = typeSymbol as INamedTypeSymbol;
                return $"new List<{genericType.TypeArguments.First().Name}>()";
            }
            return "null";
        }

        private static string ToMockReturnValue(IMethodSymbol methodSymbol)
        {
            var returnType = methodSymbol.ReturnType;

            if (returnType.Name == typeof(Task).Name)
            {
                var namedType = returnType as INamedTypeSymbol;
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
            if (returnType.Name == "Void")
            {
                return "";
            }
            return $".Returns({returnType.ToMockedValue()})";
        }

        public static MethodDeclarationSyntax CreateTestMethod(this MethodDeclarationSyntax methodDeclarationSyntax, SemanticModel model)
        {
            var method = model.GetDeclaredSymbol(methodDeclarationSyntax);
            var methodName = method.Name;
            var className = method.ContainingType.Name;
            var arguments = method.Parameters.ToTypedArguments();

            var returnValue = !method.ReturnsVoid || ((method.ReturnType as INamedTypeSymbol).IsGenericType) ? "var result = " : "";

            var mockSetups = methodDeclarationSyntax.CreateMockSetups(model);
            var statements = new List<StatementSyntax>(mockSetups){
                ParseStatement($"{returnValue}{(method.IsAsync ? "await" : "")} {className.ToMemberName()}.{methodName}({arguments});")
                    .AddLeadingNewLine(mockSetups.Any())
                    .AddTrailingNewLine(mockSetups.Any())
            };
            statements.AddRange(methodDeclarationSyntax.CreateVerifyAllForMocks(model));
            var methodDeclaration = (ParseMemberDeclaration($"[Fact]public {(method.IsAsync ? "async Task" : "void")} {methodName.ToTestMethodName()}()") as MethodDeclarationSyntax);
            return methodDeclaration.WithBody(Block(statements));
        }

        public static IEnumerable<StatementSyntax> CreateMockSetups(this MethodDeclarationSyntax methodSyntax, SemanticModel model)
        {
            var method = model.GetDeclaredSymbol(methodSyntax);
            var fields = method.ContainingType.GetMembers().OfType<IFieldSymbol>();
            var expressions = methodSyntax.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
            return expressions
                // Select mockable expressions
                .Where(_ => fields.Any(m => m.Name == (_.Expression as IdentifierNameSyntax).Identifier.Text))
                .Select(_ => _.CreateMockSetup(fields, model));
        }

        public static IEnumerable<StatementSyntax> CreateVerifyAllForMocks(this MethodDeclarationSyntax methodSyntax, SemanticModel model)
        {
            var method = model.GetDeclaredSymbol(methodSyntax);
            var fields = method.ContainingType.GetMembers().OfType<IFieldSymbol>();
            var expressions = methodSyntax.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
            return expressions
                // Select mockable expressions
                .Where(_ => fields.Any(m => m.Name == (_.Expression as IdentifierNameSyntax).Identifier.Text))
                .Select(_ => _.CreateVerifyAll(fields))
                .Distinct()
                .Select(_ => ParseStatement(_));
        }

        private static string CreateVerifyAll(this MemberAccessExpressionSyntax memberAccessExpression, IEnumerable<IFieldSymbol> availableFields)
        {
            var memberName = (memberAccessExpression.Expression as IdentifierNameSyntax).Identifier.Text;
            var field = availableFields.FirstOrDefault(_ => _.Name == memberName);
            var memberType = field.Type.Name;
            return $"{memberType.ToMemberName()}.VerifyAll();";
        }

        public static string ToTypedArguments(this IEnumerable<IParameterSymbol> parameters)
        {
            return string.Join(", ", parameters.Select(_ => ToMockedValue(_.Type)));
        }

        public static StatementSyntax CreateMockSetup(this MemberAccessExpressionSyntax memberAccessExpression, IEnumerable<IFieldSymbol> availableFields, SemanticModel model)
        {
            var memberName = model.GetSymbolInfo(memberAccessExpression.Expression).Symbol.Name;
            var fieldName = availableFields.FirstOrDefault(_ => _.Name == memberName).Type.Name;
            var methodSymbol = model.GetSymbolInfo(memberAccessExpression).Symbol as IMethodSymbol;

            var returnValue = ToMockReturnValue(methodSymbol);
            var arguments = methodSymbol.Parameters.ToTypedArguments();

            return ParseStatement($"{fieldName.ToMemberName()}.Setup(_ => _.{methodSymbol.Name}({arguments})){returnValue};");
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
            memberList.AddRange(methods.Select(_ => CreateTestMethod(_, semanticModel)));

            var className = classSyntax.Identifier.Text;

            return ClassDeclaration(className.ToTestClass())
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)))
                .WithMembers(List(memberList));
        }
    }
}
