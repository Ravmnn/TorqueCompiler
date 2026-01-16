using System;
using System.Diagnostics;


namespace Torque.CommandLine;




public enum OutputType
{
    Object,
    Assembly,
    BitCode
}




public static class OutputTypeExtensions
{
    public static string OutputTypeToFileExtension(this OutputType type) => type switch
    {
        OutputType.Object => "o",
        OutputType.Assembly => "asm",
        OutputType.BitCode => "bc",

        _ => throw new UnreachableException()
    };
}
