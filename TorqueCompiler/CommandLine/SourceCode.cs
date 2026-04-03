using System.IO;


namespace Torque.CommandLine;




public static class SourceCode
{
    public static string? Source { get; set; }
    public static string[]? SourceLines => Source?.Split('\n');

    public static string? FilePath { get; set; }

    public static string? FirstFilePath { get; set; }




    public static void SetCurrentWorkingFileTo(string file)
    {
        Source = File.ReadAllText(file);
        FilePath = file;
        FirstFilePath ??= file;
    }
}
