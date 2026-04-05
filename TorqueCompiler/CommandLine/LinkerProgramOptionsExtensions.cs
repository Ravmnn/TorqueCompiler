using Torque.CommandLine.Commands;
using Torque.Compiler.Toolchain;


namespace Torque.CommandLine;




public static class LinkerProgramOptionsExtensions
{
    public static LinkerProgramOptions LinkerOptionsFromLinkCommandSettings(this LinkCommandSettings settings)
        => new LinkerProgramOptions
        {
            Debug = settings.Debug,
            PIE = settings.PIE
        };
}
