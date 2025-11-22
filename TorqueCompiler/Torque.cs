using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Torque.Compiler;


namespace Torque;




public static class Torque
{
    public static string? Source { get; private set; }
    public static string[]? SourceLines => Source?.Split('\n');

    public static bool Failed { get; private set; }




    public static string GetSourceLine(uint line)
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
            Source = File.ReadAllText(options.File.FullName);


            var tokens = new TorqueLexer(Source).Tokenize();

            if (Failed)
                return;

            var statements = new TorqueParser(tokens).Parse().ToArray();

            if (Failed || PrintedAST(options, statements))
                return;

            var bitCode = new TorqueCompiler(statements).Compile();

            if (Failed || PrintedLLVM(options, bitCode))
                return;

            CommandLine.LLVMBitCodeToFile(GetOutputFileName(options), bitCode, options.OutputType);
        }
        catch (LanguageException exception)
        {
            Console.Error.WriteLine($"Error: {exception}"); // TODO: colorize
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Internal Error: {exception.Message}"); // TODO: colorize
        }
    }


    private static string GetOutputFileName(TorqueCompileOptions options)
    {
        var fileName = Path.GetFileNameWithoutExtension(options.File.Name);

        var outputExtension = options.OutputType.OutputTypeToFileExtension();
        var outputName = options.Output ?? $"{fileName}.{outputExtension}";

        return outputName;
    }


    private static bool PrintedAST(TorqueCompileOptions options, IEnumerable<Statement> statements)
    {
        if (options.PrintAST)
        {
            Console.WriteLine(new ASTPrinter().Print(statements));
            return true;
        }

        return false;
    }


    private static bool PrintedLLVM(TorqueCompileOptions options, string compiled)
    {
        if (options.PrintLLVM)
        {
            Console.WriteLine(compiled);
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
