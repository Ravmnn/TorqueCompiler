// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable LocalizableElement


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Torque.Compiler;
using Torque.Compiler.AST.Statements;
using Torque.Compiler.BoundAST.Statements;
using Torque.Compiler.Target;
using Torque.CommandLine.Toolchain;


namespace Torque.CommandLine;




public static class Torque
{
    private static CompileCommandSettings s_compileSettings = null!;
    private static LinkCommandSettings s_linkSettings = null!;

    public static DiagnosticLogger Logger { get; set; } = new DiagnosticLogger();




    private static void InitializeGlobals(CompileCommandSettings settings)
    {
        InitializeGlobalSourceCodeReference(settings.File);
        InitializeGlobalTargetMachine(settings);
    }


    private static void InitializeGlobalSourceCodeReference(FileInfo file)
    {
        SourceCode.Source = File.ReadAllText(file.FullName);
        SourceCode.FileName = file.Name;
    }


    private static void InitializeGlobalTargetMachine(CompileCommandSettings settings)
    {
        var targetTriple = TargetTriple.FromCompileSettings(settings);
        TargetMachine.SetGlobalTarget(targetTriple.ToString());
    }




    public static void Compile(CompileCommandSettings settings)
    {
        s_compileSettings = settings;

        try
        {
            InitializeGlobals(settings);
            CompileSourceCodeToFile(settings);
        }
        catch (InterruptCompileException)
        { }
        catch (Exception exception)
        {
            Logger.LogInternalError(exception);
        }
    }


    private static void CompileSourceCodeToFile(CompileCommandSettings settings)
    {
        if (CompileSourceCodeToBitCode() is not { } bitCode)
            return;

        var options = CompilerProgramOptions.FromCompileCommandSettings(settings);
        ProgramToolchain.Compile(bitCode, GetOutputFileName(), options);
    }




    private static string CompileSourceCodeToBitCode()
    {
        var statements = BuildFinalAST();
        PrintASTIfRequested(statements);

        var (boundStatements, scope) = SemanticAnalysis(statements);
        var bitCode = CompilerSteps.Compile(boundStatements, scope, s_compileSettings);

        PrintLLVMIfRequested(bitCode);
        PrintASMIfRequested(bitCode);

        return bitCode;
    }


    private static (IReadOnlyList<BoundStatement>, Scope) SemanticAnalysis(IReadOnlyList<Statement> statements)
    {
        var (boundStatements, scope, declaredTypes) = CompilerSteps.Bind(statements);

        CompilerSteps.TypeCheck(boundStatements, declaredTypes);
        CompilerSteps.AnalyzeControlFlow(boundStatements);

        return (boundStatements, scope);
    }


    private static IReadOnlyList<Statement> BuildFinalAST()
    {
        var tokens = CompilerSteps.Tokenize();
        var statements = CompilerSteps.Parse(tokens);
        statements = CompilerSteps.Desugarize(statements);

        return statements;
    }


    private static string GetOutputFileName()
    {
        var fileName = Path.GetFileNameWithoutExtension(s_compileSettings.File.Name);

        var outputExtension = s_compileSettings.OutputType.OutputTypeToFileExtension();
        var outputName = s_compileSettings.Output ?? $"{fileName}.{outputExtension}";

        return outputName;
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
