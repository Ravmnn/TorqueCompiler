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

            var compiler = ConstructCompilerFromOptions(statements);
            var compiled = compiler.Compile();

            if (Failed || PrintedLLVM(options, compiled))
                return;

            var fileName = Path.GetFileNameWithoutExtension(options.File.Name);
            var outputName = options.Output ?? $"{fileName}.o";

            CommandLine.Execute($"echo \"{compiled}\" | llc -o \"{outputName}\" -filetype=obj");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Error: {exception}"); // TODO: colorize
        }
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


    private static TorqueCompiler ConstructCompilerFromOptions(IEnumerable<Statement> statements)
        => new TorqueCompiler(statements);




    public static void Link(TorqueLinkOptions options)
    {

    }
}
