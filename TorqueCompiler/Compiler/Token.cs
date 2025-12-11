using System.Collections.Generic;


namespace Torque.Compiler;




public enum TokenType
{
    SemiColon, Colon, Arrow,
    Comma,

    Exclamation,
    Plus, Minus, Star, Slash, Equal, Ampersand,
    LeftParen, RightParen, LeftCurlyBrace, RightCurlyBrace,

    Identifier,
    Value,
    Type,

    KwReturn, KwAs
}




// TODO: add support for TokenRange when logging exceptions and stuff like that
public readonly record struct TokenLocation(int Start, int End, int Line)
{
    public override string ToString()
        => $"line {Line}:{Start}-{End}";
}




public readonly record struct Token(string Lexeme, TokenType Type, TokenLocation Location)
{
    public static readonly Dictionary<string, PrimitiveType> Primitives = new Dictionary<string, PrimitiveType>
    {
        {"void", PrimitiveType.Void},

        {"bool", PrimitiveType.Bool },
        {"char", PrimitiveType.Char },
        {"uint8", PrimitiveType.UInt8 },
        {"uint16", PrimitiveType.UInt16 },
        {"uint32", PrimitiveType.UInt32 },
        {"uint64", PrimitiveType.UInt64 },
        {"int8", PrimitiveType.Int8 },
        {"int16", PrimitiveType.Int16 },
        {"int32", PrimitiveType.Int32 },
        {"int64", PrimitiveType.Int64 },

        {"byte", PrimitiveType.UInt8 },
        {"uint", PrimitiveType.UInt32 },
        {"int", PrimitiveType.Int32 }
    };




    public static readonly Dictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>
    {
        {"return", TokenType.KwReturn},
        {"as", TokenType.KwAs}
    };




    public override string ToString()
        => $"\"{Lexeme}\" of type {Type}, at {Location}";




    public static implicit operator TokenLocation(Token token) => token.Location;
}
