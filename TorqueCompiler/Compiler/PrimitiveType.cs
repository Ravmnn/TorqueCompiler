using System;
using System.Linq;

using LLVMSharp.Interop;


namespace Torque.Compiler;




public enum PrimitiveType
{
    Void,
    Bool,
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


    public static PrimitiveType TokenToPrimitive(this Token token)
        => Token.Primitives[token.Lexeme];




    public static string PrimitiveToString(this PrimitiveType type)
        => Token.Primitives.First(pair => pair.Value == type).Key;




    public static LLVMTypeRef TokenToLLVMType(this Token token)
        => token.Lexeme.StringToLLVMType();


    public static LLVMTypeRef StringToLLVMType(this string source)
        => source.StringToPrimitive().PrimitiveToLLVMType();


    public static LLVMTypeRef PrimitiveToLLVMType(this PrimitiveType type) => type switch
    {
        PrimitiveType.Void => LLVMTypeRef.Void,

        PrimitiveType.Bool => LLVMTypeRef.Int1,

        PrimitiveType.Char or PrimitiveType.Int8 or PrimitiveType.UInt8 => LLVMTypeRef.Int8,
        PrimitiveType.Int16 or PrimitiveType.UInt16 => LLVMTypeRef.Int16,
        PrimitiveType.Int32 or PrimitiveType.UInt32 => LLVMTypeRef.Int32,
        PrimitiveType.Int64 or PrimitiveType.UInt64 => LLVMTypeRef.Int64,

        _ => throw new ArgumentException("Invalid primitive type.")
    };




    public static int SizeOfThisInMemory(this LLVMTypeRef type, LLVMTargetDataRef targetData)
        => (int)targetData.ABISizeOfType(type);


    public static int SizeOfThisInMemory(this PrimitiveType type, LLVMTargetDataRef targetData)
        => (int)targetData.ABISizeOfType(type.PrimitiveToLLVMType());


    public static int SizeOfThisInBits(this PrimitiveType type) => type switch
    {
        PrimitiveType.Void => 0,
        PrimitiveType.Bool => 1,
        PrimitiveType.Char or PrimitiveType.Int8 or PrimitiveType.UInt8 => 8,
        PrimitiveType.Int16 or PrimitiveType.UInt16 => 16,
        PrimitiveType.Int32 or PrimitiveType.UInt32 => 32,
        PrimitiveType.Int64 or PrimitiveType.UInt64 => 64,

        _ => throw new ArgumentException("Invalid primitive type")
    };
}
