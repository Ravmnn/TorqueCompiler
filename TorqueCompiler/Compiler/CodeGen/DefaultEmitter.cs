using System;
using System.IO;

using Torque.Compiler.Toolchain;


namespace Torque.Compiler.CodeGen;




public class DefaultEmitter(EntryInfo entry) : IIREmitter
{
    public EntryInfo Entry { get; } = entry;




    public void EmitModuleAndImports(Module module, IRGenerationOptions options)
    {
        ThrowIfNoIR(module);

        foreach (var importedModule in module.ImportedModules)
            EmitModuleAndImports(importedModule, options);

        EmitModule(module, options);
    }


    public void EmitModule(Module module, IRGenerationOptions options)
    {
        ThrowIfNoIR(module);

        var outputFile = GetOutputFile(module.SourceCode.FilePath, options.OutputDirectory);
        var bitCode = module.LLVMModule!.Value.PrintToString();

        ProgramToolchain.Compile(bitCode, outputFile, options);
    }


    private string GetOutputFile(string file, DirectoryInfo? outputDirectory)
    {
        var root = Entry.EntryDirectory.Parent!.FullName;
        var relativePath = Path.GetRelativePath(root, file);

        return Path.Combine(outputDirectory?.FullName ?? root, relativePath + ".o");
    }


    private static void ThrowIfNoIR(Module module)
    {
        if (module.LLVMModule is null)
            throw new InvalidOperationException("Module must already have the IR");
    }
}
