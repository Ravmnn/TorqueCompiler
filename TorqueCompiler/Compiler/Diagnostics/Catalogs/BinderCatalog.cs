namespace Torque.Compiler.Diagnostics.Catalogs;




public enum BinderCatalog
{
    [Item(DiagnosticScope.Binder)] MultipleSymbolDeclaration,
    [Item(DiagnosticScope.Binder)] UndeclaredSymbol,
    [Item(DiagnosticScope.Binder)] SymbolIsNotAValue,
    [Item(DiagnosticScope.Binder)] MustBeAssignmentReference,
    [Item(DiagnosticScope.Binder)] OnlyDeclarationsCanExistInFileScope,
    [Item(DiagnosticScope.Binder)] FunctionsMustBeAtFileScope,
    [Item(DiagnosticScope.Binder)] ValueMustBeAddressable
}
