using System.Collections.Generic;

using LLVMSharp.Interop;


namespace Torque.Compiler.Types;




public class StructType(IReadOnlyList<BoundGenericDeclaration> members) : BasePrimitiveType(PrimitiveType.Struct)
{
    public IReadOnlyList<BoundGenericDeclaration> Members { get; } = members;




    public override LLVMTypeRef ToLLVMType()
    {
        throw new System.NotImplementedException();
    }
}
