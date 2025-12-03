using System;
using System.Linq;
using System.Runtime.InteropServices;

using LLVMSharp.Interop;


namespace Torque.Compiler;




public static class DebugMetadataTypeEncodings
{
    public const int Void = 0;
    public const int Boolean = 2;
    public const int Float = 4;
    public const int Signed = 5;
    public const int Unsigned = 7;
    public const int UnsignedChar = 8;
    public const int UTF = 16;
}




public class DebugMetadataGenerator
{
    public LLVMDIBuilderRef DebugBuilder;
    public LLVMMetadataRef File;
    public LLVMMetadataRef CompileUnit;


   public TorqueCompiler Compiler { get; }

   public LLVMModuleRef Module => Compiler.Module;
   public LLVMBuilderRef Builder => Compiler.Builder;
   public LLVMTargetDataRef TargetData => Compiler.TargetData;

   public Scope<CompilerIdentifier> Scope => Compiler.Scope;




    public DebugMetadataGenerator(TorqueCompiler compiler)
    {
        Compiler = compiler;


        Compiler.Module.AddModuleFlag("Debug Info Version", LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorWarning, LLVM.DebugMetadataVersion());

        InitializeDebugBuilder();


        if (!Scope.IsGlobal)
            throw new InvalidOperationException("Debug metadata generator must be initialized at the global scope.");

        Scope.DebugReference = File;
    }


