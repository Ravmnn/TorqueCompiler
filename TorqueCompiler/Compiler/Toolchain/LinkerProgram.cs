using System.Collections.Generic;


namespace Torque.Compiler.Toolchain;




public class LinkerProgram : Program
{
    // we use clang because it's convenient
    public override string ProgramPath => "/bin/clang";

    public required IReadOnlyCollection<string> InputFiles { get; set; }
    public required string OutputFile { get; set; }
    public LinkerProgramOptions Options { get; set; }




    public override string GetCommandLineArguments()
    {
        // PIC (Position Independent Code) -> generic
        // PIE (Position Independent Executable) -> only to executables, not libraries

        // if you want PIC for executables, PIE is better (more optimized)
        // if you want PIC for a library, raw PIC is mandatory, since a library is not executable,
        // so PIE doesn't work

        var files = string.Join(' ', InputFiles);
        var debug = Options.Debug ? "-O0 -g" : string.Empty;
        var pie = Options.PIE ? "-pie" : "-no-pie";

        return $"{files} -o \"{OutputFile}\" {debug} {pie}";
    }
}
