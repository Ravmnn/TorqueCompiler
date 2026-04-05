using System.Collections.Generic;
using System.IO;


namespace Torque.Compiler;




public class ModuleLoader() : IModuleProvider
{
    public IEnumerable<DirectoryInfo> ImportPaths { get; set; } = [];

    public Dictionary<string, ModuleInfo> LoadedModules { get; } = [];




    public ModuleInfo LoadModuleById(string id)
    {
        //* Because the binder imports a module, and binder reporter import it again, this will be called twice by module.
        //* This isn't a performance problem because we cache the imported modules

        var relativePath = id.Replace('.', '/');

        foreach (var importPath in ImportPaths)
        {
            var moduleInfo = LoadModuleByRelativePath(relativePath, importPath);

            if (moduleInfo.State == ModuleLoadState.NonExistent)
                continue;

            moduleInfo.Module?.EntryDirectory = importPath;

            return moduleInfo;
        }

        return ModuleInfo.NonExistent;
    }


    private ModuleInfo LoadModuleByRelativePath(string relativePath, DirectoryInfo importPath)
    {
        var modulePath = Path.Combine(importPath.FullName, relativePath) + TorqueFile.SourceExtension;
        var moduleInfo = LoadModuleByPath(modulePath);

        return moduleInfo;
    }


    public ModuleInfo LoadModuleByPath(string file)
    {
        file = Path.GetFullPath(file);

        if (!File.Exists(file))
            return ModuleInfo.NonExistent;

        if (LoadedModules.TryGetValue(file, out var cachedModule))
            return cachedModule;

        return LoadModuleByPathAndInsertEntryDirectory(file);
    }


    private ModuleInfo LoadModuleByPathAndInsertEntryDirectory(string file)
    {
        StartAndFinishModuleLoading(file);

        var fileInfo = new FileInfo(file);
        var moduleInfo = LoadedModules[file];
        moduleInfo.Module!.EntryDirectory = fileInfo.Directory;

        return moduleInfo;
    }


    private void StartAndFinishModuleLoading(string file)
    {
        StartImportingState(file);
        var module = GetModuleFromFile(file);
        FinishImportingState(file, module);
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
