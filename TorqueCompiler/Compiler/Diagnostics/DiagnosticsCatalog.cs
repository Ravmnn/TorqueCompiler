using System;
using System.Reflection;


namespace Torque.Compiler.Diagnostics;




public readonly partial struct Diagnostic
{
    [AttributeUsage(AttributeTargets.Field)]
    private class ItemAttribute(DiagnosticScope scope, DiagnosticSeverity severity = DiagnosticSeverity.Error) : Attribute
    {
        public DiagnosticScope Scope { get; } = scope;
        public DiagnosticSeverity Severity { get; } = severity;
    }




    public enum LexerCatalog
    {
        [Item(DiagnosticScope.Lexer)] UnexpectedToken = 0,
        [Item(DiagnosticScope.Lexer)] UnclosedMultilineComment
    }


    public enum ParserCatalog
    {
        [Item(DiagnosticScope.Parser)] ExpectBlock = 0,
        [Item(DiagnosticScope.Parser)] ExpectSemicolonAfterStatement,
        [Item(DiagnosticScope.Parser)] ExpectExpression,
        [Item(DiagnosticScope.Parser)] ExpectIdentifier,
        [Item(DiagnosticScope.Parser)] ExpectTypeName,
        [Item(DiagnosticScope.Parser)] ExpectAssignmentOperator,
        [Item(DiagnosticScope.Parser)] ExpectLeftParen,
        [Item(DiagnosticScope.Parser)] ExpectRightParen,
        [Item(DiagnosticScope.Parser)] UnclosedBlock,
        [Item(DiagnosticScope.Parser)] WrongBlockPlacement
    }


    public enum BinderCatalog
    {
        [Item(DiagnosticScope.Binder)] MultipleSymbolDeclaration,
        [Item(DiagnosticScope.Binder)] UndeclaredSymbol,
        [Item(DiagnosticScope.Binder)] SymbolIsNotAValue,
        [Item(DiagnosticScope.Binder)] MustBeAssignmentReference
    }


    public enum TypeCheckerCatalog
    {
        [Item(DiagnosticScope.TypeChecker)] TypeDiffers,
        [Item(DiagnosticScope.TypeChecker)] PointerExpected,
        [Item(DiagnosticScope.TypeChecker)] CannotUseVoidHere,
        [Item(DiagnosticScope.TypeChecker)] ExpressionDoesNotReturnAnyValue,
        [Item(DiagnosticScope.TypeChecker)] CannotCallNonFunction,
        [Item(DiagnosticScope.TypeChecker)] ArityDiffers
    }




    private static (T, DiagnosticScope, DiagnosticSeverity) GetFromCatalog<T>(int code)
        where T : Enum
    {
        var enumType = typeof(T);
        var item = (T)Enum.ToObject(enumType, code);

        var name = Enum.GetName(enumType, item);
        var field = enumType.GetField(name!, BindingFlags.Public | BindingFlags.Static);
        var attribute = field!.GetCustomAttribute<ItemAttribute>()!;

        return (item, attribute.Scope, attribute.Severity);
    }
}
