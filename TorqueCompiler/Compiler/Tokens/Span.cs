namespace Torque.Compiler.Tokens;




public readonly record struct Span(int Start, int End, int Line)
{
    public Span(Span start, Span end)
        : this(start.Start, end.End, start.Line)
    {}


    public override string ToString()
        => $"{Line}:{Start}-{End}";
}
