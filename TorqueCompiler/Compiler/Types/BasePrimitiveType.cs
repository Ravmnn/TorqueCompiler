using LLVMSharp.Interop;


namespace Torque.Compiler.Types;




public class BasePrimitiveType(PrimitiveType type) : Type
{
    public override BasePrimitiveType BasePrimitive => this;

    public PrimitiveType Type { get; } = type;




    public override LLVMTypeRef ToLLVMType()
        => Type.PrimitiveTypeToLLVMType();
}
