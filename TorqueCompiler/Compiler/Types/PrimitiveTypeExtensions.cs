using System;
using System.Linq;

using LLVMSharp.Interop;

using Torque.Compiler.Tokens;
using Torque.Compiler.Symbols;
using Torque.Compiler.Target;


namespace Torque.Compiler.Types;




public static class PrimitiveTypeExtensions
{
    public static PrimitiveType StringToPrimitiveType(this string source)
        => Keywords.PrimitiveTypes[source];


    public static PrimitiveType SymbolToPrimitiveType(this SymbolSyntax token)
        => Keywords.PrimitiveTypes[token.Name];




    public static string PrimitiveTypeToString(this PrimitiveType type)
        => Keywords.PrimitiveTypes.First(pair => pair.Value == type).Key;




    public static LLVMTypeRef TokenToLLVMType(this Token token, LLVMTargetDataRef? targetData = null)
        => token.Lexeme.StringToLLVMType(targetData);


    public static LLVMTypeRef StringToLLVMType(this string source, LLVMTargetDataRef? targetData = null)
        => source.StringToPrimitiveType().PrimitiveTypeToLLVMType(targetData);


    public static LLVMTypeRef PrimitiveTypeToLLVMType(this PrimitiveType type, LLVMTargetDataRef? targetData = null) => type switch
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




    public static int SizeOfThisInMemory(this PrimitiveType type, LLVMTargetDataRef? targetData = null)
        => type.PrimitiveTypeToLLVMType().SizeOfThisInMemory();
}
