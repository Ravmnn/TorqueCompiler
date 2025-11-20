using System.Collections.Generic;
using System.IO;


namespace Torque;




public readonly struct TorqueLinkOptions
{
    public required IEnumerable<FileInfo> Files { get; init; }

    public required string Output { get; init; }
}
