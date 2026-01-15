using System;
using System.IO;


namespace Torque.CommandLine;




public static class TempFiles
{
    public static void ForTempFileDo(Action<string> action)
    {
        var tempFilePath = Path.GetTempFileName();

        action(tempFilePath);

        File.Delete(tempFilePath);
    }
}
