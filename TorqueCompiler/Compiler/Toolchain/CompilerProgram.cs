namespace Torque.Compiler.Toolchain;




public class CompilerProgram : Program
{
    public override string ProgramPath => "/bin/llc-20";

    public required string InputFile { get; set; }
    public required string OutputFile { get; set; }
    public CompilerProgramOptions Options { get; set; }




    public override string GetCommandLineArguments()
    {
        var fileType = Options.OutputType == OutputType.Object ? "obj" : "asm";
        var debug = Options.Debug ? "-O0" : string.Empty;
        var pic = $"-relocation-model={(Options.PIC ? "pic" : "static")}";

        return $"{InputFile} -o \"{OutputFile}\" -filetype={fileType} {debug} {pic}";
    }
}
