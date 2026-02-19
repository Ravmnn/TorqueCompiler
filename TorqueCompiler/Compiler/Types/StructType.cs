using System.Collections.Generic;

using Torque.Compiler.Symbols;


namespace Torque.Compiler.Types;




public class StructType(SymbolSyntax name, IReadOnlyList<BoundGenericDeclaration> members) : BasePrimitiveType(PrimitiveType.Struct)
{
    public SymbolSyntax Name { get; } = name;
    public IReadOnlyList<BoundGenericDeclaration> Members { get; } = members;




    public override T Process<T>(ITypeProcessor<T> processor)
        => processor.ProcessStruct(this);
}
