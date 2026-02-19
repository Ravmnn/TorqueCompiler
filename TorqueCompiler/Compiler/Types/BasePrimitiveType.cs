using LLVMSharp.Interop;


namespace Torque.Compiler.Types;




public class BasePrimitiveType(PrimitiveType type) : Type
{
    public override BasePrimitiveType BasePrimitive => this;

    public PrimitiveType Type { get; } = type;




    public override T Process<T>(ITypeProcessor<T> processor)
        => processor.ProcessPrimitive(this);
}
