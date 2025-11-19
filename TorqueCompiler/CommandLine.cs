using System.Diagnostics;


namespace Torque;




public static class CommandLine
{
    public static void Execute(string command)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "/bin/zsh",
            Arguments = $"-c \"{command}\"",
            UseShellExecute = false
        };

        var process = new Process { StartInfo = processInfo};
        process.Start();
        process.WaitForExit();
    }
}
