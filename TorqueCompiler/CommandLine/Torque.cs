// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable LocalizableElement


using System.IO;
using System.Linq;

using Torque.Compiler;
using Torque.Compiler.Target;
using Torque.Compiler.CodeGen;
using Torque.CommandLine.Toolchain;
using Torque.CommandLine.Commands;


namespace Torque.CommandLine;




public static class Torque
{
    private static CompileCommandSettings s_compileSettings = null!;
    private static LinkCommandSettings s_linkSettings = null!;


    public static DiagnosticLogger Logger { get; set; } = new DiagnosticLogger();


    public const string FileExtension = ".tor";




    public static void Initialize(CompileCommandSettings settings)
    {
        LLVMInitalize.InitializeAll();

        s_compileSettings = settings;
        InitializeGlobals(settings);
    }


    private static void InitializeGlobals(CompileCommandSettings settings)
    {
        SourceCode.SetCurrentWorkingFileTo(settings.File);
        InitializeGlobalTargetMachine(settings);
    }


    private static void InitializeGlobalTargetMachine(CompileCommandSettings settings)
    {
        var targetTriple = TargetTriple.FromCompileSettings(settings);
        TargetMachine.SetGlobalTarget(targetTriple.ToString());
    }




    public static void Compile(CompileCommandSettings settings)
    {
        CompileFileToObject(settings);
    }


    public static void CompileFileToObject(CompileCommandSettings settings)
        => CompileFileToObject(settings.File.FullName, settings.ToIRGenerationOptions());


    public static void CompileFileToObject(string file, IRGenerationOptions options)
    {
        var (_, bitCode) = GenerateModuleIR(file, options);
        var outputFile = GetOutputFile(file, options.OutputDirectory);

        ProgramToolchain.Compile(bitCode, outputFile, options);
    }


    public static void CompileModuleToObject(Module module, IRGenerationOptions options)
    {
        var bitCode = CompilerSteps.Compile(module, options);
        var outputFile = GetOutputFile(module.FileInfo.FullName, options.OutputDirectory);

        ProgramToolchain.Compile(bitCode, outputFile, options);
    }


    private static string GetOutputFile(string file, DirectoryInfo? outputDirectory)
    {
        var root = SourceCode.EntryDirectory!.Parent!.FullName;
        var relativePath = Path.GetRelativePath(root, file);

        return Path.Combine(outputDirectory?.FullName ?? root, relativePath + ".o");
    }




    public static (Module module, string bitCode) GenerateModuleIR(string file, IRGenerationOptions options)
    {
        var (module, _) = ModuleLoader.LoadModule(file);
        var bitCode = CompilerSteps.Compile(module!.Value, options);

        return (module.Value, bitCode);
    }








    public static void Link(LinkCommandSettings settings)
    {
        s_linkSettings = settings;

        var fileNames = settings.Files.Select(file => file.FullName).ToArray();
        var options = LinkerProgramOptions.FromLinkCommandSettings(settings);

        ProgramToolchain.Link(fileNames, settings.Output, options);
    }
}
