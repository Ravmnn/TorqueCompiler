using System.Diagnostics;
using Torque.Compiler;


namespace Torque.CommandLine.Toolchain;




public class CompilerProgram : Program
{
    public override string ProgramPath => "/bin/llc-20";

    public required string InputFile { get; set; }
    public required string OutputFile { get; set; }
    public CompilerOptions Options { get; set; }




    public override string GetCommandLineArguments()
    {
        var fileType = "obj";
        var debug = Options.Debug ? "-O0" : string.Empty;
        var pic = $"-relocation-model={(Options.PIC ? "pic" : "static")}";

        return $"{InputFile} -o \"{OutputFile}\" -filetype={fileType} {debug} {pic}";
    }
}
