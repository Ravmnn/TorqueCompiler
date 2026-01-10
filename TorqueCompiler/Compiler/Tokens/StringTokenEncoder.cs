using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Diagnostics;
using Torque.Compiler.Diagnostics.Catalogs;


namespace Torque.Compiler.Tokens;




public class StringTokenEncoder(string text) : DiagnosticReporter<LexerCatalog>
{
    public static IReadOnlyList<IEscapeSequence> DefaultEscapeProcessors { get; } =
    [
        new SingleEscapeSequence('0', (byte)'\0'),
        new SingleEscapeSequence('t', (byte)'\t'),
        new SingleEscapeSequence('n', (byte)'\n'),
        new SingleEscapeSequence('r', (byte)'\r'),
        new SingleEscapeSequence('e', (byte)'\e'),

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
            data.Add(GetNextCharacterOrEscapeSequenceByte());

        return data;
    }


    private byte GetNextCharacterOrEscapeSequenceByte()
    {
        var character = GetNextCharacter();
        return GetCharacterOrEscapeSequenceByte(character);
    }


    private char GetNextCharacter()
    {
        _start = _end;
        return Advance();
    }


    private byte GetCharacterOrEscapeSequenceByte(char character)
    {
        if (character == '\\')
            return GetEscapeSequenceByte();

        return (byte)character;
    }


    private byte GetEscapeSequenceByte()
    {
        var name = Advance();
        var sequence = GetEscapeSequenceByNameOrReportIfNull(name);

        if (sequence is null)
            return 0;

        var argument = GetArgumentFromArity(sequence.Arity);

        return sequence.GetByte(argument);
    }


    private string GetArgumentFromArity(int arity)
    {
        var argument = arity > 0 ? GetArgument(arity) : string.Empty;

        FillArgumentWithZeros(ref argument, arity);
        return argument;
    }


    private string GetArgument(int arity)
    {
        var argument = string.Empty;

        for (var i = 0; i < arity && !AtEnd(); i++)
            argument += Advance();

        return argument;
    }


    private static void FillArgumentWithZeros(ref string argument, int arity)
    {
        if (argument.Length >= arity)
            return;

        var zeroAmount = arity - argument.Length;
        argument += new string('0', zeroAmount);
    }




    private IEscapeSequence? GetEscapeSequenceByNameOrReportIfNull(char name)
    {
        var processor = GetEscapeSequenceByName(name);

        if (processor is null)
            Report(LexerCatalog.UnknownEscapeSequence, location: GetCurrentLocation());

        return processor;
    }


    private IEscapeSequence? GetEscapeSequenceByName(char name)
        => DefaultEscapeProcessors.FirstOrDefault(processor => processor.Name == name);


    private Span GetCurrentLocation()
        => new Span(_start, _end, 0);




    private char Advance()
        => AtEnd() ? Previous() : Text[_end++];


    private char Peek()
        => AtEnd() ? Previous() : Text[_end];


    private char Previous()
        => Text[_end - 1];


    private bool AtEnd()
        => _end >= Text.Length;
}
