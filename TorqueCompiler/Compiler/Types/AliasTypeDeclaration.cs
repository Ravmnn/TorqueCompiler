using Torque.Compiler.Symbols;


namespace Torque.Compiler.Types;




public class AliasTypeDeclaration(SymbolSyntax typeSymbol, TypeSyntax typeSyntax)
    : TypeDeclaration(typeSymbol)
{
    public override TypeSyntax TypeSyntax { get; set; } = typeSyntax;
    public override Type? Type { get; set; }
}
