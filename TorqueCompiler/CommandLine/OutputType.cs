using System;


namespace Torque.CommandLine;




public enum OutputType
{
    Object,
    Assembly,
    BitCode
}




public static class OutputTypeExtensions
{
    public static OutputType StringToOutputType(this string source) => source switch
    {
        "object" => OutputType.Object,
        "assembly" => OutputType.Assembly,
        "bitcode" => OutputType.BitCode,

        _ => throw InvalidOutputType()
    };


    public static string OutputTypeToString(this OutputType type) => type switch
    {
        OutputType.Object => "object",
        OutputType.Assembly => "assembly",
        OutputType.BitCode => "bitcode",

        _ => throw InvalidOutputType()
    };




    public static string OutputTypeToFileExtension(this OutputType type) => type switch
    {
        OutputType.Object => "o",
        OutputType.Assembly => "asm",
        OutputType.BitCode => "bc",

        _ => throw InvalidOutputType()
    };




    private static ArgumentException InvalidOutputType()
        => new ArgumentException("Invalid output type.");
}
