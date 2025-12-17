// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable LocalizableElement


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Torque.Compiler;
using Torque.Compiler.Diagnostics;


namespace Torque.CommandLine;




public static class Torque
{
    private static CompileCommandSettings s_settings = null!;

    public static bool Failed { get; private set; }




    public static void Compile(CompileCommandSettings settings)
    {
        try
        {
            s_settings = settings;

            SourceCode.Source = File.ReadAllText(s_settings.File.FullName);
            SourceCode.FileName = settings.File.Name;


            if (CompileToBitCode() is not { } bitCode)
                return;

            Toolchain.Compile(GetOutputFileName(), bitCode, s_settings.OutputType, s_settings.Debug);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Internal Error: {exception}");
        }
    }


    private static string? CompileToBitCode()
    {
        // tokenize
        var lexer = new TorqueLexer(SourceCode.Source!);
        var tokens = lexer.Tokenize();
        LogDiagnostics(lexer.Diagnostics);

        if (Failed)
            return null;


        // parse
        var parser = new TorqueParser(tokens);
        var statements = parser.Parse();
        LogDiagnostics(parser.Diagnostics);

        if (Failed || PrintedAST(statements))
            return null;


        // bind
        var binder = new TorqueBinder(statements);
        var boundStatements = binder.Bind();
        LogDiagnostics(binder.Diagnostics);

        if (Failed)
            return null;


        // type check
        var typeChecker = new TorqueTypeChecker(boundStatements);
        typeChecker.Check();
        LogDiagnostics(typeChecker.Diagnostics);

        if (Failed)
            return null;


        // control flow analysis
        var functionDeclarations = boundStatements.Cast<BoundFunctionDeclarationStatement>().ToArray();
        var graphs = new ControlFlowGraphBuilder(functionDeclarations).Build();

        var controlFlowReporter = new ControlFlowAnalysisReporter(graphs);
        controlFlowReporter.Report();
        LogDiagnostics(controlFlowReporter.Diagnostics);

        if (Failed)
            return null;


        // compile
        var compiler = new TorqueCompiler(boundStatements, binder.Scope, s_settings.File, s_settings.Debug);
        var bitCode = compiler.Compile();

        if (PrintedLLVM(bitCode) || PrintedASM(bitCode))
            return null;


        return bitCode;
    }


    private static string GetOutputFileName()
    {
        var fileName = Path.GetFileNameWithoutExtension(s_settings.File.Name);

        var outputExtension = s_settings.OutputType.OutputTypeToFileExtension();
        var outputName = s_settings.Output ?? $"{fileName}.{outputExtension}";

        return outputName;
    }




    private static bool PrintedAST(IReadOnlyList<Statement> statements)
    {
        if (!s_settings.PrintAST)
            return false;

        Console.WriteLine(new ASTPrinter().Print(statements));
        return true;

    }


    private static bool PrintedLLVM(string bitCode)
    {
        if (!s_settings.PrintLLVM)
            return false;

        Console.WriteLine(bitCode);
        return true;
    }


    private static bool PrintedASM(string bitCode)
    {
        if (!s_settings.PrintASM)
            return false;

        var tempFile = Path.GetTempFileName();
        Toolchain.Compile(tempFile, bitCode, OutputType.Assembly);

        var assembly = File.ReadAllText(tempFile);
        File.Delete(tempFile);

        Console.WriteLine(assembly);
        return true;
    }




    public static void Link(LinkCommandSettings settings)
    {
        var fileNames = settings.Files.Select(file => file.FullName).ToArray();
        Toolchain.Link(fileNames, settings.Output, settings.Debug);
    }




    public static void LogDiagnostics(IReadOnlyList<Diagnostic> diagnostics)
    {
        if (!diagnostics.Any())
            return;

        foreach (var diagnostic in diagnostics)
        {
            Console.WriteLine(DiagnosticFormatter.Format(diagnostic));

            if (diagnostic.Severity == DiagnosticSeverity.Error)
                Failed = true;
        }
    }
}
