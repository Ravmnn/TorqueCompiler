namespace Torque.Compiler.Tokens;




public enum TokenType
{
    SemiColon, Colon, Arrow,
    Comma,

    Exclamation, Ampersand, Pipe,
    Plus, Minus, Star, Slash, Equal,
    LeftParen, RightParen, LeftCurlyBracket, RightCurlyBracket,
    LeftSquareBracket, RightSquareBracket,
    GreaterThan, LessThan,


    // TODO: add bitwise operations &, | and ^ (XOR)
    GreaterThanOrEqual, LessThanOrEqual,
    Equality, Inequality, LogicAnd, LogicOr,

    Identifier,
    IntegerValue, FloatValue, BoolValue, CharValue, StringValue,
    Type,

    KwIf, KwElse, KwReturn, KwAs, KwArray, KwDefault
}




public readonly record struct Token(string Lexeme, TokenType Type, Span Location, object? Value = null)
{
    public override string ToString()
        => $"\"{Lexeme}\" of type {Type}, at {Location}";


    public static implicit operator Span(Token token) => token.Location;
}
