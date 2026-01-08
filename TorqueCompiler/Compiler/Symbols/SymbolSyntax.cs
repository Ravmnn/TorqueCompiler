using Torque.Compiler.Tokens;


namespace Torque.Compiler.Symbols;




public readonly struct SymbolSyntax(string name, Span location)
{
    public string Name { get; } = name;
    public Span Location { get; } = location;


    public SymbolSyntax(Token token) : this(token.Lexeme, token.Location)
    { }
}
