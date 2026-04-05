using Torque.CommandLine.Commands;
using Torque.Compiler.Toolchain;


namespace Torque.CommandLine;




public static class CompilerProgramOptionsExtensions
{
    public static CompilerProgramOptions FromCompileCommandSettings(this CompileCommandSettings settings)
        => new CompilerProgramOptions
        {
            Debug = settings.Debug,
            PIC = settings.PIC
        };
}
