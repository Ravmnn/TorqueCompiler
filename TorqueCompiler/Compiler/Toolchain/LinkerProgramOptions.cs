namespace Torque.Compiler.Toolchain;




public record struct LinkerProgramOptions()
{
    public bool Debug { get; set; }
    public bool PIE { get; set; }
}
