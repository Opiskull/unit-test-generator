using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace unit_test_generator
{
    public class TestFileGenerator
    {
        public static SyntaxNode GenerateTestsFromTestFile(TestFile testFile)
        {
            return GenerateTestsFromTestFile(testFile.Namespace, testFile.ClassName, testFile.Dependencies, testFile.AsyncMethods, testFile.NonAsyncMethods);
        }

        public static SyntaxNode GenerateTestsFromTestFile(string namespaceName, string className, string[] interfacesOrClasses, string[] asyncMethods, string[] nonAsyncMethods)
        {
            return CompilationUnit()
                .WithUsings(
                    List<UsingDirectiveSyntax>(
                        new UsingDirectiveSyntax[]{
                    UsingDirective(
                        IdentifierName("Xunit")),
                    UsingDirective(
                        IdentifierName("Moq"))}))
                .WithMembers(
                    SingletonList<MemberDeclarationSyntax>(
                        NamespaceDeclaration(
                            QualifiedName(namespaceName + ".Test"))
                            .WithMembers(
                                SingletonList<MemberDeclarationSyntax>(GenerateTestClass(className, interfacesOrClasses, asyncMethods, nonAsyncMethods)))));
        }

        public static NameSyntax QualifiedName(string dotedName)
        {
            return GetQualifiedName(new List<string>(dotedName.Split(".").Reverse()));
        }

        private static NameSyntax GetQualifiedName(List<string> items)
        {
            if (items.Count == 1)
            {
                return IdentifierName(items.ElementAt(0));
            }
            var item = items[0];
            items.RemoveAt(0);
            return SyntaxFactory.QualifiedName(GetQualifiedName(items), IdentifierName(item));
        }

        private static ClassDeclarationSyntax GenerateTestClass(string className, string[] interfacesOrClasses, string[] asyncMethods, string[] nonAsyncMethods)
        {
            var memberList = new List<MemberDeclarationSyntax>();
            memberList.AddRange(interfacesOrClasses.Select(key => GenerateFieldMemberMock(key, key.ToMemberName())));
            memberList.Add(GenerateFieldMemberClass(className, className.ToMemberName()));
            memberList.Add(GenerateConstructorWithMockParameters(className + "Test", className.ToMemberName(), interfacesOrClasses.Select(_ => _.ToMemberName()).ToArray()));
            memberList.AddRange(asyncMethods.Select(_ => GenerateAsyncTestMethod(_)));
            memberList.AddRange(nonAsyncMethods.Select(_ => GenerateVoidTestMethod(_)));

            return ClassDeclaration(className + "Test")
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)))
                .WithMembers(List(memberList));
        }

        private static MemberDeclarationSyntax GenerateVoidTestMethod(string methodName)
        {
            return MethodDeclaration(
                    PredefinedType(
                        Token(SyntaxKind.VoidKeyword)),
                    Identifier(methodName + "Test"))
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
                    Block());
        }

        private static MemberDeclarationSyntax GenerateAsyncTestMethod(string methodName)
        {
            return MethodDeclaration(
                            IdentifierName("Task"),
                            Identifier(methodName + "Test"))
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
                                SingletonList<StatementSyntax>(
                                    ExpressionStatement(
                                        AwaitExpression(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("Task"),
                                                IdentifierName("Completed")))))));
        }

        private static SyntaxNodeOrToken[] GenerateMocksArgumentsList(string[] arguments)
        {
            var list = new List<SyntaxNodeOrToken>();

            foreach (var argument in arguments)
            {
                list.Add(Argument(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(argument),
                                IdentifierName("Object"))));
                list.Add(Token(SyntaxKind.CommaToken));
            }
            list.RemoveAt(list.Count - 1);

            return list.ToArray();
        }

        private static MemberDeclarationSyntax GenerateFieldMemberClass(string className, string fieldName)
        {
            return FieldDeclaration(
                                VariableDeclaration(
                                    IdentifierName(className))
                                .WithVariables(
                                    SingletonSeparatedList<VariableDeclaratorSyntax>(
                                        VariableDeclarator(
                                            Identifier(fieldName)))))
                            .WithModifiers(
                                TokenList(
                                    new[]{
                                        Token(SyntaxKind.PrivateKeyword),
                                        Token(SyntaxKind.ReadOnlyKeyword)}));
        }

        private static MemberDeclarationSyntax GenerateConstructorWithMockParameters(string className, string fieldName, string[] parameters)
        {
            return ConstructorDeclaration(
                                Identifier(className))
                            .WithModifiers(
                                TokenList(
                                    Token(SyntaxKind.PublicKeyword)))
                            .WithBody(
                                Block(
                                    SingletonList<StatementSyntax>(
                                        ExpressionStatement(
                                            AssignmentExpression(
                                                SyntaxKind.SimpleAssignmentExpression,
                                                IdentifierName(fieldName),
                                                ObjectCreationExpression(
                                                    IdentifierName(className))
                                                .WithArgumentList(
                                                    ArgumentList(SeparatedList<ArgumentSyntax>(
                                                        GenerateMocksArgumentsList(parameters)
                                                        ))))))));
        }

        private static MemberDeclarationSyntax GenerateFieldMemberMock(string interfaceName, string fieldName)
        {
            return FieldDeclaration(
                    VariableDeclaration(
                        GenericName(
                            Identifier("Mock"))
                        .WithTypeArgumentList(
                            TypeArgumentList(
                                SingletonSeparatedList<TypeSyntax>(
                                    IdentifierName(interfaceName)))))
                    .WithVariables(
                        SingletonSeparatedList<VariableDeclaratorSyntax>(
                            VariableDeclarator(
                                Identifier(fieldName))
                            .WithInitializer(
                                EqualsValueClause(
                                    ObjectCreationExpression(
                                        GenericName(
                                            Identifier("Mock"))
                                        .WithTypeArgumentList(
                                            TypeArgumentList(
                                                SingletonSeparatedList<TypeSyntax>(
                                                    IdentifierName(interfaceName)))))
                                    .WithArgumentList(
                                        ArgumentList()))))))
                .WithModifiers(
                    TokenList(
                        new[]{
                            Token(SyntaxKind.PrivateKeyword),
                            Token(SyntaxKind.ReadOnlyKeyword)}));
        }
    }
}
