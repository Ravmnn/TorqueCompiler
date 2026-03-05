using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using LLVMSharp.Interop;
using Torque.Compiler.Target;
using Torque.Compiler.Types;


namespace Torque.Compiler;




public class DebugTypeMetadataGenerator(TorqueCompiler compiler, LLVMDIBuilderRef debugBuilder, LLVMMetadataRef file, LLVMMetadataRef compileUnit)
{
    private readonly Dictionary<string, LLVMMetadataRef> _structCache = [];


    public TorqueCompiler Compiler { get; } = compiler;

    public Scope GlobalScope => Compiler.GlobalScope;
    public Scope Scope => Compiler.Scope;

    public TypeBuilder TypeBuilder => Compiler.TypeBuilder;


    public LLVMDIBuilderRef DebugBuilder { get; } = debugBuilder;
    public LLVMMetadataRef File { get; } = file;
    public LLVMMetadataRef CompileUnit { get; } = compileUnit;




    public IReadOnlyList<LLVMMetadataRef> TypesToMetadataArray(IReadOnlyList<Type> types)
    {
        var metadataArray = new LLVMMetadataRef[types.Count];

        for (var i = 0; i < types.Count; i++)
            metadataArray[i] = TypeToMetadata(types[i]);

        return metadataArray;
    }


    public unsafe LLVMOpaqueMetadata*[] TypesToMetadataArrayPointer(IReadOnlyList<Type> types)
    {
        var llvmTypes = TypesToMetadataArray(types).ToArray();
        var llvmPointerTypes = new LLVMOpaqueMetadata*[llvmTypes.Length];

        for (var i = 0; i < llvmTypes.Length; i++)
            llvmPointerTypes[i] = llvmTypes[i];

        return llvmPointerTypes;
    }


    public LLVMMetadataRef TypeToMetadata(Type type)
    {
        var name = type.ToString();

        var sizeInBits = TypeBuilder.SizeOfTypeInMemoryAsBits(type);
        var encoding = GetEncodingFromType(type);

        return TypeToMetadata(type, name, sizeInBits, encoding);
    }


    private LLVMMetadataRef TypeToMetadata(Type type, string name, int sizeInBits, int encoding)
        => type switch
        {
            FunctionType functionType => CreatePointerToFunctionTypeMetadata(functionType),
            PointerType pointerType => CreatePointerTypeMetadata(TypeToMetadata(pointerType.InnerType), name, sizeInBits),
            StructType structType => CreateStructTypeMetadata(structType),

            BasePrimitiveType => CreateBasicTypeMetadata(name, sizeInBits, encoding),

            _ => throw new UnreachableException()
        };


    public LLVMMetadataRef CreateFunctionTypeMetadata(FunctionType type)
    {
        var typeArray = CreateFunctionTypeArray(type);
        var metadataTypeArray = TypesToMetadataArray(typeArray);

        var debugFunctionType = CreateFunctionTypeMetadata(metadataTypeArray);

        return debugFunctionType;
    }


    private LLVMMetadataRef CreatePointerToFunctionTypeMetadata(FunctionType type)
    {
        var pointerToFunctionType = new PointerType(type);
        var functionTypeMetadata = CreateFunctionTypeMetadata(type);

        var name = pointerToFunctionType.ToString();
        var sizeInBits = TypeBuilder.SizeOfTypeInMemoryAsBits(pointerToFunctionType);

        return CreatePointerTypeMetadata(functionTypeMetadata, name, sizeInBits);
    }


    public static IReadOnlyList<Type> CreateFunctionTypeArray(FunctionType type)
    {
        var typeArray = new Type[] { type.ReturnType };
        return typeArray.Concat(type.ParametersType).ToArray();
    }


    public LLVMMetadataRef CreateFunctionTypeMetadata(IReadOnlyList<LLVMMetadataRef> metadataTypeArray)
        => DebugBuilder.CreateSubroutineType(File, metadataTypeArray.ToArray(), LLVMDIFlags.LLVMDIFlagZero);


    public unsafe LLVMMetadataRef CreatePointerTypeMetadata(LLVMMetadataRef elementType, string name, int sizeInBits)
        => LLVM.DIBuilderCreatePointerType(
            DebugBuilder,
            elementType,
            (ulong)sizeInBits,
            (uint)sizeInBits,
            0,
            name.StringToSBytePtr(),
            (uint)name.Length
        );




    public unsafe LLVMMetadataRef CreateStructTypeMetadata(StructType type)
    {
        if (_structCache.ContainsKey(type.Name.Name))
            return _structCache.FirstOrDefault(pair => pair.Key == type.Name.Name).Value;

        var llvmType = Compiler.TypeBuilder.Process(type);

        var sizeInBits = llvmType.SizeOfThisInMemoryAsBits();
        var alignmentInBits = llvmType.AlignmentOfThisInMemoryAsBits();

        var name = type.Name.Name;
        var line = type.Name.Location.Line;
        var membersCount = (uint)type.Members.Count;

        // TODO: only use File as scope for functions

        var tempMetadata = CreateTempStructTypeMetadata(line, name, sizeInBits, alignmentInBits, membersCount);
        _structCache.Add(type.Name.Name, tempMetadata);

        fixed (LLVMOpaqueMetadata** members = CreateStructMembersMetadata(type))
        {
            var metadata = CreateStructTypeMetadata(line, name, sizeInBits, alignmentInBits, members, membersCount);
            _structCache[type.Name.Name] = metadata;

            LLVM.MetadataReplaceAllUsesWith(tempMetadata, metadata);

            return metadata;
        }
    }

