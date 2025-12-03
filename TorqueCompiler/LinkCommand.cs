#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.


using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

using Spectre.Console;
using Spectre.Console.Cli;


namespace Torque;




[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
public class LinkCommandSettings : CommandSettings
{
    [CommandArgument(0, "<files>")]
    public FileInfo[] Files { get; init; }



    [CommandOption("-o|--output")]
    [Description("The output file to store the final binary")]
    [DefaultValue("app")]
    public string Output { get; init; }



    [CommandOption("--debug")]
    [Description("Generate debug information")]
    public bool Debug { get; init; }




    public override ValidationResult Validate()
    {
        foreach (var file in Files)
            if (!file.Exists)
                return ValidationResult.Error($"Could not open source file \"{file.Name}\"");

        return ValidationResult.Success();
    }
}




public class LinkCommand : Command<LinkCommandSettings>
{
    protected override int Execute(CommandContext context, LinkCommandSettings settings, CancellationToken cancellationToken)
    {
        Torque.Link(settings);
        return 0;
    }
}
