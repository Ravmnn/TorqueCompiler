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
            ProcessInvoke.ExecuteAndWait("/bin/llc-20", $"{file} -o \"{outputFileName}\" -filetype={fileType} {debugString}");
        });
    }




    public static void Link(IReadOnlyList<string> files, string outputFileName, bool debug = false)
    {
        var filesString = string.Join(' ', files);
        var debugString = debug ? "-O0 -g" : string.Empty;

        ProcessInvoke.ExecuteAndWait("/bin/clang", $"{filesString} -o \"{outputFileName}\" {debugString}");
    }
}
