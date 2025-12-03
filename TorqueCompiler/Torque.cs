using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Torque.Compiler;


namespace Torque;




public static class Torque
{
    // TODO: don't use global settings... backend should be independent from frontend
    public static CompileCommandSettings Settings { get; private set; } = null!;

    public static string? Source { get; private set; }
    public static string[]? SourceLines => Source?.Split('\n');

    public static bool Failed { get; private set; }




    public static string GetSourceLine(int line)
        => SourceLines![line - 1];




    public static void LogError(LanguageException exception)
    {
        Failed = true;
        Console.Error.WriteLine(exception.ToString());
    }




    public static void Compile(CompileCommandSettings settings)
    {
        try
        {
            Settings = settings;
            Source = File.ReadAllText(Settings.File.FullName);


            var tokens = new TorqueLexer(Source).Tokenize();

            if (Failed)
                return;

            var statements = new TorqueParser(tokens).Parse().ToArray();

            if (Failed || PrintedAST(statements))
                return;

            var bitCode = new TorqueCompiler(statements, Settings.Debug).Compile();

            if (Failed || PrintedLLVM(bitCode) || PrintedASM(bitCode))
                return;

            Toolchain.Compile(GetOutputFileName(), bitCode, Settings.OutputType, Settings.Debug);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Internal Error: {exception}"); // TODO: colorize
        }
    }


    private static string GetOutputFileName()
    {
        var fileName = Path.GetFileNameWithoutExtension(Settings.File.Name);

        var outputExtension = Settings.OutputType.OutputTypeToFileExtension();
        var outputName = Settings.Output ?? $"{fileName}.{outputExtension}";

        return outputName;
    }




    private static bool PrintedAST(IEnumerable<Statement> statements)
    {
        if (Settings.PrintAST)
        {
            Console.WriteLine(new ASTPrinter().Print(statements));
            return true;
        }

        return false;
    }


    private static bool PrintedLLVM(string bitCode)
    {
        if (Settings.PrintLLVM)
        {
            Console.WriteLine(bitCode);
            return true;
        }

        return false;
    }


    private static bool PrintedASM(string bitCode)
    {
        if (Settings.PrintASM)
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
