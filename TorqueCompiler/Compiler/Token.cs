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
    public static readonly Dictionary<string, PrimitiveType> Primitives = new Dictionary<string, PrimitiveType>
    {
        {"byte", PrimitiveType.Byte },
        {"char", PrimitiveType.Char },
        {"bool", PrimitiveType.Bool },
        {"uint", PrimitiveType.UInt32 },
        {"uint8", PrimitiveType.UInt8 },
        {"uint16", PrimitiveType.UInt16 },
        {"uint32", PrimitiveType.UInt32 },
        {"uint64", PrimitiveType.UInt64 },
        {"int", PrimitiveType.Int32 },
        {"int8", PrimitiveType.Int8 },
        {"int16", PrimitiveType.Int16 },
        {"int32", PrimitiveType.Int32 },
        {"int64", PrimitiveType.Int64 }
    };




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
