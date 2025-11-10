using System.IO;


namespace Torque;




public readonly struct TorqueOptions
{
    public static TorqueOptions Global { get; set; }




    public required FileInfo File { get; init; }
}