    private unsafe LLVMOpaqueMetadata*[] CreateStructMembersMetadata(StructType type)
    {
        var members = new LLVMOpaqueMetadata*[type.Members.Count];
        var llvmType = TypeBuilder.Process(type);

        for (var i = 0; i < type.Members.Count; i++)
            members[i] = CreateStructFieldMetadata(type.Members[i], i, llvmType);

        return members;
    }


    private unsafe LLVMOpaqueMetadata* CreateStructFieldMetadata(BoundGenericDeclaration field, int i, LLVMTypeRef llvmType)
    {
        var sizeInBits = TypeBuilder.SizeOfTypeInMemoryAsBits(field.Type);
        var alignmentInBits = TypeBuilder.AlignmentOfTypeInMemoryAsBits(field.Type);
        var offsetInBits = TargetMachine.Global!.DataLayout.OffsetOfElement(llvmType, (uint)i) * 8;

        var name = field.Name.Name;
        var typeMetadata = TypeToMetadata(field.Type);

        return CreateStructFieldMetadata(field.Name.Location.Line, name, sizeInBits, alignmentInBits, offsetInBits, typeMetadata);
    }


    private unsafe LLVMOpaqueMetadata* CreateStructFieldMetadata(int line, string name, int sizeInBits, int alignmentInBits, ulong offsetInBits, LLVMMetadataRef typeMetadata)
        => LLVM.DIBuilderCreateMemberType(
            DebugBuilder,
            CompileUnit,
            name.StringToSBytePtr(),
            (uint)name.Length,
            File,
            (uint)line,
            (uint)sizeInBits,
            (uint)alignmentInBits,
            (uint)offsetInBits,
            LLVMDIFlags.LLVMDIFlagZero,
            typeMetadata
        );


    private unsafe LLVMMetadataRef CreateTempStructTypeMetadata(int line, string name, int sizeInBits, int alignmentInBits, uint membersCount)
        => LLVM.DIBuilderCreateReplaceableCompositeType(
            DebugBuilder,
            (uint)LLVMMetadataKind.LLVMDICompositeTypeMetadataKind,
            name.StringToSBytePtr(),
            (uint)name.Length,
            File,
            File,
            (uint)line,
            0,
            (uint)sizeInBits,
            (uint)alignmentInBits,
            LLVMDIFlags.LLVMDIFlagZero,
            name.StringToSBytePtr(),
            (uint)name.Length
        );


    private unsafe LLVMMetadataRef CreateStructTypeMetadata(int line, string name, int sizeInBits, int alignmentInBits, LLVMOpaqueMetadata** fields, uint membersCount)
        => LLVM.DIBuilderCreateStructType(
            DebugBuilder,
            File,
            name.StringToSBytePtr(),
            (uint)name.Length,
            File,
            (uint)line,
            (uint)sizeInBits,
            (uint)alignmentInBits,
            LLVMDIFlags.LLVMDIFlagZero,
            null,
            fields,
            membersCount,
            0,
            null,
            name.StringToSBytePtr(),
            (uint)name.Length
        );




    public unsafe LLVMMetadataRef CreateBasicTypeMetadata(string name, int sizeInBits, int encoding)
        => LLVM.DIBuilderCreateBasicType(
            DebugBuilder,
            name.StringToSBytePtr(),
            (uint)name.Length,
            (ulong)sizeInBits,
            (uint)encoding,
            LLVMDIFlags.LLVMDIFlagZero
        );




    public static int GetEncodingFromType(Type type) => type.BasePrimitive.Type switch
    {
        _ when type.IsPointer || type.IsFunction => DebugMetadataTypeEncodings.Address,

        PrimitiveType.PtrSize => DebugMetadataTypeEncodings.UnsignedInt,

        PrimitiveType.Bool => DebugMetadataTypeEncodings.Boolean,
        PrimitiveType.Char => DebugMetadataTypeEncodings.UnsignedChar,
        PrimitiveType.UInt8 or PrimitiveType.UInt16 or PrimitiveType.UInt32 or PrimitiveType.UInt64 => DebugMetadataTypeEncodings.UnsignedInt,
        PrimitiveType.Int8 or PrimitiveType.Int16 or PrimitiveType.Int32 or PrimitiveType.Int64 => DebugMetadataTypeEncodings.SignedInt,
        PrimitiveType.Float16 or PrimitiveType.Float32 or PrimitiveType.Float64 => DebugMetadataTypeEncodings.Float,

        _ => DebugMetadataTypeEncodings.Void
    };
}
