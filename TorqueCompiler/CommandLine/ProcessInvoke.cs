using System.Collections.Generic;
using System.Diagnostics;
using System.IO;


namespace Torque.CommandLine;




public static class ProcessInvoke
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
}
