using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Torque.Compiler;


namespace Torque;




public static class Torque
{
    private static CompileCommandSettings _settings = null!;

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
            _settings = settings;

            Source = File.ReadAllText(_settings.File.FullName);


            var tokens = new TorqueLexer(Source).Tokenize();

            if (Failed)
                return;

            var statements = new TorqueParser(tokens).Parse().ToArray();

            if (Failed || PrintedAST(statements))
                return;

            var compiler = new TorqueCompiler(statements, _settings.Debug) { FileInfo = _settings.File };
            var bitCode = compiler.Compile();

            if (Failed || PrintedLLVM(bitCode) || PrintedASM(bitCode))
                return;

            Toolchain.Compile(GetOutputFileName(), bitCode, _settings.OutputType, _settings.Debug);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Internal Error: {exception}"); // TODO: colorize
        }
    }


    private static string GetOutputFileName()
    {
        var fileName = Path.GetFileNameWithoutExtension(_settings.File.Name);

        var outputExtension = _settings.OutputType.OutputTypeToFileExtension();
        var outputName = _settings.Output ?? $"{fileName}.{outputExtension}";

        return outputName;
    }




    private static bool PrintedAST(IEnumerable<Statement> statements)
    {
        if (_settings.PrintAST)
        {
            Console.WriteLine(new ASTPrinter().Print(statements));
            return true;
        }

        return false;
    }


    private static bool PrintedLLVM(string bitCode)
    {
        if (_settings.PrintLLVM)
        {
            Console.WriteLine(bitCode);
            return true;
        }

        return false;
    }


    private static bool PrintedASM(string bitCode)
    {
        if (_settings.PrintASM)
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
