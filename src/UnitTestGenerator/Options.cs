using CommandLine;

namespace Opiskull.UnitTestGenerator
{
    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('s', "skip", Required = false, HelpText = "Skip output of filename.")]
        public bool Skip { get; set; }

        [Value(0, Required = true, HelpText = "Input filename.")]
        public string InputFilePath { get; set; }

        [Value(1, Required = false, HelpText = "Output filename.")]
        public string OutputFilePath { get; set; }
    }
}
