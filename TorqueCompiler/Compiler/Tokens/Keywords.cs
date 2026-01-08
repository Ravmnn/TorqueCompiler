using System.Collections.Generic;

using Torque.Compiler.Types;


namespace Torque.Compiler.Tokens;




public static class Keywords
{
    public static readonly IReadOnlyDictionary<string, PrimitiveType> PrimitiveTypes = new Dictionary<string, PrimitiveType>
    {
        { "let", PrimitiveType.Auto },

        { "void", PrimitiveType.Void },

        { "bool", PrimitiveType.Bool },
        { "char", PrimitiveType.Char },
        { "uint8", PrimitiveType.UInt8 },
        { "uint16", PrimitiveType.UInt16 },
        { "uint32", PrimitiveType.UInt32 },
        { "uint64", PrimitiveType.UInt64 },
        { "int8", PrimitiveType.Int8 },
        { "int16", PrimitiveType.Int16 },
        { "int32", PrimitiveType.Int32 },
        { "int64", PrimitiveType.Int64 },
        { "float16", PrimitiveType.Float16 },
        { "float32", PrimitiveType.Float32 },
        { "float64", PrimitiveType.Float64 },

        { "byte", PrimitiveType.UInt8 },
        { "uint", PrimitiveType.UInt32 },
        { "int", PrimitiveType.Int32 },
        { "half", PrimitiveType.Float16 },
        { "float", PrimitiveType.Float32 },
        { "double", PrimitiveType.Float64 },
        { "ptrsize", PrimitiveType.PtrSize }
    };


    public static readonly IReadOnlyDictionary<string, TokenType> General = new Dictionary<string, TokenType>
    {
        { "if", TokenType.KwIf },
        { "else", TokenType.KwElse },
        { "return", TokenType.KwReturn },
        { "as", TokenType.KwAs },
        { "array", TokenType.KwArray },
        { "default", TokenType.KwDefault }
    };
}
