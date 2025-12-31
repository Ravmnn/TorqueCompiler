using System;
using System.Linq;

using LLVMSharp.Interop;


namespace Torque.Compiler;




public static class PrimitiveTypeExtensions
{
    public static PrimitiveType StringToPrimitive(this string source)
        => Token.Primitives[source];


    public static PrimitiveType TokenToPrimitive(this Token token)
        => Token.Primitives[token.Lexeme];




    public static string PrimitiveToString(this PrimitiveType type)
        => Token.Primitives.First(pair => pair.Value == type).Key;




    public static LLVMTypeRef TokenToLLVMType(this Token token, LLVMTargetDataRef? targetData = null)
        => token.Lexeme.StringToLLVMType(targetData);


    public static LLVMTypeRef StringToLLVMType(this string source, LLVMTargetDataRef? targetData = null)
        => source.StringToPrimitive().PrimitiveToLLVMType(targetData);


    public static LLVMTypeRef PrimitiveToLLVMType(this PrimitiveType type, LLVMTargetDataRef? targetData = null) => type switch
    {
        PrimitiveType.Void => LLVMTypeRef.Void,
        PrimitiveType.PtrSize => LLVMTypeRef.CreateIntPtr(TargetMachine.GetDataLayoutOfOrGlobal(targetData)),

        PrimitiveType.Bool => LLVMTypeRef.Int1,

        PrimitiveType.Char or PrimitiveType.Int8 or PrimitiveType.UInt8 => LLVMTypeRef.Int8,
        PrimitiveType.Int16 or PrimitiveType.UInt16 => LLVMTypeRef.Int16,
        PrimitiveType.Int32 or PrimitiveType.UInt32 => LLVMTypeRef.Int32,
        PrimitiveType.Int64 or PrimitiveType.UInt64 => LLVMTypeRef.Int64,
        PrimitiveType.Float16 => LLVMTypeRef.Half,
        PrimitiveType.Float32 => LLVMTypeRef.Float,
        PrimitiveType.Float64 => LLVMTypeRef.Double,

        _ => throw new ArgumentException("Invalid primitive type.")
    };




    public static int SizeOfThisInMemory(this LLVMTypeRef type, LLVMTargetDataRef? targetData = null)
        => (int)TargetMachine.GetDataLayoutOfOrGlobal(targetData).ABISizeOfType(type);


    public static int SizeOfThisInMemory(this PrimitiveType type, LLVMTargetDataRef? targetData = null)
        => (int)TargetMachine.GetDataLayoutOfOrGlobal(targetData).ABISizeOfType(type.PrimitiveToLLVMType());


    // public static int SizeOfThisInBits(this PrimitiveType type) => type switch
    // {
    //     PrimitiveType.Void => 0,
    //     PrimitiveType.Bool => 1,
    //     PrimitiveType.Char or PrimitiveType.Int8 or PrimitiveType.UInt8 => 8,
    //     PrimitiveType.Int16 or PrimitiveType.UInt16 or PrimitiveType.Float16 => 16,
    //     PrimitiveType.Int32 or PrimitiveType.UInt32 or PrimitiveType.Float32 => 32,
    //     PrimitiveType.Int64 or PrimitiveType.UInt64 or PrimitiveType.Float64 => 64,
    //
    //     _ => throw new ArgumentException("Invalid primitive type")
    // };
}
