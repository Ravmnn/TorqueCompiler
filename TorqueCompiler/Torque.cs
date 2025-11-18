using System;
using System.Collections.Generic;
using System.IO;

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

            var statements = new TorqueParser(tokens).Parse();

            if (Failed)
                return;

            var compiler = ConstructCompilerWithTorqueOptions(statements, options);
            var compiled = compiler.Compile();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Error: {exception}");
        }
    }


    private static TorqueCompiler ConstructCompilerWithTorqueOptions(IEnumerable<Statement> statements, TorqueOptions options)
        => new TorqueCompiler(statements)
        {
            BitMode = options.BitMode,
            EntryPoint = options.EntryPoint
        };
}
