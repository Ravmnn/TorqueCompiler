using LLVMSharp.Interop;


namespace Torque.Compiler.Types;




public class PointerType(Type type) : Type
{
    public override BaseType Base => Type.Base;

    public Type Type { get; } = type;




    public override LLVMTypeRef ToLLVMType()
        => LLVMTypeRef.CreatePointer(Type.ToLLVMType(), 0);




    protected override bool Equals(Type other)
    {
        if (other is not PointerType otherType)
            return false;

        return Type == otherType.Type;
    }




    public override string ToString() => $"{Type}*";
}
