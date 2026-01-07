using System.Globalization;

namespace Torque.Compiler;




public static class TokenExtensions
{
    public static bool IsKeyword(this string token)
        => Token.Keywords.ContainsKey(token);

    public static bool IsType(this string token)
        => Token.Primitives.ContainsKey(token);

    public static bool IsBoolean(this string token)
        => token is "true" or "false";

    public static bool IsChar(this string token)
        => token.StartsWith('\'') && token.EndsWith('\'');

    public static bool IsFloat(this string token)
        => token.Contains('.');


    public static ulong ValueFromInteger(this string token)
        => ulong.Parse(token);

    public static double ValueFromFloat(this string token)
        => double.Parse(token, CultureInfo.InvariantCulture);

    public static bool ValueFromBool(this string token)
        => token == "true";
}
