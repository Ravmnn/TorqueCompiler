using System.Collections.Generic;
using System.IO;

using Torque.Compiler;


namespace Torque.CommandLine;




public enum ModuleImportState
{
    Loading,
    Loaded
}


public static class ModuleLoader
{
    public static string ImportReference => SourceCode.EntryDirectory!.FullName;


    public static Dictionary<string, (Module? module, ModuleImportState state)> LoadedModules { get; } = [];




    public static (Module? module, ModuleImportState state) LoadModule(string file)
    {
        file = Path.GetFullPath(file);

        if (LoadedModules.TryGetValue(file, out var moduleInfo))
            return moduleInfo;

        StartImportingState(file);
        var module = GetModuleFromFile(file);
        FinishImportingState(file, module);

        return LoadedModules[file];
    }

    private static void StartImportingState(string file)
        => LoadedModules.Add(file, (null, ModuleImportState.Loading));

    private static void FinishImportingState(string file, Module module)
        => LoadedModules[file] = (module, ModuleImportState.Loaded);




    private static Module GetModuleFromFile(string file)
    {
        var oldFile = SourceCode.File;
        var newFile = new FileInfo(file);

        SourceCode.SetCurrentWorkingFileTo(newFile);

        var statements = CompilerSteps.BuildFinalAST(SourceCode.Source!);
        var module = CompilerSteps.SemanticAnalysis(statements, SourceCode.FilePath!);

        if (oldFile is not null)
            SourceCode.SetCurrentWorkingFileTo(oldFile);

        return module;
    }
}
