using System.Globalization;


namespace Torque.Compiler.Tokens;




public static class TokenExtensions
{
    public static bool IsKeyword(this string token)
        => Keywords.General.ContainsKey(token);

    public static bool IsType(this string token)
        => Keywords.PrimitiveTypes.ContainsKey(token);


    public static bool IsLiteralBoolean(this string token)
        => token is "true" or "false";

    public static bool IsLiteralChar(this string token)
        => token.StartsWith('\'') && token.EndsWith('\'');

    public static bool IsLiteralFloat(this string token)
        => token.Contains('.');


    public static ulong ValueFromInteger(this string token)
        => ulong.Parse(token);

    public static double ValueFromFloat(this string token)
        => double.Parse(token, CultureInfo.InvariantCulture);

    public static bool ValueFromBool(this string token)
        => token == "true";
}
