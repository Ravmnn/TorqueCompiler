namespace Torque.Compiler.Types;




public class PointerTypeSyntax(TypeSyntax innerType) : TypeSyntax
{
    public override BaseTypeSyntax BaseType => InnerType.BaseType;


    public TypeSyntax InnerType { get; set; } = innerType;




    public override T Process<T>(ITypeSyntaxProcessor<T> processor)
        => processor.ProcessPointer(this);




    public override string ToString()
        => $"{InnerType}*";
}
