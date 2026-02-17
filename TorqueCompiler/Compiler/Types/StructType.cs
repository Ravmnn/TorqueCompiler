using System.Collections.Generic;
using System.Linq;

using LLVMSharp.Interop;

using Torque.Compiler.Symbols;


namespace Torque.Compiler.Types;




public class StructType(SymbolSyntax name, IReadOnlyList<BoundGenericDeclaration> members) : BasePrimitiveType(PrimitiveType.Struct)
{
    public SymbolSyntax Name { get; } = name;
    public IReadOnlyList<BoundGenericDeclaration> Members { get; } = members;




    public override unsafe LLVMTypeRef ToLLVMType()
    {
        var fieldTypes = (from member in Members select member.Type.ToLLVMType()).ToArray();

        fixed (LLVMOpaqueType** fieldOpaqueTypes = LLVMTypeRefsToOpaqueTypePointers(fieldTypes))
        {
            var structType = LLVM.StructCreateNamed(LLVM.GetGlobalContext(), Name.Name.StringToSBytePtr());
            LLVM.StructSetBody(structType, fieldOpaqueTypes, (uint)fieldTypes.Length, 0);

            return structType;
        }
    }


    private static unsafe LLVMOpaqueType*[] LLVMTypeRefsToOpaqueTypePointers(LLVMTypeRef[] fieldTypes)
    {
        var fieldOpaqueTypes = new LLVMOpaqueType*[fieldTypes.Length];

        for (var i = 0; i < fieldTypes.Length; i++)
            fieldOpaqueTypes[i] = fieldTypes[i];

        return fieldOpaqueTypes;
    }
}
