using Torque.Compiler;


namespace Torque;




public static class TorqueErrors
{
    public static LanguageException InvalidToken(TokenLocation location)
        => new LanguageException("Invalid token.", location);


    public static LanguageException UnclosedMultilineComment(TokenLocation location)
        => new LanguageException("Unclosed multiline comment.", location);
}
