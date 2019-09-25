using System;
using System.IO;
using CommandLine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Opiskull.UnitTestGenerator
{
    public class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(opts =>
                {
                    var fileContent = File.ReadAllText(opts.FileName);
                    var compilationUnitRoot = CSharpSyntaxTree.ParseText(fileContent).GetCompilationUnitRoot();
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
