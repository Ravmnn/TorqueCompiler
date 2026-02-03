using System.Collections.Generic;

using LLVMSharp.Interop;


namespace Torque.Compiler.Types;




// TODO: this must not be inherit from BasePrimitiveType
// TODO: check if you can remove PrimitiveType.Struct
public class StructType(IReadOnlyList<BoundGenericDeclaration> members) : BasePrimitiveType(PrimitiveType.Struct)
{
    public IReadOnlyList<BoundGenericDeclaration> Members { get; } = members;




    public override LLVMTypeRef ToLLVMType()
    {
        throw new System.NotImplementedException();
    }
}
