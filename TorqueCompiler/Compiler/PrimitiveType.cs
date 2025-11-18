using System;

using LLVMSharp.Interop;


namespace Torque.Compiler;




public enum PrimitiveType
{
    Bool,
    Byte,
    Char,
    UInt8,
    UInt16,
    UInt32,
    UInt64,
    Int8,
    Int16,
    Int32,
    Int64
}




public static class PrimitiveTypeExtensions
{
    public static PrimitiveType StringToPrimitive(this string source)
        => Token.Primitives[source];


    public static LLVMTypeRef TokenToLLVMType(this Token token)
        => token.Lexeme.StringToLLVMType();


    public static LLVMTypeRef StringToLLVMType(this string source)
        => source.StringToPrimitive().PrimitiveToLLVMType();


    public static LLVMTypeRef PrimitiveToLLVMType(this PrimitiveType type) => type switch
    {
        PrimitiveType.Bool => LLVMTypeRef.Int1,
        PrimitiveType.Byte or PrimitiveType.Char => LLVMTypeRef.Int8,

        PrimitiveType.Int8 or PrimitiveType.UInt8 => LLVMTypeRef.Int8,
        PrimitiveType.Int16 or PrimitiveType.UInt16 => LLVMTypeRef.Int16,
        PrimitiveType.Int32 or PrimitiveType.UInt32 => LLVMTypeRef.Int32,
        PrimitiveType.Int64 or PrimitiveType.UInt64 => LLVMTypeRef.Int64,

        _ => throw new ArgumentException("Invalid primitive type.")
    };
}
