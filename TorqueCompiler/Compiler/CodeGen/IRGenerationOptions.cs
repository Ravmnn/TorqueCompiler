using System.IO;


namespace Torque.Compiler.CodeGen;




public record struct IRGenerationOptions()
{
    public DirectoryInfo? OutputDirectory { get; set; }

    public bool Debug { get; set; }
    public bool PIC { get; set; }

    // the options below are not accessible through the command line
    public bool CompileImportedModules { get; set; } = true;
}
