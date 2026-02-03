using Torque.Compiler.Symbols;
using Torque.Compiler.Tokens;


namespace Torque.Compiler.Types;




public abstract class TypeSyntax
{
    public abstract BaseTypeSyntax BaseType { get; }
    public SymbolSyntax SymbolSyntax => BaseType.TypeSymbol;




    private static PrimitiveType? PrimitiveTypeFromTokenOrNull(string token)
    {
        if (!Keywords.PrimitiveTypes.TryGetValue(token, out var primitiveType))
            return null;

        return primitiveType;
    }




    public override string ToString()
        => BaseType.TypeSymbol.Name;
}
