using System.IO;


namespace Torque.Compiler;




public class FileSystem(FileInfo entryFile)
{
    public FileInfo EntryFile { get; } = entryFile;
    public DirectoryInfo EntryDirectory => EntryFile.Directory!;
}
