using Torque.Compiler.Symbols;


namespace Torque.Compiler.Types;




public abstract class TypeSyntax
{
    public abstract BaseTypeSyntax BaseType { get; }
    public SymbolSyntax SymbolSyntax => BaseType.TypeSymbol;




    public override string ToString()
        => BaseType.TypeSymbol.Name;
}
