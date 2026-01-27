using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using LLVMSharp.Interop;

using Torque.Compiler.Types;


namespace Torque.Compiler;




public class DebugTypeMetadataGenerator(LLVMDIBuilderRef debugBuilder, LLVMMetadataRef file)
{
    // Debug metadata generation methods require the coupled "DebugBuilder" instance.
    // That dependency makes the use of polymorphism not viable, since it would require the objects
    // to have an accessible reference to the debug builder somewhere. Injecting the dependency inside
    // the object is possible, but a bit ugly and weird. And creating a static global instance is definetely
    // a bad idea. As consequence, the procedural approach is chosen to solve the problem.
    // It's a bit inconsistent though, since the "Type" object uses the polymorphism approach
    // to convert itself to a valid LLVM's representation.

    public LLVMDIBuilderRef DebugBuilder { get; } = debugBuilder;
    public LLVMMetadataRef File { get; } = file;




    public IReadOnlyList<LLVMMetadataRef> TypesToMetadataArray(IReadOnlyList<Type> types)
    {
        var metadataArray = new LLVMMetadataRef[types.Count];

        for (var i = 0; i < types.Count; i++)
            metadataArray[i] = TypeToMetadata(types[i]);

        return metadataArray;
    }


    public LLVMMetadataRef TypeToMetadata(Type type)
    {
        var name = type.ToString();

        var sizeInBits = type.SizeOfTypeInMemoryAsBits();
        var encoding = GetEncodingFromType(type);

        return TypeToMetadata(type, name, sizeInBits, encoding);
    }


    private LLVMMetadataRef TypeToMetadata(Type type, string name, int sizeInBits, int encoding)
        => type switch
        {
            // Procedural approach. See the top of the class for detailed explanation.

            BasePrimitiveType => CreateBasicTypeMetadata(name, sizeInBits, encoding),

            FunctionType functionType => CreatePointerToFunctionTypeMetadata(functionType),
            PointerType pointerType => CreatePointerTypeMetadata(TypeToMetadata(pointerType.Type), name, sizeInBits),

            _ => throw new UnreachableException()
        };


    private LLVMMetadataRef CreatePointerToFunctionTypeMetadata(FunctionType type)
    {
        var pointerToFunctionType = new PointerType(type);
        var functionTypeMetadata = CreateFunctionTypeMetadata(type);

        var name = pointerToFunctionType.ToString();
        var sizeInBits = pointerToFunctionType.SizeOfTypeInMemoryAsBits();

        return CreatePointerTypeMetadata(functionTypeMetadata, name, sizeInBits);
    }



    public LLVMMetadataRef CreateFunctionTypeMetadata(FunctionType type)
    {
        var typeArray = CreateFunctionTypeArray(type);
        var metadataTypeArray = TypesToMetadataArray(typeArray);

        var debugFunctionType = CreateFunctionTypeMetadata(metadataTypeArray);

        return debugFunctionType;
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