    private unsafe void InitializeDebugBuilder()
    {
        if (Compiler.FileInfo is null)
            throw new InvalidOperationException("Debug metadata generator requires a compiler file info");

        var file = Compiler.FileInfo.Name;
        var directoryPath = Compiler.FileInfo.Directory?.FullName ?? "/";

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




    public void FinalizeGenerator() => DebugBuilder.DIBuilderFinalize();




    public unsafe LLVMMetadataRef SetLocation(int line, int column)
    {
        column = Math.Max(1, column);

        var location = CreateDebugLocation(line, column);
        LLVM.SetCurrentDebugLocation2(Builder, location);

        return location;
    }


    public unsafe void SetLocation()
        => LLVM.SetCurrentDebugLocation2(Builder, null);


    public unsafe LLVMMetadataRef CreateDebugLocation(int line, int column)
        => LLVM.DIBuilderCreateDebugLocation(Module.Context, (uint)line, (uint)column, Scope.DebugReference!, null);


    public unsafe LLVMMetadataRef CreateLexicalScope(int line, int column)
    {
        var scopeReference = LLVM.DIBuilderCreateLexicalBlock(DebugBuilder, Scope.DebugReference!, File, (uint)line, (uint)column);
        return scopeReference;
    }




    public unsafe LLVMMetadataRef GenerateFunction(LLVMValueRef function, string name, int lineNumber, PrimitiveType? returnType, PrimitiveType[] parametersType)
    {
        var typeArray = CreateFunctionPrimitiveTypeArray(returnType, parametersType);
        var metadataTypeArray = PrimitiveTypesToMetadataArray(typeArray);

        var debugFunctionType = CreateSubroutineType(metadataTypeArray);
        var functionMetadata = CreateFunction(name, lineNumber, debugFunctionType);

        LLVM.SetSubprogram(function, functionMetadata);

        return functionMetadata;
    }


    private LLVMMetadataRef CreateSubroutineType(LLVMMetadataRef[] metadataTypeArray)
        => DebugBuilder.CreateSubroutineType(File, metadataTypeArray, LLVMDIFlags.LLVMDIFlagZero);


    private LLVMMetadataRef CreateFunction(string name, int lineNumber, LLVMMetadataRef debugFunctionType)
        => DebugBuilder.CreateFunction(
            Scope.DebugReference!.Value, name, name, File, (uint)lineNumber, debugFunctionType, 0, 1,
            (uint)lineNumber, LLVMDIFlags.LLVMDIFlagZero, 0
        );


    private PrimitiveType[] CreateFunctionPrimitiveTypeArray(PrimitiveType? returnType, PrimitiveType[] parametersType)
    {
        var length = (returnType is not null ? 1 : 0) + parametersType.Length;
        var types = new PrimitiveType[length];

        if (returnType is not null)
            types[0] = returnType.Value;

        return types.Concat(parametersType).ToArray();
    }




    public unsafe LLVMMetadataRef GenerateLocalVariable(string name, PrimitiveType type, int lineNumber, LLVMValueRef alloca, LLVMMetadataRef location)
    {
        var typeMetadata = PrimitiveTypeToMetadata(type);
        var sizeInBits = (uint)type.SizeOfThis(TargetData) * 8;

        var debugReference = CreateAutoVariable(name, lineNumber, typeMetadata, sizeInBits);

        DeclareLocalVariable(alloca, debugReference, location);

        return debugReference;
    }


    private unsafe LLVMOpaqueMetadata* CreateAutoVariable(string name, int lineNumber, LLVMMetadataRef typeMetadata, uint sizeInBits)
        => LLVM.DIBuilderCreateAutoVariable(
            DebugBuilder, Scope.DebugReference!, StringToSBytePtr(name), (uint)name.Length, File,
            (uint)lineNumber, typeMetadata, 0, LLVMDIFlags.LLVMDIFlagZero, sizeInBits
        );


    private unsafe LLVMDbgRecordRef DeclareLocalVariable(LLVMValueRef alloca, LLVMMetadataRef variable, LLVMMetadataRef location)
        => LLVM.DIBuilderInsertDeclareRecordAtEnd(DebugBuilder, alloca, variable, EmptyExpression(), location, Builder.InsertBlock);


    private unsafe LLVMDbgRecordRef UpdateLocalVariableValue(LLVMValueRef alloca, LLVMMetadataRef variable, LLVMMetadataRef location)
        => LLVM.DIBuilderInsertDbgValueRecordAtEnd(DebugBuilder, alloca, variable, EmptyExpression(), location, Builder.InsertBlock);


    private unsafe LLVMMetadataRef EmptyExpression()
        => LLVM.DIBuilderCreateExpression(DebugBuilder, null, 0);




    public LLVMDbgRecordRef UpdateLocalVariableValue(string name, LLVMMetadataRef location)
    {
        var variable = Scope.GetIdentifier(name);
        return UpdateLocalVariableValue(variable.Address, variable.DebugReference!.Value, location);
    }




    public LLVMMetadataRef[] PrimitiveTypesToMetadataArray(PrimitiveType[] types)
    {
        var metadataArray = new LLVMMetadataRef[types.Length];

        for (var i = 0; i < types.Length; i++)
            metadataArray[i] = PrimitiveTypeToMetadata(types[i]);

        return metadataArray;
    }


    public unsafe LLVMMetadataRef PrimitiveTypeToMetadata(PrimitiveType type)
    {
        var name = Token.Primitives.First(primitive => primitive.Value == type).Key;
        var sbyteName = StringToSBytePtr(name);
        var sizeInBits = type.SizeOfThis(TargetData) * 8;
        var encoding = GetEncodingFromPrimitive(type);

        return LLVM.DIBuilderCreateBasicType(DebugBuilder, sbyteName, (uint)name.Length, (ulong)sizeInBits, (uint)encoding, LLVMDIFlags.LLVMDIFlagZero);
    }


    private int GetEncodingFromPrimitive(PrimitiveType type) => type switch
    {
        PrimitiveType.Bool => DebugMetadataTypeEncodings.Boolean,
        PrimitiveType.Char => DebugMetadataTypeEncodings.UnsignedChar,
        PrimitiveType.UInt8 or PrimitiveType.UInt16 or PrimitiveType.UInt32 or PrimitiveType.UInt64 => DebugMetadataTypeEncodings.Unsigned,
        PrimitiveType.Int8 or PrimitiveType.Int16 or PrimitiveType.Int32 or PrimitiveType.Int64 => DebugMetadataTypeEncodings.Signed,

        _ => 0
    };




    private unsafe sbyte* StringToSBytePtr(string source)
        => (sbyte*)Marshal.StringToHGlobalAnsi(source);
}
