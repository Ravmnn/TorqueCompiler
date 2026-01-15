namespace Torque.Compiler.Diagnostics.Catalogs;




public enum ParserCatalog
{
    [Item(DiagnosticScope.Parser)] ExpectBlock,
    [Item(DiagnosticScope.Parser)] ExpectSemicolonAfterStatement,
    [Item(DiagnosticScope.Parser)] ExpectExpression,
    [Item(DiagnosticScope.Parser)] ExpectIdentifier,
    [Item(DiagnosticScope.Parser)] ExpectTypeName,
    [Item(DiagnosticScope.Parser)] ExpectAssignmentOperator,
    [Item(DiagnosticScope.Parser)] ExpectLeftParen,
    [Item(DiagnosticScope.Parser)] ExpectRightParen,
    [Item(DiagnosticScope.Parser)] ExpectLeftSquareBracket,
    [Item(DiagnosticScope.Parser)] ExpectRightSquareBracket,
    [Item(DiagnosticScope.Parser)] UnclosedBlock,
    [Item(DiagnosticScope.Parser)] UnexpectedToken,
    [Item(DiagnosticScope.Parser)] ExpectLiteralInteger,
    [Item(DiagnosticScope.Parser)] ExpectLeftCurlyBracket,
    [Item(DiagnosticScope.Parser)] ExpectRightCurlyBracket,
    [Item(DiagnosticScope.Parser)] OnlyFunctionsCanBeExternal,
}
