namespace Torque.Compiler;




public record struct CompilerOptions()
{
    public bool Debug { get; set; }
    public bool PIC { get; set; }

    public string ImportReference { get; set; } = string.Empty;
}
