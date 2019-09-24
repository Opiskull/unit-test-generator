using System;
using System.IO;
using CommandLine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace unit_test_generator
{
    class Program
    {
        public class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }

            [Value(0, Required = true, HelpText = "Input filename.")]
            public string FileName { get; set; }

            [Option("out", Required = false, HelpText = "Output filename.")]
            public string OutputFileName { get; set; }
        }

        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts =>
                {
                    var fileContent = File.ReadAllText(opts.FileName);
                    var compilationUnitRoot = CSharpSyntaxTree.ParseText(fileContent).GetCompilationUnitRoot();
                    var output = compilationUnitRoot.CreateTestFile().NormalizeWhitespace().ToString();

                    if (!string.IsNullOrEmpty(opts.OutputFileName))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(opts.OutputFileName));
                        File.WriteAllText(opts.OutputFileName, output);
                    }
                    else
                    {
                        Console.WriteLine(output);
                    }
                });
        }
    }
}
