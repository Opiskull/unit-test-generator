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
                .WithParsed(async (opts) =>
                {
                    var fileSystem = new FileSystem();
                    var pathGenerator = new PathGenerator(fileSystem);

                    var filePath = fileSystem.Path.GetFullPath(opts.FileName);
                    var model = await new ProjectLoader(pathGenerator).LoadSemanticModelAsync(filePath);

                    var testFileContent = new TestFileGenerator(model).CreateTestFile();

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
