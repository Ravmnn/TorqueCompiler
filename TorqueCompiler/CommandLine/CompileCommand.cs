#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.


using System.ComponentModel;
using System.IO;
using System.Threading;

using Spectre.Console;
using Spectre.Console.Cli;
using Torque.Compiler;
using Torque.Compiler.Target;


namespace Torque.CommandLine;




public class CompileCommandSettings : CommandSettings
{
    [CommandArgument(0, "<file>")]
    public FileInfo File { get; init; }




    [CommandOption("-I|--import-reference")]
    [Description("The directory the import system will use as reference")]
    public string? ImportReference { get; set; }




    [CommandOption("--target-arch")]
    [Description("The CPU architecture to generate instructions")]
    [DefaultValue(ArchitectureType.X86_64)]
    public ArchitectureType Architecture { get; init; }


    [CommandOption("--target-os")]
    [Description("The OS target type")]
    [DefaultValue(OperationalSystemType.Linux)]
    public OperationalSystemType OperationalSystem { get; init; }


    [CommandOption("--target-environment")]
    [Description("The environment target type")]
    [DefaultValue(EnvironmentType.GNU)]
    public EnvironmentType Environment { get; init; }


    [CommandOption("--target-vendor")]
    [Description("The vendor target type")]
    [DefaultValue(VendorType.PC)]
    public VendorType Vendor { get; init; }




    [CommandOption("-d|--debug")]
    [Description("Generate debug information")]
    public bool Debug { get; init; }


    [CommandOption("--pic")]
    [Description("Generate Position Independent Code")]
    [DefaultValue(true)]
    public bool PIC { get; init; }




    public override ValidationResult Validate()
    {
        if (!File.Exists)
            return ValidationResult.Error($"Could not open source file \"{File.Name}\"");

        return ValidationResult.Success();
    }
}




public class CompileCommand : Command<CompileCommandSettings>
{
    protected override int Execute(CommandContext context, CompileCommandSettings settings, CancellationToken cancellationToken)
    {
        Torque.Initialize(settings);
        Torque.Compile(settings);
        return 0;
    }
}




public static class CompileCommandSettingsExtensions
{
    public static CompilerOptions ToLowLevelOptions(this CompileCommandSettings settings)
        => new CompilerOptions
        {
            Debug = settings.Debug,
            PIC = settings.PIC,
            ImportReference = settings.ImportReference ?? settings.File.Directory!.FullName
        };
}