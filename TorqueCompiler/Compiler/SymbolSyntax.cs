namespace Torque.Compiler;




public struct SymbolSyntax(string name, Span location)
{
    public string Name { get; } = name;
    public Span Location { get; } = location;


    public SymbolSyntax(Token token) : this(token.Lexeme, token.Location)
    { }
}
