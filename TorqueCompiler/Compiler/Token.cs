using System.Collections.Generic;


namespace Torque.Compiler;




public enum TokenType
{
    SemiColon, Colon, Arrow,
    Comma,

    Plus, Minus, Star, Slash, Equal,
    ParenLeft, ParenRight,

    Identifier,
    Value,
    Type,

    KwStart, KwEnd, KwReturn, KwAs
}




public readonly record struct TokenLocation(uint Start, uint End, uint Line)
{
    public override string ToString()
        => $"line {Line}:{Start}-{End}";
};




public readonly record struct Token(string Lexeme, TokenType Type, TokenLocation Location)
{
    public static readonly string[] Types =
    [
        "byte", "char", "bool", "uint8", "uint16",
        "uint32", "uint64", "int8", "int16", "int32",
        "int64"
    ];


    public static readonly Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>
    {
        {"start", TokenType.KwStart},
        {"end", TokenType.KwEnd},
        {"return", TokenType.KwReturn},
        {"as", TokenType.KwAs}
    };




    public override string ToString()
        => $"\"{Lexeme}\" of type {Type}, at {Location}";
}
