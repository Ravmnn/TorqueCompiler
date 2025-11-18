namespace Torque.Compiler;




public static class TokenExtensions
{
    public static bool IsKeyword(this string token)
        => Token.Keywords.ContainsKey(token);

    public static bool IsType(this string token)
        => Token.Primitives.ContainsKey(token);

    public static bool IsBoolean(this string token)
        => token is "true" or "false";




    public static bool IsKeyword(this Token token)
        => token.Lexeme.IsKeyword();

    public static bool IsType(this Token token)
        => token.Lexeme.IsType();

    public static bool IsBoolean(this Token token)
        => token.Lexeme.IsBoolean();
}
