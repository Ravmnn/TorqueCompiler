using Torque.Compiler.Tokens;


namespace Torque.Compiler.Symbols;




public readonly struct SymbolSyntax(string name, Span location) : IName
{
    public string Name { get; } = name;
    public Span Location { get; } = location;




    public SymbolSyntax(Token token) : this(token.Lexeme, token.Location)
    { }




    public override string ToString()
        => Name;
}
