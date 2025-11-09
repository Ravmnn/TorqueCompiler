using System.IO;


namespace TorqueCompiler;




public readonly struct TorqueOptions
{
    public static TorqueOptions Global { get; set; }


    public required FileInfo File { get; init; }
}
