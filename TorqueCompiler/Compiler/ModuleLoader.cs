using System.Collections.Generic;
using System.IO;


namespace Torque.Compiler;




public class ModuleLoader(FileSystem fileSystem) : IModuleProvider
{
    public const string TorqueExtension = ".tor";


    public FileSystem FileSystem { get; } = fileSystem;
    public string ImportReference => FileSystem.EntryDirectory.FullName;

    public Dictionary<string, ModuleInfo> LoadedModules { get; } = [];




    public ModuleInfo LoadModuleById(string id)
    {
        var relativePath = id.Replace('.', '/');
        var modulePath = Path.Combine(ImportReference, relativePath) + TorqueExtension;

        return LoadModuleByPath(modulePath);
    }


    public ModuleInfo LoadModuleByPath(string file)
    {
        file = Path.GetFullPath(file);

        if (!File.Exists(file))
            return ModuleInfo.NonExistent;

        if (LoadedModules.TryGetValue(file, out var moduleInfo))
            return moduleInfo;

        StartImportingState(file);
        var module = GetModuleFromFile(file);
        FinishImportingState(file, module);

        return LoadedModules[file];
    }

    private void StartImportingState(string file)
        => LoadedModules.Add(file, new ModuleInfo(null, ModuleLoadState.Loading));

    private void FinishImportingState(string file, Module module)
        => LoadedModules[file] = new ModuleInfo(module, ModuleLoadState.Loaded);




    public Module GetModuleFromFile(string file)
    {
        var fileInfo = new FileInfo(file);

        var sourceCode = new SourceCode(fileInfo);
        var statements = CompilerSteps.BuildFinalAST(sourceCode);
        var module = CompilerSteps.SemanticAnalysis(statements, sourceCode, this);

        return module;
    }
}
