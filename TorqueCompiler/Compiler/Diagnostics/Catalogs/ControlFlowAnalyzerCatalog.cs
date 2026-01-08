namespace Torque.Compiler.Diagnostics.Catalogs;




public enum ControlFlowAnalyzerCatalog
{
    [Item(DiagnosticScope.ControlFlowAnalyzer)] FunctionMustReturnFromAllPaths,
    [Item(DiagnosticScope.ControlFlowAnalyzer)] FunctionCannotReturnAValue,
    [Item(DiagnosticScope.ControlFlowAnalyzer, DiagnosticSeverity.Warning)] UnreachableCode
}
