using System.Collections.Generic;


namespace Torque.Compiler;




public enum TokenType
{
    SemiColon, Colon, Arrow,
    Comma,

    Exclamation, Ampersand, Pipe,
    Plus, Minus, Star, Slash, Equal,
    LeftParen, RightParen, LeftCurlyBrace, RightCurlyBrace,
    GreaterThan, LessThan,

    // TODO: add bitwise operations &, | and ^ (XOR)
    GreaterThanOrEqual, LessThanOrEqual,
    Equality, Inequality, LogicAnd, LogicOr,

    Identifier,
    Value,
    Type,

    KwReturn, KwAs
}




public readonly record struct Token(string Lexeme, TokenType Type, SourceLocation Location, object? Value = null)
{
    public static readonly IReadOnlyDictionary<string, PrimitiveType> Primitives = new Dictionary<string, PrimitiveType>
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
        {"float16", PrimitiveType.Float16 },
        {"float32", PrimitiveType.Float32 },
        {"float64", PrimitiveType.Float64 },

        // TODO: add more type aliases
        {"byte", PrimitiveType.UInt8 },
        {"uint", PrimitiveType.UInt32 },
        {"int", PrimitiveType.Int32 }
    };


    public static readonly IReadOnlyDictionary<string, TokenType> Keywords = new Dictionary<string, TokenType>
    {
        {"return", TokenType.KwReturn},
        {"as", TokenType.KwAs}
    };




    public override string ToString()
        => $"\"{Lexeme}\" of type {Type}, at {Location}";


    public static implicit operator SourceLocation(Token token) => token.Location;
}
