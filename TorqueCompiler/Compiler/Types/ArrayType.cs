using LLVMSharp.Interop;


namespace Torque.Compiler.Types;




// arrays are represented by a pointer to the first element,
// the only exception being with an "ArrayExpression", which uses
// the fixed array type from LLVM to allocate the memory for it.
// In other words, this type must not be available for the programmer
public class ArrayType(Type type, ulong length) : PointerType(type)
{
    public ulong Length { get; } = length;




    public override LLVMTypeRef ToLLVMType()
        => LLVMTypeRef.CreateArray2(Type.ToLLVMType(), Length);
}
