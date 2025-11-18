using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Torque.Compiler;


namespace Torque;




public static class Torque
{
    public static TorqueOptions Options { get; private set; }

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




    public static void Run(TorqueOptions options)
    {
        try
        {
            Options = options;

            Source = File.ReadAllText(options.File.FullName);


            var tokens = new TorqueLexer(Source).Tokenize();

            if (Failed)
                return;

            var statements = new TorqueParser(tokens).Parse().ToArray();

            if (PrintedAST(statements))
                return;

            if (Failed)
                return;

            var compiler = ConstructCompilerFromOptions(statements);
            var compiled = compiler.Compile();

            if (PrintedLLVM(compiled))
                return;

            File.WriteAllText(Options.Output, compiled);
            // TODO: make this executable
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Error: {exception}"); // TODO: colorize
        }
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


    private static bool PrintedLLVM(string compiled)
    {
        if (Options.PrintLLVM)
        {
            Console.WriteLine(compiled);
            return true;
        }

        return false;
    }


    private static TorqueCompiler ConstructCompilerFromOptions(IEnumerable<Statement> statements)
        => new TorqueCompiler(statements);
}
