using System.Collections.Generic;
using System.IO;

using Torque.Compiler;


namespace Torque.CommandLine;




public enum ModuleImportState
{
    Importing,
    Imported
}


public static class ModuleImporter
{
    public static Dictionary<string, (Module? module, ModuleImportState state)> ImportedModules { get; } = [];




    public static (Module? module, ModuleImportState state) GetModule(string file)
    {
        file = Path.GetFullPath(file);

        if (ImportedModules.TryGetValue(file, out var moduleInfo))
            return moduleInfo;

        StartImportingState(file);
        var module = GetModuleFromFile(file);
        FinishImportingState(file, module);

        return ImportedModules[file];
    }

    private static void StartImportingState(string file)
        => ImportedModules.Add(file, (null, ModuleImportState.Importing));

    private static void FinishImportingState(string file, Module module)
        => ImportedModules[file] = (module, ModuleImportState.Imported);




    private static Module GetModuleFromFile(string file)
    {
        var oldFile = SourceCode.FilePath;

        SourceCode.SetCurrentWorkingFileTo(file);

        var source = File.ReadAllText(file);
        var statements = CompilerSteps.BuildFinalAST(source);
        var module = CompilerSteps.SemanticAnalysis(statements, file);

        if (oldFile is not null)
            SourceCode.SetCurrentWorkingFileTo(oldFile);

        return module;
    }




    public static string GetCurrentImportReference()
        => new FileInfo(SourceCode.FirstFilePath!).DirectoryName!;
}
