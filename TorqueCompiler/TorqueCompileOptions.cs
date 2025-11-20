using System;
using System.IO;


namespace Torque;




public enum OutputType
{
    Object,
    Assembly,
    BitCode
}




public readonly struct TorqueCompileOptions
{
    public required FileInfo File { get; init; }

    public required string? Output { get; init; }

    public required OutputType OutputType { get; init; }

    public required bool PrintAST { get; init; }
    public required bool PrintLLVM { get; init; }
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
