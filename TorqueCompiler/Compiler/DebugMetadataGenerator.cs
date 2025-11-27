using System.Linq;
using System.Runtime.InteropServices;

using LLVMSharp.Interop;


namespace Torque.Compiler;




public class DebugMetadataGenerator
{
    private LLVMMetadataRef? _currentFunctionMetadata;

    public LLVMDIBuilderRef DebugBuilder;
    public LLVMMetadataRef File;
    public LLVMMetadataRef CompileUnit;


    public LLVMModuleRef Module { get; }
    public LLVMBuilderRef Builder { get; }
    public LLVMTargetDataRef TargetData { get; }




    public DebugMetadataGenerator(LLVMModuleRef module, LLVMBuilderRef builder, LLVMTargetDataRef targetData)
    {
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




    public unsafe void SetLocation(int line, int column)
    {
        if (_currentFunctionMetadata is null)
            return;

        var location = LLVM.DIBuilderCreateDebugLocation(Module.Context, (uint)line, (uint)column, _currentFunctionMetadata, null);
        LLVM.SetCurrentDebugLocation2(Builder, location);
    }




    public unsafe void GenerateFunction(LLVMValueRef function, string name, int lineNumber, PrimitiveType? returnType, PrimitiveType[] parametersType)
    {
        var typeArray = CreateFunctionPrimitiveTypeArray(returnType, parametersType);
        var metadataTypeArray = PrimitiveTypesToMetadataArray(typeArray);

        var debugFunctionType = DebugBuilder.CreateSubroutineType(File, metadataTypeArray, LLVMDIFlags.LLVMDIFlagZero);

        var functionMetadata = DebugBuilder.CreateFunction(File, name, name, File, (uint)lineNumber, debugFunctionType, 0, 1, (uint)lineNumber, LLVMDIFlags.LLVMDIFlagZero, 0);

        _currentFunctionMetadata = functionMetadata;

        LLVM.SetSubprogram(function, functionMetadata);
    }


    private PrimitiveType[] CreateFunctionPrimitiveTypeArray(PrimitiveType? returnType, PrimitiveType[] parametersType)
    {
        var length = (returnType is not null ? 1 : 0) + parametersType.Length;
        var types = new PrimitiveType[length];

        if (returnType is not null)
            types[0] = returnType.Value;

        return types.Concat(parametersType).ToArray();
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
        var sbyteName = (sbyte*)Marshal.StringToHGlobalAnsi(name);
        var sizeInBits = type.SizeOfThis(TargetData) * 8;

        return LLVM.DIBuilderCreateBasicType(DebugBuilder, sbyteName, (uint)name.Length, (ulong)sizeInBits, 0, LLVMDIFlags.LLVMDIFlagZero);
    }
}
