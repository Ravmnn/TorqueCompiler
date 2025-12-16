using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Diagnostics;


namespace Torque.Compiler;




public interface IEscapeSequenceProcessor
{
    char Name { get; }
    int Arity { get; }


    byte Get(string argument);
}


public struct SingleEscapeSequence(char name, byte value) : IEscapeSequenceProcessor
{
    public char Name { get; } = name;
    public int Arity => 0;

    public byte Value { get; } = value;


    public byte Get(string argument) => Value;
}




public class StringTokenEncoder(string text) : DiagnosticReporter<Diagnostic.LexerCatalog>
{
    public static IReadOnlyList<IEscapeSequenceProcessor> DefaultEscapeProcessors { get; } =
    [
        new SingleEscapeSequence('0', 0),
        new SingleEscapeSequence('t', 9),
        new SingleEscapeSequence('n', 10),
        new SingleEscapeSequence('r', 13),
        new SingleEscapeSequence('e', 27),

        new SingleEscapeSequence('\'', (byte)'\''),
        new SingleEscapeSequence('"', (byte)'"'),
        new SingleEscapeSequence('\\', (byte)'\\')
    ];




    private int _start;
    private int _end;


    public string Text { get; } = text;




    public IReadOnlyList<byte> ToASCII()
    {
        var data = new List<byte>();

        while (!AtEnd())
        {
            _start = _end;
            var current = Text[_end];
            _end++;

            if (current == '\\')
                data.Add(ProcessEscapeSequence());
            else
                data.Add((byte)current);

        }

        return data;
    }


    private byte ProcessEscapeSequence()
    {
        var name = Text[_end++];
        var processor = GetEscapeSequenceProcessorByName(name);
        var argument = string.Empty;

        if (processor is null)
            return 0;

        if (processor.Arity > 0)
            argument = GetArgument(processor.Arity);

        FillArgumentWithZeros(ref argument, processor);

        return processor.Get(argument);
    }


    private string GetArgument(int arity)
    {
        var argument = string.Empty;

        for (var o = 0; o < arity && _end < Text.Length; o++, _end++)
            argument += Text[_end];

        return argument;
    }


    private static void FillArgumentWithZeros(ref string argument, IEscapeSequenceProcessor processor)
    {
        if (argument.Length < processor.Arity)
            argument += new string('0', processor.Arity - argument.Length);
    }


    private IEscapeSequenceProcessor? GetEscapeSequenceProcessorByName(char name)
    {
        var processor = DefaultEscapeProcessors.FirstOrDefault(processor => processor.Name == name);

        if (processor is null)
            Report(Diagnostic.LexerCatalog.UnknownEscapeSequence, location: GetCurrentLocation());

        return processor;
    }




    private SourceLocation GetCurrentLocation()
        => new SourceLocation(_start, _end, 0);




    private bool AtEnd()
        => _end >= Text.Length;
}
