using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;


namespace Torque;




public static class CommandLine
{
    public static Process CreateProcess(string command, bool redirectOutput = true, bool redirectInput = true)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "/bin/zsh",
            Arguments = $"-c \"{command}\"",
            UseShellExecute = false,
            RedirectStandardOutput = redirectOutput,
            RedirectStandardError = redirectOutput,
            RedirectStandardInput = redirectInput
        };

        return new Process { StartInfo = processInfo};
    }




    public static Process Execute(string command)
    {
        var process = CreateProcess(command);
        process.Start();

        return process;
    }


    public static void ExecuteAndWait(string command)
        => Execute(command).WaitForExit();




    public static void LLVMBitCodeToFile(string outputFileName, string bitCode, OutputType outputType = OutputType.Object)
    {
        if (outputType == OutputType.BitCode)
        {
            File.WriteAllText(outputFileName, bitCode);
            return;
        }

        var fileType = outputType == OutputType.Object ? "obj" : "asm";

        var process = CreateProcess($"llc -o \"{outputFileName}\" -filetype={fileType}");
        process.Start();
        process.StandardInput.Write(bitCode);
        process.StandardInput.Flush();
        process.StandardInput.Close();
        process.WaitForExit();
    }


    public static void Link(IEnumerable<string> files, string outputFileName)
    {
        var filesString = string.Join(' ', files);

        ExecuteAndWait($"clang {filesString} -o {outputFileName}");
    }
}
