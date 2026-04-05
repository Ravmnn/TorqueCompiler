namespace Torque.Compiler.Diagnostics.Catalogs;




public enum TypeCheckerCatalog
{
    [Item(DiagnosticScope.TypeChecker)] TypeDiffers,
    [Item(DiagnosticScope.TypeChecker)] PointerExpected,
    [Item(DiagnosticScope.TypeChecker)] CannotUseVoidHere,
    [Item(DiagnosticScope.TypeChecker)] ExpressionDoesNotReturnAnyValue,
    [Item(DiagnosticScope.TypeChecker)] CannotCallNonFunction,
    [Item(DiagnosticScope.TypeChecker)] ArityDiffers,
    [Item(DiagnosticScope.TypeChecker)] CannotHaveAZeroSizedArray,
    [Item(DiagnosticScope.TypeChecker)] ExpectedAReturnValue,
    [Item(DiagnosticScope.TypeChecker)] StructExpected,
    [Item(DiagnosticScope.TypeChecker)] UndeclaredStructMember,
    [Item(DiagnosticScope.TypeChecker)] CannotCastBetweenStructs,
    [Item(DiagnosticScope.TypeChecker)] InfiniteTypeRecursionChain,
    [Item(DiagnosticScope.TypeChecker)] ExpressionIncompatibleWithStructs,
    [Item(DiagnosticScope.TypeChecker)] ExpressionMustHaveNumericOperands,
    [Item(DiagnosticScope.TypeChecker)] ExpressionMustHaveIntegerOperands,
    [Item(DiagnosticScope.TypeChecker)] FunctionCannotReturnAValue,
}
