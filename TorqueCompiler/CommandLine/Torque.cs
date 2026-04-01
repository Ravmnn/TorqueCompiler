// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable LocalizableElement


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Torque.Compiler;
using Torque.Compiler.Target;
using Torque.CommandLine.Toolchain;
using Torque.CommandLine.Commands;
using Torque.CommandLine.Exceptions;


namespace Torque.CommandLine;




public enum ModuleImportState
{
    Importing,
    Imported
}


public static class Torque
{
    private static CompileCommandSettings s_compileSettings = null!;
    private static LinkCommandSettings s_linkSettings = null!;


    public static Dictionary<string, (Module? module, ModuleImportState state)> ImportedModules { get; } = [];


    public static DiagnosticLogger Logger { get; set; } = new DiagnosticLogger();


    public const string FileExtension = ".tor";




    public static void Initialize(CompileCommandSettings settings)
    {
        s_compileSettings = settings;
        InitializeGlobals(settings);
    }


    private static void InitializeGlobals(CompileCommandSettings settings)
    {
        InitializeGlobalSourceCodeReference(settings.File.FullName);
        InitializeGlobalTargetMachine(settings);
    }


    private static void InitializeGlobalSourceCodeReference(string file)
    {
        SourceCode.Source = File.ReadAllText(file);
        SourceCode.FilePath = file;
        SourceCode.FirstFilePath ??= file;
    }


    private static void InitializeGlobalTargetMachine(CompileCommandSettings settings)
    {
        var targetTriple = TargetTriple.FromCompileSettings(settings);
        TargetMachine.SetGlobalTarget(targetTriple.ToString());
    }




    public static void Compile(CompileCommandSettings settings)
    {
        try
        {
            CompileFileToObject(settings);
        }
        catch (InterruptCompileException) {}
        catch (Exception exception)
        {
            DiagnosticLogger.LogInternalError(exception);
        }
    }


    // TODO: maybe split importing module logic from here
    public static void CompileFileToObject(CompileCommandSettings settings)
        => CompileFileToObject(settings.File.FullName, settings.ToLowLevelOptions());


    public static void CompileFileToObject(string file, CompilerOptions options)
    {
        // TODO: add command line options to modify the output folder

        var (_, bitCode) = CompileModule(file, options);
        ProgramToolchain.Compile(bitCode, file + ".o", options);
    }


    public static void CompileModuleToObject(Module module, CompilerOptions options)
    {
        var bitCode = CompilerSteps.Compile(module, options);
        ProgramToolchain.Compile(bitCode, module.FileInfo.FullName + ".o", options);
    }







    public static (Module module, string bitCode) CompileModule(string file, CompilerOptions options)
    {
        var (module, _) = GetModule(file);
        var bitCode = CompilerSteps.Compile(module!.Value, options);

        return (module.Value, bitCode);
    }




    public static (Module? module, ModuleImportState state) GetModule(string file)
    {
        file = Path.GetFullPath(file);

        if (ImportedModules.TryGetValue(file, out var moduleInfo))
            return moduleInfo;

        ImportedModules.Add(file, (null, ModuleImportState.Importing));
        var module = GetModuleFromFile(file);
        ImportedModules[file] = (module, ModuleImportState.Imported);

        return ImportedModules[file];
    }


    private static Module GetModuleFromFile(string file)
    {
        var oldFile = SourceCode.FilePath;

        InitializeGlobalSourceCodeReference(file);

        var source = File.ReadAllText(file);
        var statements = CompilerSteps.BuildFinalAST(source);
        var module = CompilerSteps.SemanticAnalysis(statements, file);

        if (oldFile is not null)
            InitializeGlobalSourceCodeReference(oldFile);

        return module;
    }




    public static void Link(LinkCommandSettings settings)
    {
        s_linkSettings = settings;

        var fileNames = settings.Files.Select(file => file.FullName).ToArray();
        var options = LinkerProgramOptions.FromLinkCommandSettings(settings);

        ProgramToolchain.Link(fileNames, settings.Output, options);
    }




    public static string GetCurrentImportReference()
        => new FileInfo(SourceCode.FirstFilePath!).DirectoryName!;
}
