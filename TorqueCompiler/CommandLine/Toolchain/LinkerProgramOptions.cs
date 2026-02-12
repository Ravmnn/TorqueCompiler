namespace Torque.CommandLine.Toolchain;




public record struct LinkerProgramOptions()
{
    public bool Debug { get; set; }
    public bool PIE { get; set; }




    public static LinkerProgramOptions FromLinkCommandSettings(LinkCommandSettings settings)
        => new LinkerProgramOptions
        {
            Debug = settings.Debug,
            PIE = settings.PIE
        };
}
