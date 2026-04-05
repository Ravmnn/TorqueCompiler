using System.IO;


namespace Torque.Compiler;




public class EntryInfo(FileInfo entryFile)
{
    public FileInfo EntryFile { get; } = entryFile;
    public DirectoryInfo EntryDirectory => EntryFile.Directory!;
}
