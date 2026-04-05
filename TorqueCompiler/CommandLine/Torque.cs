// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable LocalizableElement


using System.Linq;

using Torque.Compiler;
using Torque.Compiler.Target;
using Torque.CommandLine.Commands;
using Torque.Compiler.Toolchain;


namespace Torque.CommandLine;




public static class Torque
{
    public const string FileExtension = ".tor";




    public static void Initialize(CompileCommandSettings settings)
    {
        LLVMInitalize.InitializeAll();

        InitializeGlobalTargetMachine(settings);
    }


    private static void InitializeGlobalTargetMachine(CompileCommandSettings settings)
    {
        var targetTriple = TargetTriple.FromCompileSettings(settings);
        TargetMachine.SetGlobalTarget(targetTriple.ToString());
    }




    public static void Compile(CompileCommandSettings settings)
    {
        var entry = new EntryInfo(settings.File);
        var moduleLoader = new ModuleLoader(entry);

        var (module, _) = moduleLoader.LoadModuleByPath(settings.File.FullName);

        CompilerSteps.Compile(module!, settings.ToIRGenerationOptions(), entry);
    }








    public static void Link(LinkCommandSettings settings)
    {
        var fileNames = settings.Files.Select(file => file.FullName).ToArray();
        var options = settings.LinkerOptionsFromLinkCommandSettings();

        ProgramToolchain.Link(fileNames, settings.Output, options);
    }
}
