using System;
using System.Collections.Generic;
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
    private readonly Stack<LLVMMetadataRef> _scopes;


    public LLVMDIBuilderRef DebugBuilder;
    public LLVMMetadataRef File;
    public LLVMMetadataRef CompileUnit;


    public LLVMModuleRef Module { get; }
    public LLVMBuilderRef Builder { get; }
    public LLVMTargetDataRef TargetData { get; }




    public DebugMetadataGenerator(LLVMModuleRef module, LLVMBuilderRef builder, LLVMTargetDataRef targetData)
    {
        _scopes = [];
        _scopes.Push(File);


        Module = module;
        Builder = builder;
        TargetData = targetData;

        Module.AddModuleFlag("Debug Info Version", LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorWarning, LLVM.DebugMetadataVersion());

        InitializeDebugBuilder();
    }


    private unsafe void InitializeDebugBuilder()
    {
        var file = Torque.Options.File.Name;
        var directoryPath = Torque.Options.File.Directory?.FullName ?? "/";

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
        var location = CreateDebugLocation(line, column);
        LLVM.SetCurrentDebugLocation2(Builder, location);

        return location;
    }


    private unsafe LLVMMetadataRef CreateDebugLocation(int line, int column)
        => LLVM.DIBuilderCreateDebugLocation(Module.Context, (uint)line, (uint)column, CurrentScope(), null);




    public void ScopeEnter(int line, int column)
        => _scopes.Push(CreateScope(line, column));


    public void ScopeEnterFunction(LLVMMetadataRef function)
        => _scopes.Push(function);


    public void ScopeExit()
    {
        if (_scopes.Count <= 1)
            throw new InvalidOperationException("Internal debug scope stack must have at least one item");

        _scopes.Pop();
    }


    private unsafe LLVMMetadataRef CreateScope(int line, int column)
        => LLVM.DIBuilderCreateLexicalBlock(DebugBuilder, CurrentScope(), File, (uint)line, (uint)column);


    private LLVMMetadataRef CurrentScope() => _scopes.Peek();




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
            CurrentScope(), name, name, File, (uint)lineNumber, debugFunctionType, 0, 1,
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
        var sbyteName = StringToSBytePtr(name);

        var variable = LLVM.DIBuilderCreateAutoVariable(
            DebugBuilder, CurrentScope(), sbyteName, (uint)name.Length, File,
            (uint)lineNumber, typeMetadata, 0, LLVMDIFlags.LLVMDIFlagZero, sizeInBits
        );

        DeclareLocalVariable(alloca, variable, location);
        UpdateLocalVariableValue(alloca, variable, location);

        return variable;
    }


    private unsafe void DeclareLocalVariable(LLVMValueRef alloca, LLVMMetadataRef variable, LLVMMetadataRef location)
        => LLVM.DIBuilderInsertDeclareRecordAtEnd(DebugBuilder, alloca, variable, EmptyExpression(), location, Builder.InsertBlock);


    private unsafe void UpdateLocalVariableValue(LLVMValueRef alloca, LLVMMetadataRef variable, LLVMMetadataRef location)
        => LLVM.DIBuilderInsertDbgValueRecordAtEnd(DebugBuilder, alloca, variable, EmptyExpression(), location, Builder.InsertBlock);


    private unsafe LLVMMetadataRef EmptyExpression()
        => LLVM.DIBuilderCreateExpression(DebugBuilder, null, 0);


    // TODO: when this language support variable reassignment, use LLVM.DIBuilderInsertDbgValueAtEnd so the debugger can follow the variable value




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
