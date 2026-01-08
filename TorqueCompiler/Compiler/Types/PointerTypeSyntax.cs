namespace Torque.Compiler.Types;




public class PointerTypeSyntax(TypeSyntax innerType) : TypeSyntax
{
    public override BaseTypeSyntax BaseType => InnerType.BaseType;


    public TypeSyntax InnerType { get; } = innerType;




    public override string ToString()
        => $"{InnerType}*";
}
