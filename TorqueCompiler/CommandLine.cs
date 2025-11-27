using System.Collections.Generic;
using System.Diagnostics;
using System.IO;


namespace Torque;




public static class CommandLine
{
    public static Process CreateProcess(string path, string arguments, bool redirectOutput = true, bool redirectInput = true)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = path,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = redirectOutput,
            RedirectStandardError = redirectOutput,
            RedirectStandardInput = redirectInput,
            CreateNoWindow = true
        };

        return new Process { StartInfo = processInfo };
    }


    public static void ExecuteAndWait(string path, string arguments, bool redirectOutput = false, bool redirectInput = false)
    {
        var process = CreateProcess(path, arguments, redirectOutput, redirectInput);
        process.Start();
        process.WaitForExit();
    }


    public static void LLVMBitCodeToFile(string outputFileName, string bitCode, OutputType outputType = OutputType.Object)
    {
        if (outputType == OutputType.BitCode)
        {
            File.WriteAllText(outputFileName, bitCode);
            return;
        }

        var fileType = outputType == OutputType.Object ? "obj" : "asm";

        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, bitCode);

        ExecuteAndWait("/bin/llc", $"{tempFile} -o \"{outputFileName}\" -filetype={fileType}");

        File.Delete(tempFile);
    }


    public static void Link(IEnumerable<string> files, string outputFileName, bool debug = false)
    {
        var filesString = string.Join(' ', files);
        var debugString = debug ? "-g" : string.Empty;

        ExecuteAndWait("/bin/clang", $"{filesString} -o \"{outputFileName}\" {debugString}");
    }
}
