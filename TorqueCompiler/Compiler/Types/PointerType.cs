using LLVMSharp.Interop;


namespace Torque.Compiler.Types;




public class PointerType(Type type) : Type
{
    public override BasePrimitiveType BasePrimitive => Type.BasePrimitive;

    public Type Type { get; } = type;




    public override T Process<T>(ITypeProcessor<T> processor)
        => processor.ProcessPointer(this);




    protected override bool Equals(Type other)
    {
        if (other is not PointerType otherType)
            return false;

        return Type == otherType.Type;
    }




    public override string ToString() => $"{Type}*";
}
