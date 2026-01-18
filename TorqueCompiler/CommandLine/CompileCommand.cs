#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

using Spectre.Console;
using Spectre.Console.Cli;

using Torque.Compiler.Target;


namespace Torque.CommandLine;




[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
public class CompileCommandSettings : CommandSettings
{
    [CommandArgument(0, "<file>")]
    public FileInfo File { get; init; }




    [CommandOption("-o|--output")]
    [Description("The output file to store the compiling result")]
    public string? Output { get; init; }


    [CommandOption("-O|--output-type")]
    [Description("The kind of output the compiler should generate")]
    [DefaultValue(OutputType.Object)]
    public OutputType OutputType { get; init; }




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




    [CommandOption("--print-ast")]
    [Description("Print Abstract Syntactic Tree and quit")]
    public bool PrintAST { get; init; }


    [CommandOption("--print-llvm")]
    [Description("Print LLVM bit code and quit")]
    public bool PrintLLVM { get; init; }


    [CommandOption("--print-asm")]
    [Description("Print Assembly and quit")]
    public bool PrintASM { get; init; }




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
        Torque.Compile(settings);
        return 0;
    }
}
