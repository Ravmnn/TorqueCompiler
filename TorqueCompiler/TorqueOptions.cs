using System.IO;

using Torque.Compiler;


namespace Torque;




public readonly struct TorqueOptions
{
    public required FileInfo File { get; init; }

    public required string Output { get; init; }

    public required BitMode BitMode { get; init; }
    public required string EntryPoint { get; init; }
}
