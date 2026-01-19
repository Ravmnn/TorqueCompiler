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
using Torque.Compiler.Tokens;


namespace Torque.CommandLine;




public static class Torque
{
    private static CompileCommandSettings s_settings = null!;

    public static DiagnosticLogger Logger { get; set; } = new DiagnosticLogger();




    public static void CompileToFile(CompileCommandSettings settings)
    {
        try
        {
            InitializeGlobals(settings);

            if (CompileToBitCode() is { } bitCode)
                Toolchain.Compile(GetOutputFileName(), bitCode, s_settings.OutputType, s_settings.Debug);
        }
        catch (InterruptCompileException)
        { }
        catch (Exception exception)
        {
            Console.WriteLine($"Internal Error: {exception}");
        }
    }


    private static void InitializeGlobals(CompileCommandSettings settings)
    {
        s_settings = settings;

        SourceCode.Source = File.ReadAllText(s_settings.File.FullName);
        SourceCode.FileName = settings.File.Name;

        var targetTriple = TargetTriple.FromCompileSettings(settings);
        TargetMachine.SetGlobalTarget(targetTriple.ToString());
    }


    private static string GetOutputFileName()
    {
        var fileName = Path.GetFileNameWithoutExtension(s_settings.File.Name);

        var outputExtension = s_settings.OutputType.OutputTypeToFileExtension();
        var outputName = s_settings.Output ?? $"{fileName}.{outputExtension}";

        return outputName;
    }




    private static string CompileBitCodeToAssembly(string bitCode) => TempFiles.ForTempFileDo(file =>
    {
        Toolchain.Compile(file, bitCode, OutputType.Assembly);
        return File.ReadAllText(file);
    });


    private static string CompileToBitCode()
    {
        var statements = BuildFinalAST();
        PrintASTIfRequested(statements);

        var (boundStatements, scope) = SemanticAnalysis(statements);
        var bitCode = Compile(boundStatements, scope);

        PrintLLVMIfRequested(bitCode);
        PrintASMIfRequested(bitCode);

        return bitCode;
    }


    private static (IReadOnlyList<BoundStatement>, Scope) SemanticAnalysis(IReadOnlyList<Statement> statements)
    {
        var (boundStatements, scope) = Bind(statements);

        TypeCheck(boundStatements);
        AnalyzeControlFlow(boundStatements);

        return (boundStatements, scope);
    }


    private static IReadOnlyList<Statement> BuildFinalAST()
    {
        var tokens = Tokenixe();
        var statements = Parse(tokens);
        statements = Desugarize(statements);

        return statements;
    }




    private static string Compile(IReadOnlyList<BoundStatement> boundStatements, Scope scope)
    {
        var compiler = new TorqueCompiler(boundStatements, scope, s_settings.File, s_settings.Debug);
        var bitCode = compiler.Compile();

        return bitCode;
    }


    private static void AnalyzeControlFlow(IReadOnlyList<BoundStatement> boundStatements)
    {
        var functionDeclarations = boundStatements.Cast<BoundFunctionDeclarationStatement>().ToArray();
        var graphs = new ControlFlowGraphBuilder(functionDeclarations).Build();

        var controlFlowReporter = new ControlFlowAnalysisReporter(graphs);
        controlFlowReporter.Report();

        Logger.LogDiagnosticsAndInterruptIfAny(controlFlowReporter.Diagnostics);
    }


    private static void TypeCheck(IReadOnlyList<BoundStatement> boundStatements)
    {
        var typeChecker = new TorqueTypeChecker(boundStatements);
        typeChecker.Check();

        Logger.LogDiagnosticsAndInterruptIfAny(typeChecker.Diagnostics);
    }


    private static (IReadOnlyList<BoundStatement>, Scope) Bind(IReadOnlyList<Statement> statements)
    {
        var binder = new TorqueBinder(statements);
        var boundStatements = binder.Bind();

        Logger.LogDiagnosticsAndInterruptIfAny(binder.Diagnostics);

        return (boundStatements, binder.Scope);
    }


    private static IReadOnlyList<Statement> Desugarize(IReadOnlyList<Statement> statements)
    {
        var desugarizer = new TorqueDesugarizer(statements);
        statements = desugarizer.Desugarize(); // desugarizer cannot fail

        return statements;
    }


    private static IReadOnlyList<Statement> Parse(IReadOnlyList<Token> tokens)
    {
        var parser = new TorqueParser(tokens);
        var statements = parser.Parse();

        Logger.LogDiagnosticsAndInterruptIfAny(parser.Diagnostics);

        return statements;
    }


    private static IReadOnlyList<Token> Tokenixe()
    {
        var lexer = new TorqueLexer(SourceCode.Source!);
        var tokens = lexer.Tokenize();

        Logger.LogDiagnosticsAndInterruptIfAny(lexer.Diagnostics);

        return tokens;
    }




    private static void PrintASTIfRequested(IReadOnlyList<Statement> statements)
    {
        if (!s_settings.PrintAST)
            return;

        Console.WriteLine(new ASTPrinter().Print(statements));
        Interrupt();
    }


    private static void PrintLLVMIfRequested(string bitCode)
    {
        if (!s_settings.PrintLLVM)
            return;

        Console.WriteLine(bitCode);
        Interrupt();
    }


    private static void PrintASMIfRequested(string bitCode)
    {
        if (!s_settings.PrintASM)
            return;

        var assembly = CompileBitCodeToAssembly(bitCode);
        Console.WriteLine(assembly);
        Interrupt();
    }




    public static void Link(LinkCommandSettings settings)
    {
        var fileNames = settings.Files.Select(file => file.FullName).ToArray();
        Toolchain.Link(fileNames, settings.Output, settings.Debug);
    }




    private static void Interrupt()
        => throw new InterruptCompileException();
}
