using System;


namespace Torque.CommandLine;




public static class EnumExtensions
{
    public static T StringToEnum<T>(this string @string) where T : struct, Enum
    {
        foreach (var value in Enum.GetValues<T>())
            if (value.ToString().Equals(@string, StringComparison.OrdinalIgnoreCase))
                return value;

        throw new ArgumentException("Enum does not have item with specified name");
    }
}
