namespace Torque.Compiler;




public readonly record struct SourceLocation(int Start, int End, int Line)
{
    public SourceLocation(SourceLocation start, SourceLocation end)
        : this(start.Start, end.End, start.Line)
    {}


    public override string ToString()
        => $"{Line}:{Start}-{End}";
}
