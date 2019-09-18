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
                    var classParser = new ClassParser();
                    var fileContent = File.ReadAllText(opts.FileName);

                    var testClass = classParser.Parse(fileContent);
                    var output = TestFileGenerator.GenerateTestsFromTestFile(testClass).NormalizeWhitespace().ToString();

                    if (!string.IsNullOrEmpty(opts.OutputFileName))
                    {
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
