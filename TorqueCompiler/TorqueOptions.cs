using System.IO;


namespace Torque;




public readonly struct TorqueOptions
{
    public required FileInfo File { get; init; }

    public required string Output { get; init; }

    public required bool PrintAST { get; init; }
    public required bool PrintLLVM { get; init; }
}
