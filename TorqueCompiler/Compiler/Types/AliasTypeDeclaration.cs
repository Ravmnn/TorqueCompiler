using Torque.Compiler.Symbols;


namespace Torque.Compiler.Types;




public class AliasTypeDeclaration(SymbolSyntax typeSymbol, TypeSyntax typeSyntax) : TypeDeclaration(typeSymbol)
{
    public TypeSyntax TypeSyntax { get; } = typeSyntax;




    public override TypeSyntax GetTypeSyntax()
        => TypeSyntax;
}
