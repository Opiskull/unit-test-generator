using System;
using System.IO;
using System.IO.Abstractions;
using CommandLine;

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

                    var filePath = fileSystem.Path.GetFullPath(opts.InputFilePath);

                    Console.WriteLine($"Input: {filePath}");

                    var model = await new ProjectLoader(pathGenerator).LoadSemanticModelAsync(filePath);

                    var testFileContent = model.CreateTestFileContent();

                    var outputFilePath = pathGenerator.CreateTestFilePath(filePath);

                    if (!string.IsNullOrEmpty(opts.OutputFilePath))
                    {
                        outputFilePath = fileSystem.Path.GetFullPath(opts.OutputFilePath);
                        Console.WriteLine($"Overwriting Output Path: {outputFilePath}");
                    }

                    Console.WriteLine("--------------------------------------------------");
                    Console.WriteLine(testFileContent);
                    Console.WriteLine("--------------------------------------------------");

                    if (!opts.Skip)
                    {
                        Console.WriteLine($"Output: {outputFilePath}");
                        fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(outputFilePath));
                        fileSystem.File.WriteAllText(outputFilePath, testFileContent);
                    }
                });
        }
    }
}
