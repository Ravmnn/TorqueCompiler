using LLVMSharp.Interop;


namespace Torque.Compiler.Types;




public class BaseType(PrimitiveType type) : Type
{
    public override BaseType Base => this;

    public PrimitiveType Type { get; } = type;




    public override LLVMTypeRef ToLLVMType()
        => Type.PrimitiveTypeToLLVMType();
}
