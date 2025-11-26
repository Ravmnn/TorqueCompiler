using System.Linq;
using System.Runtime.InteropServices;

using LLVMSharp.Interop;


namespace Torque.Compiler;




public class DebugMetadataGenerator
{
    private LLVMDIBuilderRef _debugBuilder;
    private LLVMMetadataRef _file;
    private LLVMMetadataRef _compileUnit;

    private LLVMMetadataRef? _currentFunctionMetadata;


    public LLVMModuleRef Module { get; }
    public LLVMTargetDataRef TargetData { get; }




    public DebugMetadataGenerator(LLVMModuleRef module, LLVMTargetDataRef targetData)
    {
        Module = module;
        TargetData = targetData;

        Module.AddModuleFlag("Debug Info Version", LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorWarning, LLVM.DebugMetadataVersion());

        InitializeDebugBuilder();
    }


    private unsafe void InitializeDebugBuilder()
    {
        var file = Torque.Options.File.Name;
        var directoryPath = Torque.Options.File.Directory?.FullName ?? "/";

        _debugBuilder = LLVM.CreateDIBuilder(Module);
        _file = _debugBuilder.CreateFile(file, directoryPath);
        _compileUnit = _debugBuilder.CreateCompileUnit(
            LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageC,
            _file,
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




    public void FinalizeGenerator() => _debugBuilder.DIBuilderFinalize();




    public unsafe void SetLocation(LLVMValueRef instruction, int line, int column)
    {
        if (_currentFunctionMetadata is null)
            return;

        var location = LLVM.DIBuilderCreateDebugLocation(Module.Context, (uint)line, (uint)column, _currentFunctionMetadata, null);
        var valueLocation = LLVM.MetadataAsValue(Module.Context, location);

        var kind = LLVM.GetMDKindID((sbyte*)Marshal.StringToHGlobalAnsi("dbg"), 3);
        instruction.SetMetadata(kind, valueLocation);
    }




    public unsafe void GenerateFunction(LLVMValueRef function, string name, int lineNumber, PrimitiveType? returnType, PrimitiveType[] parametersType)
    {
        var typeArray = CreateFunctionPrimitiveTypeArray(returnType, parametersType);

        var metadataTypeArray = MetadataArrayToTypeArray(typeArray);

        var temp = new LLVMMetadataRef[typeArray.Length];

        for (var i = 0; i < typeArray.Length; i++)
            temp[i] = metadataTypeArray + i;

        // BUG: subroutine type is invalid, although it's being created

        var debugFunctionType = _debugBuilder.CreateSubroutineType(_file, temp, LLVMDIFlags.LLVMDIFlagZero);

        var functionMetadata = _debugBuilder.CreateFunction(_file, name, name, _file, (uint)lineNumber, debugFunctionType, 0, 1, (uint)lineNumber, LLVMDIFlags.LLVMDIFlagZero, 0);

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


    private unsafe LLVMOpaqueMetadata* MetadataArrayToTypeArray(PrimitiveType[] types)
    {
        var metadataArray = PrimitiveTypesToMetadataArray(types);

        return LLVM.DIBuilderGetOrCreateTypeArray(_debugBuilder, metadataArray, (uint)types.Length);
    }




    public unsafe LLVMOpaqueMetadata** PrimitiveTypesToMetadataArray(PrimitiveType[] types)
    {
        var opaqueMetadataArray = new LLVMOpaqueMetadata*[types.Length];

        for (var i = 0; i < types.Length; i++)
            opaqueMetadataArray[i] = PrimitiveTypeToMetadata(types[i]);

        fixed (LLVMOpaqueMetadata** ptr = opaqueMetadataArray)
            return ptr;
    }


    public unsafe LLVMOpaqueMetadata* PrimitiveTypeToMetadata(PrimitiveType type)
    {
        var name = Token.Primitives.First(primitive => primitive.Value == type).Key;
        var sbyteName = (sbyte*)Marshal.StringToHGlobalAnsi(name);
        var sizeInBits = type.SizeOfThis(TargetData) * 8;

        return LLVM.DIBuilderCreateBasicType(_debugBuilder, sbyteName, (uint)name.Length, (ulong)sizeInBits, 0, LLVMDIFlags.LLVMDIFlagZero);
    }
}
