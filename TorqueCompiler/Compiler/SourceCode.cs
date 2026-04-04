using System.IO;


namespace Torque.Compiler;




public class SourceCode(FileInfo file)
{
    public string Source { get; } = System.IO.File.ReadAllText(file.FullName);
    public string[] SourceLines => Source.Split('\n');

    public FileInfo File { get; } = file;
    public string FilePath => File.FullName;
}
