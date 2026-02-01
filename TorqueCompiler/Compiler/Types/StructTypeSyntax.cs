using System.Collections.Generic;


namespace Torque.Compiler.Types;




public class StructTypeSyntax(IReadOnlyList<GenericDeclaration> members) : TypeSyntax
{
    public override BaseTypeSyntax BaseType => null!;


    public IReadOnlyList<GenericDeclaration> Members { get; } = members;
}
