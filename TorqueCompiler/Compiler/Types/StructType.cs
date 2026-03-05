using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Symbols;


namespace Torque.Compiler.Types;




public class StructType(SymbolSyntax name, IReadOnlyList<BoundGenericDeclaration> members) : BasePrimitiveType(PrimitiveType.Struct)
{
    public SymbolSyntax Name { get; } = name;
    public IList<BoundGenericDeclaration> Members { get; set; } = members.ToList();




    public override T Process<T>(ITypeProcessor<T> processor)
        => processor.ProcessStruct(this);




    public (BoundGenericDeclaration member, int index)? GetField(string name)
    {
        for (var i = 0; i < Members.Count; i++)
            if (Members[i].Name.Name == name)
                return (Members[i], i);

        return null;
    }
}
