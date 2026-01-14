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

        var fileType = outputType == OutputType.Object ? "obj" : "asm";
        var debugString = debug ? "-O0" : string.Empty;

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, bitCode);

        // uses LLVM 20.0
        ProcessInvoke.ExecuteAndWait("/bin/llc-20", $"{tempFile} -o \"{outputFileName}\" -filetype={fileType} {debugString}");

        File.Delete(tempFile);
    }


    public static void Link(IReadOnlyList<string> files, string outputFileName, bool debug = false)
    {
        var filesString = string.Join(' ', files);
        var debugString = debug ? "-O0 -g" : string.Empty;

        ProcessInvoke.ExecuteAndWait("/bin/clang", $"{filesString} -o \"{outputFileName}\" {debugString}");
    }
}
