using System.Diagnostics;


namespace Torque.CommandLine.Toolchain;




public class CompilerProgram : Program
{
    public override string ProgramPath => "/bin/llc-20";

    public required string InputFile { get; set; }
    public required string OutputFile { get; set; }
    public CompilerProgramOptions Options { get; set; }




    public override string GetCommandLineArguments()
    {
        var fileType = OutputTypeToLLVMOutputType();
        var debug = Options.Debug ? "-O0" : string.Empty;

        // TODO: PIC enabled by default, move this to CLI option later
        return $"{InputFile} -o \"{OutputFile}\" -filetype={fileType} {debug} -relocation-model=pic";
    }


    private string OutputTypeToLLVMOutputType() => Options.OutputType switch
    {
        OutputType.Object => "obj",
        OutputType.Assembly => "asm",

        // `OutputType.BitCode` should be processed by the caller

        _ => throw new UnreachableException()
    };
}
