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
        public static IEnumerable<MemberAccessExpressionSyntax> SelectExpressionsWithName(this MethodDeclarationSyntax syntax, IEnumerable<FieldDeclarationSyntax> members)
        {
            var expressions = syntax.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
            return expressions.Where(_ => members.Any(m => m.Declaration.Variables.First().Identifier.Text == (_.Expression as IdentifierNameSyntax).Identifier.Text));
        }

        public static NameSyntax QualifiedName(string dotedName)
        {
            return GetQualifiedName(new List<string>(dotedName.Split(".").Reverse()));
        }

        private static NameSyntax GetQualifiedName(List<string> items)
        {
            var item = items.FirstOrDefault();
            if (items.Count == 1)
            {
                return IdentifierName(item);
            }
            items.RemoveAt(0);
            return SyntaxFactory.QualifiedName(GetQualifiedName(items), IdentifierName(item));
        }

        public static SyntaxNode CreateTestFile(this CompilationUnitSyntax compilationUnit)
        {
            var namespaceName = compilationUnit.DescendantNodes().OfType<NamespaceDeclarationSyntax>().FirstOrDefault().Name.ToString();
            var usings = compilationUnit.DescendantNodes().OfType<UsingDirectiveSyntax>().Select(_ => _.Name.ToString());
            var classSyntax = compilationUnit.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();

            var testUsings = usings.Append("Xunit").Append("Moq");
            return CompilationUnit()
                .WithUsings(
                    List<UsingDirectiveSyntax>(
                        testUsings.Select(_ => UsingDirective(QualifiedName(_))).ToArray()))
                .WithMembers(
                    SingletonList<MemberDeclarationSyntax>(
                        NamespaceDeclaration(
                            QualifiedName(namespaceName + ".Test"))
                            .WithMembers(
                                SingletonList<MemberDeclarationSyntax>(classSyntax.CreateTestClass()))));
        }

        public static MethodDeclarationSyntax CreateAsyncTestMethod(this MethodDeclarationSyntax method, ClassDeclarationSyntax classSyntax)
        {
            var methodName = method.Identifier.Text;

            var fields = classSyntax.Members.OfType<FieldDeclarationSyntax>();
            var expressions = method.SelectExpressionsWithName(fields);
            var mockSetups = expressions.Select(_ => _.CreateMockSetup(fields));

            var statements = new List<StatementSyntax>(mockSetups){
            ExpressionStatement(
                AwaitExpression(InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(classSyntax.Identifier.Text.ToMemberName()),
                        IdentifierName(methodName)))))
            };
            return
                MethodDeclaration(
                    IdentifierName("Task"),
                    Identifier(methodName.ToTestMethodName()))
                .WithModifiers(
                    TokenList(
                        new[]{
                            Token(SyntaxKind.PublicKeyword),
                            Token(SyntaxKind.AsyncKeyword)}))
                .WithAttributeLists(
                    SingletonList<AttributeListSyntax>(
                        AttributeList(
                            SingletonSeparatedList<AttributeSyntax>(
                                Attribute(
                                    IdentifierName("Fact"))))))
                .WithBody(
                    Block(
                        statements
                    ));
        }

        public static ExpressionStatementSyntax CreateMockSetup(this MemberAccessExpressionSyntax memberAccessExpression, IEnumerable<FieldDeclarationSyntax> availableFields)
        {
            var memberName = (memberAccessExpression.Expression as IdentifierNameSyntax).Identifier.Text;
            var field = availableFields.FirstOrDefault(_ => _.Declaration.Variables.First().Identifier.Text == memberName);
            var memberType = (field.Declaration.Type as IdentifierNameSyntax).Identifier.Text;
            var methodName = memberAccessExpression.Name.Identifier.Text;
            return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(memberType.ToMemberName()),
                            IdentifierName("Setup")))
                    .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList<ArgumentSyntax>(
                                Argument(
                                    SimpleLambdaExpression(
                                        Parameter(
                                            Identifier("_")),
                                        InvocationExpression(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("_"),
                                                IdentifierName(methodName)))))))));
        }

        public static MethodDeclarationSyntax CreateVoidTestMethod(this MethodDeclarationSyntax method, ClassDeclarationSyntax classSyntax)
        {
            var methodName = method.Identifier.Text;

            var fields = classSyntax.Members.OfType<FieldDeclarationSyntax>();
            var expressions = method.SelectExpressionsWithName(fields);
            var mockSetups = expressions.Select(_ => _.CreateMockSetup(fields));

            var statements = new List<StatementSyntax>(mockSetups){
            ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(classSyntax.Identifier.Text.ToMemberName()),
                        IdentifierName(methodName))))
            };
            return
                MethodDeclaration(
                    PredefinedType(
                        Token(SyntaxKind.VoidKeyword)),
                    Identifier(methodName.ToTestMethodName()))
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)))
                .WithAttributeLists(
                    SingletonList<AttributeListSyntax>(
                        AttributeList(
                            SingletonSeparatedList<AttributeSyntax>(
                                Attribute(
                                    IdentifierName("Fact"))))))
                .WithBody(
                    Block(statements));
        }

        public static FieldDeclarationSyntax CreateMockAsField(this TypeSyntax type)
        {
            var typeName = (type as IdentifierNameSyntax).Identifier.Text;
            return
                FieldDeclaration(
                    VariableDeclaration(
                        GenericName(
                            Identifier("Mock"))
                        .WithTypeArgumentList(
                            TypeArgumentList(
                                SingletonSeparatedList<TypeSyntax>(
                                    IdentifierName(typeName)))))
                    .WithVariables(
                        SingletonSeparatedList<VariableDeclaratorSyntax>(
                            VariableDeclarator(
                                Identifier(typeName.ToMemberName()))
                            .WithInitializer(
                                EqualsValueClause(
                                    ObjectCreationExpression(
                                        GenericName(
                                            Identifier("Mock"))
                                        .WithTypeArgumentList(
                                            TypeArgumentList(
                                                SingletonSeparatedList<TypeSyntax>(
                                                    IdentifierName(typeName)))))
                                    .WithArgumentList(
                                        ArgumentList()))))))
                .WithModifiers(
                    TokenList(
                        new[]{
                            Token(SyntaxKind.PrivateKeyword),
                            Token(SyntaxKind.ReadOnlyKeyword)}));
        }

        public static ConstructorDeclarationSyntax CreateConstructor(this ClassDeclarationSyntax classDeclaration)
        {
            var className = classDeclaration.Identifier.Text;
            var constructor = classDeclaration.DescendantNodes().OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
            var parameters = constructor.DescendantNodes().OfType<ParameterListSyntax>().FirstOrDefault().Parameters;
            return
            ConstructorDeclaration(
                Identifier(className.ToTestClass()))
            .WithModifiers(
                TokenList(
                    Token(SyntaxKind.PublicKeyword)))
            .WithBody(
                Block(
                    SingletonList<StatementSyntax>(
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(className.ToMemberName()),
                                ObjectCreationExpression(
                                    IdentifierName(className))
                                .WithArgumentList(
                                    ArgumentList(SeparatedList<ArgumentSyntax>(
                                        GenerateMocksArgumentsList(parameters)
                                        ))))))));
        }

        public static MemberDeclarationSyntax CreateMemberOfClass(this ClassDeclarationSyntax classType)
        {
            var className = classType.Identifier.Text;
            return
                FieldDeclaration(
                    VariableDeclaration(
                        IdentifierName(className))
                    .WithVariables(
                        SingletonSeparatedList<VariableDeclaratorSyntax>(
                            VariableDeclarator(
                                Identifier(className.ToMemberName())))))
                .WithModifiers(
                    TokenList(
                        new[]{
                            Token(SyntaxKind.PrivateKeyword),
                            Token(SyntaxKind.ReadOnlyKeyword)}));
        }

        private static SyntaxNodeOrToken[] GenerateMocksArgumentsList(this IEnumerable<ParameterSyntax> parameterSyntaxes)
        {
            var list = new List<SyntaxNodeOrToken>();

            foreach (var parameterSyntax in parameterSyntaxes)
            {
                list.Add(parameterSyntax.CreateMemberAccessToMock());
                list.Add(Token(SyntaxKind.CommaToken));
            }
            list.RemoveAt(list.Count - 1);

            return list.ToArray();
        }

        public static ArgumentSyntax CreateMemberAccessToMock(this ParameterSyntax parameterSyntax)
        {
            var parameterName = (parameterSyntax.Type as IdentifierNameSyntax).Identifier.Text.ToMemberName();
            return
                Argument(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(parameterName),
                        IdentifierName("Object")));
        }

        public static ClassDeclarationSyntax CreateTestClass(this ClassDeclarationSyntax classSyntax)
        {
            var memberList = new List<MemberDeclarationSyntax>();

            var constructor = classSyntax.DescendantNodes().OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
            var parameters = constructor.DescendantNodes().OfType<ParameterListSyntax>().FirstOrDefault().Parameters;

            memberList.AddRange(parameters.Select(_ => _.Type.CreateMockAsField()));
            memberList.Add(classSyntax.CreateMemberOfClass());
            memberList.Add(classSyntax.CreateConstructor());

            var methods = classSyntax.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(_ => _.Modifiers.Any(SyntaxKind.PublicKeyword));
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
