namespace Torque.Compiler.Diagnostics.Catalogs;




public enum BinderCatalog
{
    [Item(DiagnosticScope.Binder)] MultipleSymbolDeclaration,
    [Item(DiagnosticScope.Binder)] UndeclaredSymbol,
    [Item(DiagnosticScope.Binder)] SymbolIsNotAValue,
    [Item(DiagnosticScope.Binder)] MustBeAssignmentReference,
    [Item(DiagnosticScope.Binder)] ValueMustBeAddressable,
    [Item(DiagnosticScope.Binder)] MultipleSameModifiers,
    [Item(DiagnosticScope.Binder)] InvalidModifierTarget,
    [Item(DiagnosticScope.Binder)] FunctionMustHaveABody,
    [Item(DiagnosticScope.Binder)] ExternalFunctionCannotHaveABody,
    [Item(DiagnosticScope.Binder)] LoopControlInstructionMustBeInLoop,
    [Item(DiagnosticScope.Binder)] UnknownType,
    [Item(DiagnosticScope.Binder)] InvalidTypeKind,
    [Item(DiagnosticScope.Binder)] ThisStatementMustBePlacedAtFileScope,
    [Item(DiagnosticScope.Binder)] ThisStatementMustBePlacedAtFunctionScope,
    [Item(DiagnosticScope.Binder)] UnknownModule,
    [Item(DiagnosticScope.Binder)] SymbolAlreadyDeclaredAsType,
    [Item(DiagnosticScope.Binder)] ImportedSymbolHasMultipleDeclarations,
}
