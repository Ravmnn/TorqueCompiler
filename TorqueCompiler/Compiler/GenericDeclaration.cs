using Torque.Compiler.Symbols;
using Torque.Compiler.Types;


namespace Torque.Compiler;




public readonly record struct GenericDeclaration(TypeSyntax Type, SymbolSyntax Name)
{
    public override string ToString()
        => $"{Type} {Name}";
}


public readonly record struct BoundGenericDeclaration(Type Type, SymbolSyntax Name)
{
    public override string ToString()
        => $"{Type} {Name}";
}
