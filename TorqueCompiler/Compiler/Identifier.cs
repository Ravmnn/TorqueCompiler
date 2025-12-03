using LLVMSharp.Interop;


namespace Torque.Compiler;




public interface IIdentifier
{
    public string Name { get; }
}


public readonly record struct CompilerIdentifier(LLVMValueRef Address, LLVMTypeRef Type, LLVMMetadataRef? DebugReference = null)
    : IIdentifier
{
    public string Name => Address.Name;
}
