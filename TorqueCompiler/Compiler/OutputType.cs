using System.Diagnostics;


namespace Torque.Compiler;




public enum OutputType
{
    Object,
    Assembly
}




public static class OutputTypeExtensions
{
    public static string OutputTypeToFileExtension(this OutputType type) => type switch
    {
        OutputType.Object => "o",
        OutputType.Assembly => "asm",

        _ => throw new UnreachableException()
    };
}
