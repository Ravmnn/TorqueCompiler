using Torque.Compiler.Tokens;


namespace Torque.Compiler.Types;




public abstract class TypeSyntax
{
    public abstract BaseTypeSyntax BaseType { get; }


    public bool IsAuto => Keywords.PrimitiveTypes[BaseType.TypeSymbol.Name] == PrimitiveType.Auto;
    public bool IsVoid => Keywords.PrimitiveTypes[BaseType.TypeSymbol.Name] == PrimitiveType.Void;
    public bool IsBase => this is BaseTypeSyntax;
    public bool IsPointer => this is PointerTypeSyntax;
    public bool IsFunction => this is FunctionTypeSyntax;




    public override string ToString()
        => BaseType.TypeSymbol.Name;
}
