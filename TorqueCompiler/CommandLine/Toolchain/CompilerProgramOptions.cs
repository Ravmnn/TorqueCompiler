using Torque.CommandLine.Commands;
using Torque.Compiler;


namespace Torque.CommandLine.Toolchain;




public record struct CompilerProgramOptions()
{
    public OutputType OutputType { get; set; } = OutputType.Object;

    public bool Debug { get; set; }
    public bool PIC { get; set; }




    public static CompilerProgramOptions FromCompileCommandSettings(CompileCommandSettings settings)
        => new CompilerProgramOptions
        {
            Debug = settings.Debug,
            PIC = settings.PIC
        };


    public static CompilerProgramOptions FromCompilerOptions(CompilerOptions options)
        => new CompilerProgramOptions
        {
            Debug = options.Debug,
            PIC = options.PIC
        };
}

