using Torque.Compiler.Symbols;


namespace Torque.Compiler.Types;




public abstract class TypeDeclaration(SymbolSyntax typeSymbol) : IName
{
    public SymbolSyntax TypeSymbol { get; } = typeSymbol;

    public abstract TypeSyntax TypeSyntax { get; set; }
    public abstract Type? Type { get; set; }

    public string Name => TypeSymbol.Name;
}
