namespace Torque.Compiler.Diagnostics.Catalogs;




public enum LexerCatalog
{
    [Item(DiagnosticScope.Lexer)] UnexpectedToken,
    [Item(DiagnosticScope.Lexer)] UnclosedMultilineComment,
    [Item(DiagnosticScope.Lexer)] SingleCharacterEmpty,
    [Item(DiagnosticScope.Lexer)] SingleCharacterMoreThanOne,
    [Item(DiagnosticScope.Lexer)] UnclosedSingleCharacterString,
    [Item(DiagnosticScope.Lexer)] UnknownEscapeSequence,
    [Item(DiagnosticScope.Lexer)] MoreThanOneDotInFloatNumber
}
