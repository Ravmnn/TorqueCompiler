using System.IO;


namespace Torque.CommandLine;




public static class SourceCode
{
    public static string? Source { get; private set; }
    public static string[]? SourceLines => Source?.Split('\n');

    public static FileInfo? File { get; private set; }
    public static string? FilePath => File?.FullName;

    public static FileInfo? EntryFile { get; private set; }
    public static DirectoryInfo? EntryDirectory => EntryFile?.Directory;




    public static void SetCurrentWorkingFileTo(FileInfo file)
    {
        var path = file.FullName;

        Source = System.IO.File.ReadAllText(path);
        File = file;
        EntryFile ??= file;
    }
}
