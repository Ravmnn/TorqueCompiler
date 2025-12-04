// ReSharper disable PossibleMultipleEnumeration


using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

using Torque.Compiler;
using Torque.Compiler.Diagnostics;


namespace Torque.CommandLine;




public static class Torque
{
    private static CompileCommandSettings s_settings = null!;

    public static bool Failed { get; private set; }




    public static void LogDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        if (!diagnostics.Any())
            return;

        Failed = true;

        foreach (var diagnostic in diagnostics)
            Console.Error.WriteLine(DiagnosticFormatter.Format(diagnostic));
    }




    public static void Compile(CompileCommandSettings settings)
    {
        try
        {
            s_settings = settings;

            SourceCode.Source = File.ReadAllText(s_settings.File.FullName);


            if (CompileToBitCode() is not { } bitCode)
                return;

            Toolchain.Compile(GetOutputFileName(), bitCode, s_settings.OutputType, s_settings.Debug);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Internal Error: {exception}");
        }
    }


    private static string? CompileToBitCode()
    {
        var lexer = new TorqueLexer(SourceCode.Source!);
        var tokens = lexer.Tokenize();
        LogDiagnostics(lexer.Diagnostics);

        if (Failed)
            return null;

        var parser = new TorqueParser(tokens);
        var statements = parser.Parse();
        LogDiagnostics(parser.Diagnostics);

        if (Failed || PrintedAST(statements))
            return null;

        var compiler = new TorqueCompiler(statements, s_settings.Debug) { FileInfo = s_settings.File };
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




    private static bool PrintedAST(IEnumerable<Statement> statements)
    {
        if (s_settings.PrintAST)
        {
            Console.WriteLine(new ASTPrinter().Print(statements));
            return true;
        }

        return false;
    }


    private static bool PrintedLLVM(string bitCode)
    {
        if (s_settings.PrintLLVM)
        {
            Console.WriteLine(bitCode);
            return true;
        }

        return false;
    }


    private static bool PrintedASM(string bitCode)
    {
        if (s_settings.PrintASM)
        {
            var tempFile = Path.GetTempFileName();
            Toolchain.Compile(tempFile, bitCode, OutputType.Assembly);

            var assembly = File.ReadAllText(tempFile);
            File.Delete(tempFile);

            Console.WriteLine(assembly);
            return true;
        }

        return false;
    }




    public static void Link(LinkCommandSettings settings)
    {
        var fileNames = from file in settings.Files select file.FullName;
        Toolchain.Link(fileNames, settings.Output, settings.Debug);
    }
}
