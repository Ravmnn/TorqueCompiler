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


    public const string FileExtension = ".tor";




    public static void Initialize(CompileCommandSettings settings)
    {
        LLVMInitalize.InitializeAll();

        s_compileSettings = settings;
        InitializeGlobalTargetMachine(settings);
    }


    private static void InitializeGlobalTargetMachine(CompileCommandSettings settings)
    {
        var targetTriple = TargetTriple.FromCompileSettings(settings);
        TargetMachine.SetGlobalTarget(targetTriple.ToString());
    }




    public static void Compile(CompileCommandSettings settings)
    {
        CompileFileAndImportsToObject(settings);
    }


    public static void CompileFileAndImportsToObject(CompileCommandSettings settings)
        => CompileFileAndImportsToObject(settings.File.FullName, settings.ToIRGenerationOptions());


    public static void CompileFileAndImportsToObject(string file, IRGenerationOptions options)
    {
        var fileInfo = new FileInfo(file);
        var fileSystem = new FileSystem(fileInfo);

        var (_, bitCode) = GenerateModuleIR(file, options, fileSystem);
        var outputFile = GetOutputFile(file, fileSystem.EntryDirectory, options.OutputDirectory);

        ProgramToolchain.Compile(bitCode, outputFile, options);
    }


    public static void CompileSingleModuleToObject(Module module, IRGenerationOptions options, FileSystem fileSystem)
    {
        var bitCode = CompilerSteps.Compile(module, options, fileSystem);

        var modulePath = module.FileInfo.FullName;
        var outputFile = GetOutputFile(modulePath, fileSystem.EntryDirectory, options.OutputDirectory);

        ProgramToolchain.Compile(bitCode, outputFile, options);
    }


    private static string GetOutputFile(string file, DirectoryInfo entryDirectory, DirectoryInfo? outputDirectory)
    {
        var root = entryDirectory.Parent!.FullName;
        var relativePath = Path.GetRelativePath(root, file);

        return Path.Combine(outputDirectory?.FullName ?? root, relativePath + ".o");
    }




    public static (Module module, string bitCode) GenerateModuleIR(string file, IRGenerationOptions options, FileSystem fileSystem)
    {
        var moduleLoader = new ModuleLoader(fileSystem);

        var (module, _) = moduleLoader.LoadModuleByPath(file);
        var bitCode = CompilerSteps.Compile(module!.Value, options, fileSystem);

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
