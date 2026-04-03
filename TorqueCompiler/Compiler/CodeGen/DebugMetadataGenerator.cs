using System;
using System.IO;

using LLVMSharp.Interop;

using Torque.Compiler.Tokens;
using Torque.Compiler.Symbols;


namespace Torque.Compiler.CodeGen;




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
    public LLVMDIBuilderRef DebugBuilder { get; private set; }
    public LLVMMetadataRef File { get; private set; }
    public LLVMMetadataRef CompileUnit { get; private set; }


    public IRGenerator Compiler { get; }

    public LLVMModuleRef Module => Compiler.LLVMModule;
    public LLVMBuilderRef Builder => Compiler.Builder;
    public LLVMTargetDataRef TargetData => Compiler.DataLayout;

    public Scope GlobalScope => Compiler.GlobalScope;
    public Scope Scope => Compiler.Scope;

    public IRTypeBuilder TypeBuilder => Compiler.TypeBuilder;


    public DebugTypeMetadataGenerator TypeGenerator { get; }




    public DebugMetadataGenerator(IRGenerator compiler)
    {
        Compiler = compiler;
        AddDebugInfoVersion();

        InitializeDebugBuilder();

        Compiler.GlobalScope.DebugMetadata = File;

        TypeGenerator = new DebugTypeMetadataGenerator(Compiler, DebugBuilder, File, CompileUnit);
    }


    public DebugMetadataGenerator(DebugMetadataGenerator generator, IRGenerator compiler)
    {
        DebugBuilder = generator.DebugBuilder;
        File = generator.File;
        CompileUnit = generator.CompileUnit;

        var fileInfo = new FileInfo(compiler.Module.Path);
        InitializeFileAndCompileUnit(fileInfo);

        Compiler = compiler;
        Compiler.GlobalScope.DebugMetadata = File;

        TypeGenerator = new DebugTypeMetadataGenerator(this, generator.TypeGenerator, compiler);
    }


    private void AddDebugInfoVersion()
        => Module.AddModuleFlag("Debug Info Version", LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorWarning, LLVM.DebugMetadataVersion());


    private void InitializeDebugBuilder()
    {
        ThrowIfInvalidFileInfo();
        InitializeLLVMDebugProperties(Compiler.File);
    }


    private void ThrowIfInvalidFileInfo()
    {
        if (Compiler.File is null || !Compiler.File.Exists)
            throw new InvalidOperationException("Debug metadata generator requires a valid compiler file info");
    }


    private unsafe void InitializeLLVMDebugProperties(FileInfo fileInfo)
    {
        DebugBuilder = LLVM.CreateDIBuilder(Module);
        InitializeFileAndCompileUnit(fileInfo);
    }


    private void InitializeFileAndCompileUnit(FileInfo fileInfo)
    {
        var file = fileInfo.Name;
        var directoryPath = fileInfo.Directory!.FullName;

        File = DebugBuilder.CreateFile(file, directoryPath);
        CompileUnit = CreateDefaultCompileUnit();
    }


    private LLVMMetadataRef CreateDefaultCompileUnit()
        => DebugBuilder.CreateCompileUnit(
            LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageC,
            File,
            "Torque Compiler",
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
        var debugFunctionType = TypeGenerator.CreateFunctionTypeMetadata(function.Type);
        return CreateFunction(function.Name, function.Location.Line, debugFunctionType, !function.IsExternal);
    }


    private LLVMMetadataRef CreateFunction(string name, int lineNumber, LLVMMetadataRef debugFunctionType, bool isDefinition = true)
        => DebugBuilder.CreateFunction(
            CompileUnit,
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
        var typeMetadata = TypeGenerator.TypeToMetadata(variable.Type!);
        var sizeInBits = (uint)TypeBuilder.SizeOfTypeInMemoryAsBits(variable.Type!, TargetData);
        var llvmLocation = CreateDebugLocation(variable.Location);

        var debugReference = CreateLocalVariable(variable.Name, variable.Location.Line, typeMetadata, sizeInBits);
        DeclareLocalVariable(variable.LLVMReference!.Value, debugReference, llvmLocation);

        return debugReference;
    }


    private unsafe LLVMOpaqueMetadata* CreateLocalVariable(string name, int lineNumber, LLVMMetadataRef typeMetadata, uint sizeInBits)
        => LLVM.DIBuilderCreateAutoVariable(
            DebugBuilder,
            Scope.DebugMetadata!.Value,
            name.StringToSBytePtr(),
            (uint)name.Length,
            File,
            (uint)lineNumber,
            typeMetadata,
            0,
            LLVMDIFlags.LLVMDIFlagZero,
            sizeInBits
        );




    public unsafe LLVMMetadataRef GenerateParameter(VariableSymbol parameter, int index)
    {
        var typeMetadata = TypeGenerator.TypeToMetadata(parameter.Type!);
        var llvmLocation = CreateDebugLocation(parameter.Location);

        var debugReference = CreateParameterVariable(parameter.Name, parameter.Location.Line, index, typeMetadata);
        DeclareLocalVariable(parameter.LLVMReference!.Value, debugReference, llvmLocation);

        return debugReference;
    }


    private unsafe LLVMOpaqueMetadata* CreateParameterVariable(string name, int lineNumber, int index, LLVMMetadataRef typeMetadata)
        => LLVM.DIBuilderCreateParameterVariable(
            DebugBuilder,
            Scope.DebugMetadata!.Value,
            name.StringToSBytePtr(),
            (uint)name.Length,
            (uint)index,
            File,
            (uint)lineNumber,
            typeMetadata,
            0,
            LLVMDIFlags.LLVMDIFlagZero
        );




    private unsafe LLVMDbgRecordRef DeclareLocalVariable(LLVMValueRef alloca, LLVMMetadataRef variable, LLVMMetadataRef location)
        => LLVM.DIBuilderInsertDeclareRecordAtEnd(
            DebugBuilder,
            alloca,
            variable,
            EmptyExpression(),
            location,
            LLVM.GetInstructionParent(alloca)
        );


    private unsafe LLVMMetadataRef EmptyExpression()
        => LLVM.DIBuilderCreateExpression(DebugBuilder, null, 0);




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
        => LLVM.DIBuilderCreateDebugLocation(
            Module.Context,
            (uint)line,
            (uint)column,
            Scope.DebugMetadata!.Value,
            null
        );




    public unsafe LLVMMetadataRef CreateLexicalScope(int line, int column)
    {
        // This function assumes "TorqueCompiler.Scope" is the new scope to insert debug metadata.

        var parentScope = Scope.Parent!.DebugMetadata!.Value;
        var scopeReference = LLVM.DIBuilderCreateLexicalBlock(DebugBuilder, parentScope, File, (uint)line, (uint)column);
        return scopeReference;
    }
}
