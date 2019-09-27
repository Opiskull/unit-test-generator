using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using CommandLine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Opiskull.UnitTestGenerator
{
    public class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(opts =>
                {
                    var fileSystem = new FileSystem();
                    var filePath = fileSystem.Path.GetFullPath(opts.FileName);
                    var result = new PathGenerator(fileSystem).FindProjectFileUpwards(opts.FileName);

                    var model = ProjectLoader.LoadAsync(result, filePath).Result;

                    var methodSyntax = model.SyntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().ElementAt(1);
                    var methodSymbol = model.GetDeclaredSymbol(methodSyntax);
                    var compilationUnitRoot = model.SyntaxTree.GetCompilationUnitRoot();
                    var testFileContent = compilationUnitRoot.CreateTestFile().ToString();

                    if (!string.IsNullOrEmpty(opts.OutputFileName))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(opts.OutputFileName));
                        File.WriteAllText(opts.OutputFileName, testFileContent);
                    }
                    else
                    {
                        Console.WriteLine(testFileContent);
                    }
                });
        }
    }
}
