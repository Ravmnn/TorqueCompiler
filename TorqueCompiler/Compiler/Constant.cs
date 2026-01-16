using LLVMSharp.Interop;

using Torque.Compiler.Types;
using Torque.Compiler.Target;


namespace Torque.Compiler;




public static class Constant
{
    public static LLVMTypeRef GenericPointerType { get; }
        = LLVMTypeRef.CreatePointer(LLVMTypeRef.CreateIntPtr(TargetMachine.Global!.DataLayout), 0);


    public static LLVMValueRef Zero { get; } = Integer(0);
    public static LLVMValueRef One { get; } = Integer(1);




    public static LLVMValueRef Integer(ulong value, LLVMTypeRef? type = null)
        => LLVMValueRef.CreateConstInt(type ?? LLVMTypeRef.Int32, value);

    public static LLVMValueRef Real(double value, LLVMTypeRef? type = null)
        => LLVMValueRef.CreateConstReal(type ?? LLVMTypeRef.Double, value);

    public static LLVMValueRef Boolean(bool value)
        => LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, value ? 1UL : 0UL);

    public static LLVMValueRef NullPointer(LLVMTypeRef? pointerType = null)
        => LLVMValueRef.CreateConstPointerNull(pointerType ?? GenericPointerType);




    public static LLVMValueRef GetDefaultValueForType(Type type) => type switch
    {
        _ when type.IsPointer => NullPointer(),
        _ when type.IsFloat => Real(0, type.ToLLVMType()),
        _ => Integer(0, type.ToLLVMType())
    };
}
