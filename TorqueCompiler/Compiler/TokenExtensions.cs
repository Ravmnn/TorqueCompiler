namespace Torque.Compiler;




public static class TokenExtensions
{
    public static bool IsKeyword(this string token)
        => Token.Keywords.ContainsKey(token);

    public static bool IsType(this string token)
        => Token.Primitives.ContainsKey(token);

    public static bool IsBoolean(this string token)
        => token is "true" or "false";


    public static ulong ValueFromNumber(this string token)
        => ulong.Parse(token);

    public static ulong ValueFromBool(this string token)
        => token == "true" ? 1UL : 0UL;




    public static bool IsKeyword(this Token token)
        => token.Lexeme.IsKeyword();

    public static bool IsType(this Token token)
        => token.Lexeme.IsType();

    public static bool IsBoolean(this Token token)
        => token.Lexeme.IsBoolean();


    public static ulong ValueFromNumber(this Token token)
        => ValueFromNumber(token.Lexeme);

    public static ulong ValueFromBool(this Token token)
        => ValueFromBool(token.Lexeme);
}
