namespace Torque.Compiler.Types;




public class PointerType(Type type) : Type
{
    public override BasePrimitiveType BasePrimitive => InnerType.BasePrimitive;

    public override Type InnerType { get; } = type;




    public override T Process<T>(ITypeProcessor<T> processor)
        => processor.ProcessPointer(this);




    protected override bool Equals(Type other)
    {
        if (other is not PointerType otherType)
            return false;

        return InnerType == otherType.InnerType;
    }




    public override string ToString() => $"{InnerType}*";
}
