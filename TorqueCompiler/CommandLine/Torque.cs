// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable LocalizableElement


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Torque.Compiler;
using Torque.Compiler.Target;
using Torque.Compiler.AST.Statements;
using Torque.CommandLine.Exceptions;
using Torque.CommandLine.Toolchain;


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
        InitializeNullableCompileSettings(settings);
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


    private static void InitializeNullableCompileSettings(CompileCommandSettings settings)
    {
        settings.ImportReference ??= GetImportReference();
    }


    private static string GetImportReference()
        => s_compileSettings.ImportReference ?? s_compileSettings.File.Directory!.FullName;




    public static void Compile(CompileCommandSettings settings)
    {
        try
        {
            CompileFileToObject(settings);
        }
        catch (InterruptCompileException)
        { }
        catch (Exception exception)
        {
            DiagnosticLogger.LogInternalError(exception);
        }
    }


    public static void CompileFileToObject(string file, CompilerOptions options)
    {
        // TODO: add command line options to modify the output folder

        var (_, bitCode) = CompileModule(file, options);

        ProgramToolchain.Compile(bitCode, file + ".o", options);
    }


    public static void CompileFileToObject(CompileCommandSettings settings)
        => CompileFileToObject(settings.File.FullName, settings.ToLowLevelOptions());


    public static void CompileModuleToObject(Module module, CompilerOptions options)
    {
        var bitCode = CompilerSteps.Compile(module, options);
        ProgramToolchain.Compile(bitCode, module.FileInfo.FullName + ".o", options);
    }


/*     private static void PrintRequestedModuleFormats(IReadOnlyList<Statement> statements, string bitCode)
    {
        PrintASTIfRequested(statements);
        PrintLLVMIfRequested(bitCode);
        PrintASMIfRequested(bitCode);
    } */




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
        var statements = BuildFinalAST(source);
        var module = SemanticAnalysis(statements, file);

        if (oldFile is not null)
            InitializeGlobalSourceCodeReference(oldFile);

        return module;
    }



    private static Module SemanticAnalysis(IReadOnlyList<Statement> statements, string modulePath)
    {
        var moduleContext = CompilerSteps.Bind(statements, s_compileSettings.ImportReference!, modulePath);

        CompilerSteps.TypeCheck(moduleContext);
        CompilerSteps.AnalyzeControlFlow(moduleContext.Statements);

        return moduleContext;
    }




    private static IReadOnlyList<Statement> BuildFinalAST(string source)
    {
        var tokens = CompilerSteps.Tokenize(source);
        var statements = CompilerSteps.Parse(tokens);
        statements = CompilerSteps.Desugarize(statements);

        return statements;
    }




/*     private static void PrintASTIfRequested(IReadOnlyList<Statement> statements)
    {
        if (!s_compileSettings.PrintAST)
            return;

        Console.WriteLine(new ASTPrinter().Print(statements));
        Interrupt();
    }


    private static void PrintLLVMIfRequested(string bitCode)
    {
        if (!s_compileSettings.PrintLLVM)
            return;

        Console.WriteLine(bitCode);
        Interrupt();
    }


    private static void PrintASMIfRequested(string bitCode)
    {
        if (!s_compileSettings.PrintASM)
            return;

        var assembly = CompileBitCodeToAssembly(bitCode);
        Console.WriteLine(assembly);
        Interrupt();
    }


    private static string CompileBitCodeToAssembly(string bitCode) => TempFiles.ForTempFileDo(file =>
    {
        var options = CompilerProgramOptions.FromCompileCommandSettings(s_compileSettings)
            with { OutputType = OutputType.Assembly };

        ProgramToolchain.Compile(bitCode, file, options);
        return File.ReadAllText(file);
    }); */




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
