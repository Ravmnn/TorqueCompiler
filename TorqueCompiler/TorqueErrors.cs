using Torque.Compiler;


namespace Torque;




public static class TorqueErrors
{
    public static LanguageException InvalidToken(TokenLocation location)
        => new LanguageException("Invalid token.", location);


    public static LanguageException UnclosedMultilineComment(TokenLocation location)
        => new LanguageException("Unclosed multiline comment.", location);




    public static LanguageException ExpectBlockStatement(TokenLocation location)
        => new LanguageException("Expect block statement.", location);


    public static LanguageException ExpectSemicolonAfterStatement(TokenLocation location)
        => new LanguageException("Expect \";\" after statement.", location);


    public static LanguageException ExpectExpression(TokenLocation location)
        => new LanguageException("Expect expression.", location);


    public static LanguageException ExpectIdentifier(TokenLocation location)
        => new LanguageException("Expect identifier.", location);


    public static LanguageException ExpectTypeName(TokenLocation location)
        => new LanguageException("Expect type name.", location);


    public static LanguageException ExpectTypeSpecifier(TokenLocation location)
        => new LanguageException("Expect type specifier.", location);


    public static LanguageException ExpectAssignmentOperator(TokenLocation location)
        => new LanguageException("Expect assignment operator.", location);


    public static LanguageException ExpectLeftParenAfterFunctionName(TokenLocation location)
        => new LanguageException("Expect \"(\" after function name.", location);


    public static LanguageException ExpectRightParenBeforeReturnType(TokenLocation location)
        => new LanguageException("Expect \")\" before return type specifier.", location);


    public static LanguageException ExpectReturnTypeSpecifierAfterParameters(TokenLocation location)
        => new LanguageException("Expect function return type specifier after parameter list.", location);


    public static LanguageException ExpectRightParenAfterArguments(TokenLocation location)
        => new LanguageException("Expect \")\" after argument list.", location);


    public static LanguageException UnclosedGroupingExpression(TokenLocation location)
        => new LanguageException("Unclosed grouping expression.", location);


    public static LanguageException UnclosedBlockStatement(TokenLocation location)
        => new LanguageException("Unclosed block statement.", location);


    public static LanguageException WrongKeywordPlacement(TokenLocation location)
        => new LanguageException("Wrong keyword placement.", location);
}
