namespace Torque.CommandLine.Toolchain;




public record struct CompilerProgramOptions()
{
    public OutputType OutputType { get; set; }
    public bool Debug { get; set; }
    public bool PIC { get; set; }




    public static CompilerProgramOptions FromCompileCommandSettings(CompileCommandSettings settings)
        => new CompilerProgramOptions
        {
            OutputType = settings.OutputType,
            Debug = settings.Debug,
            PIC = settings.PIC
        };
}
