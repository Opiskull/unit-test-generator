using CommandLine;

namespace Opiskull.UnitTestGenerator
{
    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Value(0, Required = true, HelpText = "Input filename.")]
        public string FileName { get; set; }

        [Value(1, Required = false, HelpText = "Output filename.")]
        public string OutputFileName { get; set; }
    }
}
