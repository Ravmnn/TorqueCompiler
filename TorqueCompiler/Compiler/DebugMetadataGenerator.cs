using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using LLVMSharp.Interop;


namespace Torque.Compiler;




public static class DebugMetadataTypeEncodings
{
    public const int Void = 0;
    public const int Address = 1;
    public const int Boolean = 2;
    public const int Float = 4;
    public const int SignedInt = 5;
    public const int UnsignedInt = 7;
    public const int SignedChar = 6;
    public const int UnsignedChar = 8;
    public const int UTF = 16;
    public const int ASCII = 18;
}




public class DebugMetadataGenerator
{
    public LLVMDIBuilderRef DebugBuilder;
    public LLVMMetadataRef File;
    public LLVMMetadataRef CompileUnit;


   public TorqueCompiler Compiler { get; }

   public LLVMModuleRef Module => Compiler.Module;
   public LLVMBuilderRef Builder => Compiler.Builder;
   public LLVMTargetDataRef TargetData => Compiler.DataLayout;


   public Scope GlobalScope => Compiler.GlobalScope;
   public Scope Scope => Compiler.Scope;




    public DebugMetadataGenerator(TorqueCompiler compiler)
    {
        Compiler = compiler;
        Compiler.Module.AddModuleFlag("Debug Info Version", LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorWarning, LLVM.DebugMetadataVersion());

        InitializeDebugBuilder();


        Compiler.GlobalScope.DebugMetadata = File;
    }


    private unsafe void InitializeDebugBuilder()
    {
        ThrowIfInvalidFileInfo();

        var file = Compiler.File!.Name;
        var directoryPath = Compiler.File.Directory?.FullName ?? "/";

        DebugBuilder = LLVM.CreateDIBuilder(Module);
        File = DebugBuilder.CreateFile(file, directoryPath);
        CompileUnit = DebugBuilder.CreateCompileUnit(
            LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageC,
            File,
            "Torque Compiler dev",
            0,
            null!,
            0,
            null!,
            LLVMDWARFEmissionKind.LLVMDWARFEmissionFull,
            0,
            0,
            0,
            null!,
            null!
        );
    }


    private void ThrowIfInvalidFileInfo()
    {
        if (Compiler.File is null || !Compiler.File.Exists)
            throw new InvalidOperationException("Debug metadata generator requires a valid compiler file info");
    }




    public void FinalizeGenerator()
        => DebugBuilder.DIBuilderFinalize();




    public unsafe LLVMMetadataRef SetLocation(int line, int column)
    {
        column = Math.Max(1, column); // LLVM uses 1-based column indices

        var location = CreateDebugLocation(line, column);
        LLVM.SetCurrentDebugLocation2(Builder, location);

        return location;
    }


    public unsafe void SetLocation()
        => LLVM.SetCurrentDebugLocation2(Builder, null);


    public unsafe LLVMMetadataRef CreateDebugLocation(int line, int column)
        => LLVM.DIBuilderCreateDebugLocation(Module.Context, (uint)line, (uint)column, Scope.DebugMetadata!.Value, null);


    public unsafe LLVMMetadataRef CreateLexicalScope(int line, int column)
    {
        // this function assumes "TorqueCompiler.Scope" is the new scope to insert debug metadata

        var parentScope = Scope.Parent!.DebugMetadata!.Value;
        var scopeReference = LLVM.DIBuilderCreateLexicalBlock(DebugBuilder, parentScope, File, (uint)line, (uint)column);
        return scopeReference;
    }




    public unsafe LLVMMetadataRef GenerateFunction(LLVMValueRef function, string name, int lineNumber, FunctionType type)
    {
        var typeArray = CreateFunctionTypeArray(type);
        var metadataTypeArray = TypesToMetadataArray(typeArray);

        var debugFunctionType = CreateSubroutineType(metadataTypeArray);
        var functionMetadata = CreateFunction(name, lineNumber, debugFunctionType);

        LLVM.SetSubprogram(function, functionMetadata);

        return functionMetadata;
    }


    private LLVMMetadataRef CreateSubroutineType(IReadOnlyList<LLVMMetadataRef> metadataTypeArray)
        => DebugBuilder.CreateSubroutineType(File, metadataTypeArray.ToArray(), LLVMDIFlags.LLVMDIFlagZero);


    private LLVMMetadataRef CreateFunction(string name, int lineNumber, LLVMMetadataRef debugFunctionType)
        => DebugBuilder.CreateFunction(
            Scope.DebugMetadata!.Value, name, name, File, (uint)lineNumber, debugFunctionType, 0, 1,
            (uint)lineNumber, LLVMDIFlags.LLVMDIFlagZero, 0
        );


    private static IReadOnlyList<Type> CreateFunctionTypeArray(FunctionType type)
    {
        var typeArray = new Type[] { type.ReturnType };
        return typeArray.Concat(type.ParametersType).ToArray();
    }




