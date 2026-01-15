using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using LLVMSharp.Interop;

using Torque.Compiler.Symbols;
using Torque.Compiler.Tokens;
using Torque.Compiler.Types;


using Type = Torque.Compiler.Types.Type;


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
        AddDebugInfoVersion();

        InitializeDebugBuilder();


        Compiler.GlobalScope.DebugMetadata = File;
    }


    private void AddDebugInfoVersion()
        => Compiler.Module.AddModuleFlag("Debug Info Version", LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorWarning, LLVM.DebugMetadataVersion());


    private void InitializeDebugBuilder()
    {
        ThrowIfInvalidFileInfo();
        InitializeLLVMDebugProperties(Compiler.File!);
    }


    private void ThrowIfInvalidFileInfo()
    {
        if (Compiler.File is null || !Compiler.File.Exists)
            throw new InvalidOperationException("Debug metadata generator requires a valid compiler file info");
    }


    private unsafe void InitializeLLVMDebugProperties(FileInfo fileInfo)
    {
        var file = fileInfo.Name;
        var directoryPath = fileInfo.Directory!.FullName;

        DebugBuilder = LLVM.CreateDIBuilder(Module);
        File = DebugBuilder.CreateFile(file, directoryPath);
        CompileUnit = NewCompileUnit();
    }


    private LLVMMetadataRef NewCompileUnit()
        => DebugBuilder.CreateCompileUnit(
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




    public void FinalizeGenerator()
        => DebugBuilder.DIBuilderFinalize();




    public unsafe LLVMMetadataRef GenerateFunction(FunctionSymbol function)
    {
        var functionMetadata = CreateFunction(function);
        LLVM.SetSubprogram(function.LLVMReference!, functionMetadata);

        return functionMetadata;
    }


    private LLVMMetadataRef CreateFunction(FunctionSymbol function)
    {
        var debugFunctionType = CreateFunctionTypeMetadata(function.Type!);
        return CreateFunction(function.Name, function.Location.Line, debugFunctionType, !function.IsExternal);
    }


    private LLVMMetadataRef CreateFunction(string name, int lineNumber, LLVMMetadataRef debugFunctionType, bool isDefinition = true)
        => DebugBuilder.CreateFunction(
            Scope.DebugMetadata!.Value,
            name,
            name,
            File,
            (uint)lineNumber,
            debugFunctionType,
            0,
            isDefinition.BoolToInt(),
            (uint)lineNumber,
            LLVMDIFlags.LLVMDIFlagZero,
            0
        );




    public unsafe LLVMMetadataRef GenerateLocalVariable(VariableSymbol variable)
    {
        var typeMetadata = TypeToMetadata(variable.Type!);
        var sizeInBits = (uint)variable.Type!.SizeOfTypeInMemoryAsBits(TargetData);
        var llvmLocation = CreateDebugLocation(variable.Location);

        var debugReference = CreateAutoVariable(variable.Name, variable.Location.Line, typeMetadata, sizeInBits);
        DeclareLocalVariable(variable.LLVMReference!.Value, debugReference, llvmLocation);

        return debugReference;
    }


    private unsafe LLVMOpaqueMetadata* CreateAutoVariable(string name, int lineNumber, LLVMMetadataRef typeMetadata, uint sizeInBits)
        => LLVM.DIBuilderCreateAutoVariable(
            DebugBuilder, Scope.DebugMetadata!.Value, name.StringToSBytePtr(), (uint)name.Length, File,
            (uint)lineNumber, typeMetadata, 0, LLVMDIFlags.LLVMDIFlagZero, sizeInBits
        );




    public unsafe LLVMMetadataRef GenerateParameter(VariableSymbol parameter, int index)
    {
        var typeMetadata = TypeToMetadata(parameter.Type!);
        var llvmLocation = CreateDebugLocation(parameter.Location);

        var debugReference = CreateParameterVariable(parameter.Name, parameter.Location.Line, index, typeMetadata);
        DeclareLocalVariable(parameter.LLVMReference!.Value, debugReference, llvmLocation);

        return debugReference;
    }


    private unsafe LLVMOpaqueMetadata* CreateParameterVariable(string name, int lineNumber, int index, LLVMMetadataRef typeMetadata)
        => LLVM.DIBuilderCreateParameterVariable(
            DebugBuilder, Scope.DebugMetadata!.Value, name.StringToSBytePtr(), (uint)name.Length, (uint)index, File,
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




    public unsafe LLVMMetadataRef SetCurrentLocation(int line, int column)
    {
        column = Math.Max(1, column); // DWARF uses 1-based column indices

        var location = CreateDebugLocation(line, column);
        LLVM.SetCurrentDebugLocation2(Builder, location);

        return location;
    }


    public unsafe void SetCurrentLocation()
        => LLVM.SetCurrentDebugLocation2(Builder, null);


    public LLVMMetadataRef CreateDebugLocation(Span location)
        => CreateDebugLocation(location.Line, location.Start);


    public unsafe LLVMMetadataRef CreateDebugLocation(int line, int column)
        => LLVM.DIBuilderCreateDebugLocation(Module.Context, (uint)line, (uint)column, Scope.DebugMetadata!.Value, null);




    public unsafe LLVMMetadataRef CreateLexicalScope(int line, int column)
    {
        // this function assumes "TorqueCompiler.Scope" is the new scope to insert debug metadata

        var parentScope = Scope.Parent!.DebugMetadata!.Value;
        var scopeReference = LLVM.DIBuilderCreateLexicalBlock(DebugBuilder, parentScope, File, (uint)line, (uint)column);
        return scopeReference;
    }




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

        var sizeInBits = type.SizeOfTypeInMemoryAsBits(TargetData);
        var encoding = GetEncodingFromType(type);

        return TypeToMetadata(type, name, sizeInBits, encoding);
    }


    private LLVMMetadataRef TypeToMetadata(Type type, string name, int sizeInBits, int encoding)
        => type switch
        {
            BaseType => CreateBasicTypeMetadata(name, sizeInBits, encoding),

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



    private LLVMMetadataRef CreateFunctionTypeMetadata(FunctionType type)
    {
        var typeArray = CreateFunctionTypeArray(type);
        var metadataTypeArray = TypesToMetadataArray(typeArray);

        var debugFunctionType = CreateFunctionTypeMetadata(metadataTypeArray);

        return debugFunctionType;
    }


    private static IReadOnlyList<Type> CreateFunctionTypeArray(FunctionType type)
    {
        var typeArray = new Type[] { type.ReturnType };
        return typeArray.Concat(type.ParametersType).ToArray();
    }


    private LLVMMetadataRef CreateFunctionTypeMetadata(IReadOnlyList<LLVMMetadataRef> metadataTypeArray)
        => DebugBuilder.CreateSubroutineType(File, metadataTypeArray.ToArray(), LLVMDIFlags.LLVMDIFlagZero);


    public unsafe LLVMMetadataRef CreatePointerTypeMetadata(LLVMMetadataRef elementType, string name, int sizeInBits)
        => LLVM.DIBuilderCreatePointerType(DebugBuilder, elementType, (ulong)sizeInBits, (uint)sizeInBits, 0, name.StringToSBytePtr(), (uint)name.Length);


    public unsafe LLVMMetadataRef CreateBasicTypeMetadata(string name, int sizeInBits, int encoding)
        => LLVM.DIBuilderCreateBasicType(DebugBuilder, name.StringToSBytePtr(), (uint)name.Length, (ulong)sizeInBits, (uint)encoding, LLVMDIFlags.LLVMDIFlagZero);




    public static int GetEncodingFromType(Type type) => type.Base.Type switch
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
