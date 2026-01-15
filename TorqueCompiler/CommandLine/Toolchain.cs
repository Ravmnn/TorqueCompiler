using System.IO;
using System.Collections.Generic;


namespace Torque.CommandLine;




public static class Toolchain
{
    public static void Compile(string outputFileName, string bitCode, OutputType outputType = OutputType.Object, bool debug = false)
    {
        if (outputType == OutputType.BitCode)
        {
            File.WriteAllText(outputFileName, bitCode);
            return;
        }

        CompileUsingTempFile(outputFileName, bitCode, outputType, debug);
    }


    private static void CompileUsingTempFile(string outputFileName, string bitCode, OutputType outputType, bool debug)
    {
        var fileType = outputType == OutputType.Object ? "obj" : "asm";
        var debugString = debug ? "-O0" : string.Empty;

        TempFiles.ForTempFileDo(file =>
        {
            File.WriteAllText(file, bitCode);

            // uses LLVM 20.0
            ProcessInvoke.ExecuteAndWait("/bin/llc-20", // TODO: PIC enabled by default, move this to CLI option later
                $"{file} -o \"{outputFileName}\" -filetype={fileType} {debugString} -relocation-model=pic");
        });
    }




    public static void Link(IReadOnlyList<string> files, string outputFileName, bool debug = false)
    {
        var filesString = string.Join(' ', files);
        var debugString = debug ? "-O0 -g" : string.Empty;

        // PIC (Position Independent Code) -> generic
        // PIE (Position Independent Executable) -> only to executables, not libraries

        // if you want PIC for executables, PIE is better (more optimized)
        // if you want PIC for a library, raw PIC is mandatory, since a library is not executable,
        // so PIE doesn't work

        ProcessInvoke.ExecuteAndWait("/bin/clang", // TODO: again, move PIC to CLI option.
            $"{filesString} -o \"{outputFileName}\" {debugString} -fPIC");
    }
}
