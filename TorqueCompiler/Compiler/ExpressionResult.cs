using LLVMSharp.Interop;


namespace Torque.Compiler;




public struct ExpressionResult(LLVMValueRef value, LLVMTypeRef type, bool isAddress)
{
    public bool IsAddress { get; } = isAddress;
    public bool IsValue => !IsAddress;

    public LLVMValueRef Value { get; } = value;
    public LLVMTypeRef Type { get; } = type;
}
