using System;
using System.IO;

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




    public static void Run(TorqueOptions options)
    {
        try
        {
            TorqueOptions.Global = options;

            if (!options.File.Exists)
                throw new FileNotFoundException("Could not open source file.", options.File.Name);

            Source = File.ReadAllText(options.File.FullName);


            var tokens = new TorqueLexer(Source).Tokenize();

            if (!Failed)
                foreach (var token in tokens)
                    Console.WriteLine(token);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Error: {exception}");
        }
    }
}
