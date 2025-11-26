using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Torque.Compiler;


namespace Torque;




public static class Torque
{
    public static TorqueCompileOptions Options { get; private set; }

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




    public static void Compile(TorqueCompileOptions options)
    {
        try
        {
            Options = options;
            Source = File.ReadAllText(Options.File.FullName);


            var tokens = new TorqueLexer(Source).Tokenize();

            if (Failed)
                return;

            var statements = new TorqueParser(tokens).Parse().ToArray();

            if (Failed || PrintedAST(statements))
                return;

            var bitCode = new TorqueCompiler(statements, Options.Debug).Compile();

            if (Failed || PrintedLLVM(bitCode) || PrintedASM(bitCode))
                return;

            CommandLine.LLVMBitCodeToFile(GetOutputFileName(), bitCode, Options.OutputType);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Internal Error: {exception.Message}"); // TODO: colorize
        }
    }


    private static string GetOutputFileName()
    {
        var fileName = Path.GetFileNameWithoutExtension(Options.File.Name);

        var outputExtension = Options.OutputType.OutputTypeToFileExtension();
        var outputName = Options.Output ?? $"{fileName}.{outputExtension}";

        return outputName;
    }


    private static bool PrintedAST(IEnumerable<Statement> statements)
    {
        if (Options.PrintAST)
        {
            Console.WriteLine(new ASTPrinter().Print(statements));
            return true;
        }

        return false;
    }


    private static bool PrintedLLVM(string bitCode)
    {
        if (Options.PrintLLVM)
        {
            Console.WriteLine(bitCode);
            return true;
        }

        return false;
    }


    private static bool PrintedASM(string bitCode)
    {
        if (Options.PrintASM)
        {
            var tempFile = Path.GetTempFileName();
            CommandLine.LLVMBitCodeToFile(tempFile, bitCode, OutputType.Assembly);

            var assembly = File.ReadAllText(tempFile);
            File.Delete(tempFile);

            Console.WriteLine(assembly);
            return true;
        }

        return false;
    }




    public static void Link(TorqueLinkOptions options)
    {
        var fileNames = from file in options.Files select file.FullName;
        CommandLine.Link(fileNames, options.Output);
    }
}
