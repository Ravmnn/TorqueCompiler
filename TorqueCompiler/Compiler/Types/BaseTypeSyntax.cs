using Torque.Compiler.Symbols;


namespace Torque.Compiler.Types;




public class BaseTypeSyntax(SymbolSyntax typeSymbol) : TypeSyntax
{
    public override BaseTypeSyntax BaseType => this;


    public SymbolSyntax TypeSymbol { get; } = typeSymbol;
}
