using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace unit_test_generator
{
    public static class TestGenerationExtensions
    {
        public static SyntaxNode CreateTestFile(this CompilationUnitSyntax compilationUnit)
        {
            var namespaceName = compilationUnit.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault().Name.ToString();
            var usings = compilationUnit.Usings.Select(_ => _.Name.ToString());
            var classSyntax = compilationUnit.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();

            var testUsings = usings.Append("Xunit").Append("Moq");
            return CompilationUnit()
                .WithUsings(
                    List(
                        testUsings.Select(_ => UsingDirective(ParseName(_))).ToArray()))
                .WithMembers(
                    SingletonList<MemberDeclarationSyntax>(
                        NamespaceDeclaration(
                            ParseName(namespaceName.ToTestNamespace()))
                            .WithMembers(
                                SingletonList<MemberDeclarationSyntax>(classSyntax.CreateTestClass()))));
        }

        public static MethodDeclarationSyntax CreateAsyncTestMethod(this MethodDeclarationSyntax method, ClassDeclarationSyntax classSyntax)
        {
            var methodName = method.Identifier.Text;
            var className = classSyntax.Identifier.Text;
            var mockSetups = method.CreateSetupsForMocks(classSyntax);
            var statements = new List<StatementSyntax>(mockSetups){
                ParseStatement($"await {className.ToMemberName()}.{methodName}();")
            };
            var methodDeclaration = (ParseMemberDeclaration($"[Fact]public async Task {methodName.ToTestMethodName()}(){{}}") as MethodDeclarationSyntax);
            return methodDeclaration.WithBody(Block(statements));
        }

        public static IEnumerable<StatementSyntax> CreateSetupsForMocks(this MethodDeclarationSyntax method, ClassDeclarationSyntax classSyntax)
        {
            var fields = classSyntax.Members.OfType<FieldDeclarationSyntax>();
            var expressions = method.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
            return expressions
                // Select mockable expressions
                .Where(_ => fields.Any(m => m.Declaration.Variables.First().Identifier.Text == (_.Expression as IdentifierNameSyntax).Identifier.Text))
                .Select(_ => _.CreateMockSetup(fields));
        }

        public static StatementSyntax CreateMockSetup(this MemberAccessExpressionSyntax memberAccessExpression, IEnumerable<FieldDeclarationSyntax> availableFields)
        {
            var memberName = (memberAccessExpression.Expression as IdentifierNameSyntax).Identifier.Text;
            var field = availableFields.FirstOrDefault(_ => _.Declaration.Variables.First().Identifier.Text == memberName);
            var memberType = (field.Declaration.Type as IdentifierNameSyntax).Identifier.Text;
            var methodName = memberAccessExpression.Name.Identifier.Text;
            return ParseStatement($"{memberType.ToMemberName()}.Setup(_ => _.{methodName}());");
        }

        public static MemberDeclarationSyntax CreateVoidTestMethod(this MethodDeclarationSyntax method, ClassDeclarationSyntax classSyntax)
        {
            var methodName = method.Identifier.Text;
            var mockSetups = method.CreateSetupsForMocks(classSyntax);
            var statements = new List<StatementSyntax>(mockSetups){
                ParseStatement($"{classSyntax.Identifier.Text.ToMemberName()}.{methodName}();")
            };
            var methodDeclaration = (ParseMemberDeclaration($"[Fact]public void {methodName.ToTestMethodName()}(){{}}") as MethodDeclarationSyntax);
            return methodDeclaration.WithBody(Block(statements));
        }

        public static MemberDeclarationSyntax CreateMockAsField(this TypeSyntax type)
        {
            var typeName = (type as IdentifierNameSyntax).Identifier.Text;
            return ParseMemberDeclaration($"private readonly Mock<{typeName}> {typeName.ToMemberName()} = new Mock<{typeName}>();");
        }

        public static MemberDeclarationSyntax CreateConstructor(this ClassDeclarationSyntax classDeclaration)
        {
            var className = classDeclaration.Identifier.Text;
            var constructor = classDeclaration.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
            var parameters = constructor.ParameterList.Parameters;
            var arguments = parameters.Select(_ => (_.Type as IdentifierNameSyntax).Identifier.Text.ToMemberName() + ".Object");

            return ParseMemberDeclaration($"public {className.ToTestClass()}() {{ {className.ToMemberName()} = new {className}({string.Join(", ", arguments)}); }}");
        }

        public static MemberDeclarationSyntax CreateMemberOfClass(this ClassDeclarationSyntax classType)
        {
            var className = classType.Identifier.Text;
            return ParseMemberDeclaration($"private readonly {className} {className.ToMemberName()};");
        }

        public static ClassDeclarationSyntax CreateTestClass(this ClassDeclarationSyntax classSyntax)
        {
            var memberList = new List<MemberDeclarationSyntax>();
            var constructor = classSyntax.Members.OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
            var parameters = constructor.ParameterList.Parameters;

            memberList.AddRange(parameters.Select(_ => _.Type.CreateMockAsField()));
            memberList.Add(classSyntax.CreateMemberOfClass());
            memberList.Add(classSyntax.CreateConstructor());

            var methods = classSyntax.Members.OfType<MethodDeclarationSyntax>().Where(_ => _.Modifiers.Any(SyntaxKind.PublicKeyword));
            memberList.AddRange(methods.Where(_ => _.Modifiers.Any(SyntaxKind.AsyncKeyword)).Select(_ => _.CreateAsyncTestMethod(classSyntax)));
            memberList.AddRange(methods.Where(_ => !_.Modifiers.Any(SyntaxKind.AsyncKeyword)).Select(_ => _.CreateVoidTestMethod(classSyntax)));

            var className = classSyntax.Identifier.Text;

            return ClassDeclaration(className.ToTestClass())
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)))
                .WithMembers(List(memberList));
        }
    }
}
