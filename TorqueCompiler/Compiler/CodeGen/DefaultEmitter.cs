using System;
using System.IO;

using Torque.Compiler.Toolchain;


namespace Torque.Compiler.CodeGen;




public class DefaultEmitter() : IIREmitter
{
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

        var outputFile = GetOutputFileForModule(module, options.OutputDirectory);
        var bitCode = module.LLVMModule!.Value.PrintToString();

        ProgramToolchain.Compile(bitCode, outputFile, options);
    }


    private static string GetOutputFileForModule(Module module, DirectoryInfo? outputDirectory)
    {
        var file = module.SourceCode.FilePath;
        var root = module.EntryDirectory!.FullName;
        var relativePath = Path.GetRelativePath(root, file);

        return Path.Combine(outputDirectory?.FullName ?? root, relativePath + TorqueFile.ObjectExtension);
    }


    private static void ThrowIfNoIR(Module module)
    {
        if (module.LLVMModule is null)
            throw new InvalidOperationException("Module must already have the IR");
    }
}