    public unsafe LLVMMetadataRef GenerateLocalVariable(string name, Type type, int lineNumber, LLVMValueRef alloca, LLVMMetadataRef location)
    {
        const int BitsInOneByte = 8;

        var typeMetadata = TypeToMetadata(type);
        var sizeInBits = (uint)type.SizeOfThisInMemory(TargetData) * BitsInOneByte;

        var debugReference = CreateAutoVariable(name, lineNumber, typeMetadata, sizeInBits);
        DeclareLocalVariable(alloca, debugReference, location);

        return debugReference;
    }


    private unsafe LLVMOpaqueMetadata* CreateAutoVariable(string name, int lineNumber, LLVMMetadataRef typeMetadata, uint sizeInBits)
        => LLVM.DIBuilderCreateAutoVariable(
            DebugBuilder, Scope.DebugMetadata!.Value, StringToSBytePtr(name), (uint)name.Length, File,
            (uint)lineNumber, typeMetadata, 0, LLVMDIFlags.LLVMDIFlagZero, sizeInBits
        );




    public unsafe LLVMMetadataRef GenerateParameter(string name, Type type, int lineNumber, int index, LLVMValueRef alloca, LLVMMetadataRef location)
    {
        var typeMetadata = TypeToMetadata(type);

        var debugReference = CreateParameterVariable(name, lineNumber, index, typeMetadata);
        DeclareLocalVariable(alloca, debugReference, location);

        return debugReference;
    }


    private unsafe LLVMOpaqueMetadata* CreateParameterVariable(string name, int lineNumber, int index, LLVMMetadataRef typeMetadata)
        => LLVM.DIBuilderCreateParameterVariable(
            DebugBuilder, Scope.DebugMetadata!.Value, StringToSBytePtr(name), (uint)name.Length, (uint)index, File,
            (uint)lineNumber, typeMetadata, 0, LLVMDIFlags.LLVMDIFlagZero
        );




    private unsafe LLVMDbgRecordRef DeclareLocalVariable(LLVMValueRef alloca, LLVMMetadataRef variable, LLVMMetadataRef location)
        => LLVM.DIBuilderInsertDeclareRecordAtEnd(DebugBuilder, alloca, variable, EmptyExpression(), location, Builder.InsertBlock);


    private unsafe LLVMDbgRecordRef UpdateLocalVariableValue(LLVMValueRef alloca, LLVMMetadataRef variable, LLVMMetadataRef location)
        => LLVM.DIBuilderInsertDbgValueRecordAtEnd(DebugBuilder, alloca, variable, EmptyExpression(), location, Builder.InsertBlock);


    private unsafe LLVMMetadataRef EmptyExpression()
        => LLVM.DIBuilderCreateExpression(DebugBuilder, null, 0);




    public LLVMDbgRecordRef UpdateLocalVariableValue(string name, LLVMMetadataRef location)
    {
        var variable = Scope.GetSymbol(name);
        return UpdateLocalVariableValue(variable.LLVMReference!.Value, variable.LLVMDebugMetadata!.Value, location);
    }


    public LLVMDbgRecordRef UpdateLocalVariableValue(LLVMValueRef reference, LLVMMetadataRef location)
    {
        var variable = Scope.GetSymbol(reference);
        return UpdateLocalVariableValue(reference, variable.LLVMDebugMetadata!.Value, location);
    }




    public IReadOnlyList<LLVMMetadataRef> TypesToMetadataArray(IReadOnlyList<Type> types)
    {
        var metadataArray = new LLVMMetadataRef[types.Count];

        for (var i = 0; i < types.Count; i++)
            metadataArray[i] = TypeToMetadata(types[i]);

        return metadataArray;
    }


    // TODO: create PointerType specific metadata
    public unsafe LLVMMetadataRef TypeToMetadata(Type type)
    {
        var name = type.ToString();

        var sbyteName = StringToSBytePtr(name);
        var sizeInBits = type.SizeOfThisInMemory(TargetData) * 8;
        var encoding = GetEncodingFromType(type);

        return BasicTypeMetadata(sbyteName, name.Length, sizeInBits, encoding);
    }


    private unsafe LLVMMetadataRef BasicTypeMetadata(sbyte* sbyteName, int nameLength, int sizeInBits, int encoding)
        => LLVM.DIBuilderCreateBasicType(DebugBuilder, sbyteName, (uint)nameLength, (ulong)sizeInBits, (uint)encoding, LLVMDIFlags.LLVMDIFlagZero);


    private static int GetEncodingFromType(Type type) => type.Base.Type switch
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




    private static unsafe sbyte* StringToSBytePtr(string source)
        => (sbyte*)Marshal.StringToHGlobalAnsi(source);
}
