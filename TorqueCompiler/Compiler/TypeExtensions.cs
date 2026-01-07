using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using LLVMSharp.Interop;


namespace Torque.Compiler;




public static class TypeExtensions
{
    public static IReadOnlyList<LLVMTypeRef> TypesToLLVMTypes(this IReadOnlyList<Type> types)
        => types.Select(TypeToLLVMType).ToArray();


    public static LLVMTypeRef TypeToLLVMType(this Type type) => type switch
    {
        BaseType baseType => baseType.Type.PrimitiveToLLVMType(),

        FunctionType functionType => FunctionTypeToLLVMType(functionType),
        ArrayType arrayType => ArrayTypeToLLVMType(arrayType),
        PointerType pointerType => PointerTypeToLLVMType(pointerType),

        _ => throw new UnreachableException()
    };


    public static LLVMTypeRef PointerTypeToLLVMType(this PointerType pointerType)
        => LLVMTypeRef.CreatePointer(pointerType.Type.TypeToLLVMType(), 0);


    public static LLVMTypeRef ArrayTypeToLLVMType(this ArrayType arrayType)
        => LLVMTypeRef.CreateArray2(arrayType.Type.TypeToLLVMType(), arrayType.Size);


    public static LLVMTypeRef FunctionTypeToLLVMType(this FunctionType functionType, bool pointer = true)
    {
        var llvmReturnType = functionType.ReturnType.TypeToLLVMType();
        var llvmParametersType = functionType.ParametersType.TypesToLLVMTypes();
        var llvmFunctionType = LLVMTypeRef.CreateFunction(llvmReturnType, llvmParametersType.ToArray());

        // "pointer" determines whether we want a pointer to the function: ptr to "i32 (i32, i32)"
        // (this is used by a variable to store the function)
        // or the function type itself: "i32 (i32, i32)"
        // (this is used by LLVM everytime we want to do any operation with that function, like creating one,
        // calling one... This works that way now because LLVM uses only opaque pointers, so we have to
        // store the real type of the pointer somewhere)

        return pointer ? LLVMTypeRef.CreatePointer(llvmFunctionType, 0) : llvmFunctionType;
    }




    public static int SizeOfThisInMemory(this Type type, LLVMTargetDataRef? targetData = null) => type switch
    {
        _ when type.IsVoid => 0,

        _ => (int)TargetMachine.GetDataLayoutOfOrGlobal(targetData).ABISizeOfType(type.TypeToLLVMType())
    };


    public static int SizeOfThisInMemoryAsBits(this Type type, LLVMTargetDataRef? targetData = null)
        => type.SizeOfThisInMemory(targetData) * 8;
}
