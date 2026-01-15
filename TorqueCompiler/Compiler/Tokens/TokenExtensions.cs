using System.Globalization;


namespace Torque.Compiler.Tokens;




public static class TokenExtensions
{
    extension(string token)
    {
        public bool IsKeyword()
            => Keywords.General.ContainsKey(token);

        public bool IsModifier()
            => Keywords.Modifiers.ContainsKey(token);

        public bool IsType()
            => Keywords.PrimitiveTypes.ContainsKey(token);

        public bool IsLiteralBoolean()
            => token is "true" or "false";

        public bool IsLiteralChar()
            => token.StartsWith('\'') && token.EndsWith('\'');

        public bool IsLiteralFloat()
            => token.Contains('.');

        public ulong ValueFromInteger()
            => ulong.Parse(token);

        public double ValueFromFloat()
            => double.Parse(token, CultureInfo.InvariantCulture);

        public bool ValueFromBool()
            => token == "true";
    }
}
