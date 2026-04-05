using Torque.Compiler.CodeGen;


namespace Torque.Compiler.Toolchain;




public record struct CompilerProgramOptions()
{
    public OutputType OutputType { get; set; } = OutputType.Object;

    public bool Debug { get; set; }
    public bool PIC { get; set; }




    public static CompilerProgramOptions FromCompilerOptions(IRGenerationOptions options)
        => new CompilerProgramOptions
        {
            Debug = options.Debug,
            PIC = options.PIC
        };
}

