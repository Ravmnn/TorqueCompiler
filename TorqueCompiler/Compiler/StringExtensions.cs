using System.Runtime.InteropServices;


namespace Torque.Compiler;




public static class StringExtensions
{
    public static unsafe sbyte* StringToSBytePtr(this string source)
        => (sbyte*)Marshal.StringToHGlobalAnsi(source);
}
