using System.Collections.Generic;
using System.Linq;

using LLVMSharp.Interop;


namespace Torque.Compiler.Types;




public static class TypeExtensions
{
    public static IReadOnlyList<LLVMTypeRef> TypesToLLVMTypes(this IReadOnlyList<Type> types)
        => types.Select(type => type.ToLLVMType()).ToArray();




    public static int SizeOfTypeInMemoryAsBits(this Type type, LLVMTargetDataRef? targetData = null)
        => type.SizeOfTypeInMemory(targetData) * 8;


    public static int SizeOfTypeInMemory(this Type type, LLVMTargetDataRef? targetData = null) => type switch
    {
        _ when type.IsVoid => 0,

        _ => type.ToLLVMType().SizeOfThisInMemory(targetData)
    };
}
