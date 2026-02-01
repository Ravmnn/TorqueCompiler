using System.Collections.Generic;

using Torque.Compiler.Symbols;


namespace Torque.Compiler.Types;




public class StructTypeDeclaration(SymbolSyntax typeSymbol, IReadOnlyList<GenericDeclaration> members)
    : TypeDeclaration(typeSymbol)
{
    public IReadOnlyList<GenericDeclaration> Members { get; } = members;




    public override TypeSyntax GetTypeSyntax()
        => new StructTypeSyntax(Members);
}
