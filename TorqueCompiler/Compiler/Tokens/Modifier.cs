namespace Torque.Compiler.Tokens;




public readonly record struct Modifier(TokenType Type, Span Location)
{
    public Modifier(Token modifier) : this(modifier.Type, modifier.Location)
    {}


    public static implicit operator TokenType(Modifier modifier) => modifier.Type;
}
