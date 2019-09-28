using CommandLine;

namespace Opiskull.UnitTestGenerator
{
    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('s', "skip", Required = false, HelpText = "Skip creation of output file.")]
        public bool Skip { get; set; }

        [Option('o', "overwrite", Required = false, HelpText = "Overwrite output file.")]
        public bool Overwrite { get; set; }

        [Value(0, Required = true, HelpText = "Input filename.")]
        public string InputFilePath { get; set; }

        [Value(1, Required = false, HelpText = "Output filename.")]
        public string OutputFilePath { get; set; }
    }
}
