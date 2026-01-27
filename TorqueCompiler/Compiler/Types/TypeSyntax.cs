using Torque.Compiler.Symbols;
using Torque.Compiler.Tokens;


namespace Torque.Compiler.Types;




public abstract class TypeSyntax
{
    public abstract BaseTypeSyntax BaseType { get; }
    public SymbolSyntax SymbolSyntax => BaseType.TypeSymbol;


    public bool IsAuto => PrimitiveTypeFromTokenOrNull(BaseType.TypeSymbol.Name) == PrimitiveType.Auto;
    public bool IsVoid => PrimitiveTypeFromTokenOrNull(BaseType.TypeSymbol.Name) == PrimitiveType.Void;
    public bool IsBase => this is BaseTypeSyntax;
    public bool IsPointer => this is PointerTypeSyntax;
    public bool IsFunction => this is FunctionTypeSyntax;




    private static PrimitiveType? PrimitiveTypeFromTokenOrNull(string token)
    {
        if (!Keywords.PrimitiveTypes.TryGetValue(token, out var primitiveType))
            return null;

        return primitiveType;
    }




    public override string ToString()
        => BaseType.TypeSymbol.Name;
}
