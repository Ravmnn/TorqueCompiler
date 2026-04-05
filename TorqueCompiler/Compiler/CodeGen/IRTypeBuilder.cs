using System.Collections.Generic;
using System.Linq;

using LLVMSharp.Interop;

using Torque.Compiler.Types;


namespace Torque.Compiler.CodeGen;




public class IRTypeBuilder : ITypeProcessor<LLVMTypeRef>
{
    private readonly Dictionary<string, LLVMTypeRef> _structCache = [];




    public LLVMTypeRef Process(Type type)
        => type.Process(this);


    public IReadOnlyCollection<LLVMTypeRef> ProcessAll(IReadOnlyCollection<Type> types)
        => types.Select(Process).ToArray();




    public LLVMTypeRef ProcessPrimitive(BasePrimitiveType type)
    {
        return type.Type.PrimitiveTypeToLLVMType();
    }




    public LLVMTypeRef ProcessPointer(PointerType type)
    {
        var innerType = Process(type.InnerType);
        return LLVMTypeRef.CreatePointer(innerType, 0);
    }




    public LLVMTypeRef ProcessArray(ArrayType type)
    {
        var innerType = Process(type.InnerType);
        return LLVMTypeRef.CreateArray2(innerType, type.Length);
    }




    public LLVMTypeRef ProcessFunction(FunctionType type)
        => LLVMTypeRef.CreatePointer(ProcessRawFunction(type), 0);


    public LLVMTypeRef ProcessRawFunction(FunctionType type)
    {
        var llvmReturnType = Process(type.ReturnType);
        var llvmParametersType = ProcessAll(type.ParametersType);
        var llvmFunctionType = LLVMTypeRef.CreateFunction(llvmReturnType, llvmParametersType.ToArray());

        return llvmFunctionType;
    }




    public LLVMTypeRef ProcessStruct(StructType type)
    {
        if (_structCache.TryGetValue(type.Name.Name, out var cachedType))
            return cachedType;

        return CreateAndCacheLLVMStruct(type);
    }


    private unsafe LLVMTypeRef CreateAndCacheLLVMStruct(StructType type)
    {
        var name = type.Name.Name;
        var llvmType = LLVM.StructCreateNamed(LLVM.GetGlobalContext(), name.StringToSBytePtr());

        _structCache.TryAdd(name, llvmType);

        fixed (LLVMOpaqueType** fieldsOpaqueTypes = GetStructFieldsOpaqueTypes(type))
            LLVM.StructSetBody(llvmType, fieldsOpaqueTypes, (uint)type.Members.Count, 0);

        return llvmType;
    }


    private unsafe LLVMOpaqueType*[] GetStructFieldsOpaqueTypes(StructType type)
    {
        var types = new LLVMOpaqueType*[type.Members.Count];

        for (var i = 0; i < type.Members.Count; i++)
            types[i] = Process(type.Members[i].Type);

        return types;
    }




    public int SizeOfTypeInMemoryAsBits(Type type, LLVMTargetDataRef? targetData = null)
        => SizeOfTypeInMemory(type, targetData) * 8;


    public int SizeOfTypeInMemory(Type type, LLVMTargetDataRef? targetData = null) => type switch
    {
        _ when type.IsVoid => 0,

        _ => Process(type).SizeOfThisInMemory(targetData)
    };




    public int AlignmentOfTypeInMemoryAsBits(Type type, LLVMTargetDataRef? targetData = null)
        => AlignmentOfTypeInMemory(type, targetData) * 8;


    public int AlignmentOfTypeInMemory(Type type, LLVMTargetDataRef? targetData = null) => type switch
    {
        _ when type.IsVoid => 0,

        _ => Process(type).AlignmentOfThisInMemory(targetData)
    };
}
