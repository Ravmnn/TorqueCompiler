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




public static class Torque
{
    private static CompileCommandSettings s_compileSettings = null!;
    private static LinkCommandSettings s_linkSettings = null!;

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
    }


    private static void InitializeGlobalTargetMachine(CompileCommandSettings settings)
    {
        var targetTriple = TargetTriple.FromCompileSettings(settings);
        TargetMachine.SetGlobalTarget(targetTriple.ToString());
    }


    private static void InitializeNullableCompileSettings(CompileCommandSettings settings)
    {
        settings.ImportReference ??= GetImportReference();
        settings.Output ??= GetOutputFileName();
    }


    private static string GetImportReference()
        => s_compileSettings.ImportReference ?? s_compileSettings.File.Directory!.FullName;


    private static string GetOutputFileName()
    {
        var fileName = Path.GetFileNameWithoutExtension(s_compileSettings.File.Name);

        var outputExtension = s_compileSettings.OutputType.OutputTypeToFileExtension();
        var outputName = s_compileSettings.Output ?? $"{fileName}.{outputExtension}";

        return outputName;
    }




    public static void Compile(CompileCommandSettings settings)
    {
        try
        {
            CompileSourceCodeToFile(settings);
        }
        catch (InterruptCompileException)
        { }
        catch (Exception exception)
        {
            DiagnosticLogger.LogInternalError(exception);
        }
    }


    private static void CompileSourceCodeToFile(CompileCommandSettings settings)
    {
        if (CompileSourceCodeToBitCode() is not { } bitCode)
            return;

        var options = CompilerProgramOptions.FromCompileCommandSettings(settings);
        ProgramToolchain.Compile(bitCode, settings.Output!, options);
    }




    private static string CompileSourceCodeToBitCode()
    {
        var moduleContext = GetModule(SourceCode.FilePath!);
        var bitCode = CompilerSteps.Compile(moduleContext, s_compileSettings);

        PrintASTIfRequested(moduleContext.SyntaxStatements);
        PrintLLVMIfRequested(bitCode);
        PrintASMIfRequested(bitCode);

        return bitCode;
    }




    public static Module GetModule(string file)
    {
        var source = File.ReadAllText(file);
        var statements = BuildFinalAST(source);
        var module = SemanticAnalysis(statements);

        return module;
    }




    private static Module SemanticAnalysis(IReadOnlyList<Statement> statements)
    {
        var moduleContext = CompilerSteps.Bind(statements, s_compileSettings.ImportReference!);

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




    private static void PrintASTIfRequested(IReadOnlyList<Statement> statements)
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
    });




    public static void Link(LinkCommandSettings settings)
    {
        s_linkSettings = settings;

        var fileNames = settings.Files.Select(file => file.FullName).ToArray();
        var options = LinkerProgramOptions.FromLinkCommandSettings(settings);

        ProgramToolchain.Link(fileNames, settings.Output, options);
    }




    private static void Interrupt()
        => throw new InterruptCompileException();
}
