namespace Torque.Compiler.Diagnostics.Catalogs;




public enum TypeCheckerCatalog
{
    [Item(DiagnosticScope.TypeChecker)] TypeDiffers,
    [Item(DiagnosticScope.TypeChecker)] PointerExpected,
    [Item(DiagnosticScope.TypeChecker)] CannotUseVoidHere,
    [Item(DiagnosticScope.TypeChecker)] CannotUseLetHere,
    [Item(DiagnosticScope.TypeChecker)] ExpressionDoesNotReturnAnyValue,
    [Item(DiagnosticScope.TypeChecker)] CannotCallNonFunction,
    [Item(DiagnosticScope.TypeChecker)] ArityDiffers,
    [Item(DiagnosticScope.TypeChecker)] CannotHaveAZeroSizedArray,
    [Item(DiagnosticScope.TypeChecker)] ExpectedAReturnValue,
}
