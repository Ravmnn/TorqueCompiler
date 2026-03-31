using Torque.Compiler.Symbols;


namespace Torque.Compiler.Types;




public abstract class TypeDeclaration(SymbolSyntax typeSymbol)
{
    public SymbolSyntax TypeSymbol { get; } = typeSymbol;

    public abstract TypeSyntax TypeSyntax { get; set; }
    public abstract Type? Type { get; set; }
}
