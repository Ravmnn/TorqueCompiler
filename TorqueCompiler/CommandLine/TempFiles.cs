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


    public static T ForTempFileDo<T>(Func<string, T> func)
    {
        var tempFilePath = Path.GetTempFileName();
        var returnValue = func(tempFilePath);
        File.Delete(tempFilePath);

        return returnValue;
    }
}
