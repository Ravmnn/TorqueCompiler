using System.Collections.Generic;

using Torque.Compiler.Symbols;


namespace Torque.Compiler.Types;




public class StructTypeSyntax(SymbolSyntax name, IReadOnlyList<GenericDeclaration> members) : BaseTypeSyntax(name)
{
    public SymbolSyntax Name { get; } = name;
    public IReadOnlyList<GenericDeclaration> Members { get; } = members;
}
