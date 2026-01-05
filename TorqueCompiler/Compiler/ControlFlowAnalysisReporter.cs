using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Diagnostics;


namespace Torque.Compiler;




public class ControlFlowAnalysisReporter(IReadOnlyList<ControlFlowGraph> graphs) : DiagnosticReporter<Diagnostic.ControlFlowAnalyzerCatalog>
{
    public IReadOnlyList<ControlFlowGraph> Graphs { get; } = graphs;




    public void Report()
    {
        foreach (var graph in Graphs)
        {
            var conclusion = graph.Entry; // for now, functions only have one block, since there are no control flow statements yet

            var functionDeclaration = graph.FunctionDeclaration;
            var functionType = functionDeclaration.Symbol.Type!;

            ReportIfNonVoidAndDoesNotReturn(functionType, graph);
            ReportIfVoidAndReturn(functionType, graph);
            ReportUnreachable(graph);
        }
    }




    private bool ReportIfNonVoidAndDoesNotReturn(FunctionType functionType, ControlFlowGraph graph)
    {
        if (functionType.IsVoid || graph.Conclusion().State.HasReturn)
            return false;

        var returnLocation = graph.Conclusion().Statements.LastOrDefault()?.Location ?? graph.FunctionDeclaration.Location;
        Report(Diagnostic.ControlFlowAnalyzerCatalog.FunctionMustReturnFromAllPaths, location: returnLocation);
        return true;
    }


    private bool ReportIfVoidAndReturn(FunctionType functionType, ControlFlowGraph graph)
    {
        if (!functionType.IsVoid || !graph.Conclusion().State.HasReturn)
            return false;

        var returnLocation = graph.Conclusion().Statements.Last().Location;
        Report(Diagnostic.ControlFlowAnalyzerCatalog.FunctionCannotReturnAValue, location: returnLocation);
        return true;
    }


    private void ReportUnreachable(ControlFlowGraph graph)
    {
        foreach (var block in graph.Blocks)
            if (!block.State.Reachable)
                Report(Diagnostic.ControlFlowAnalyzerCatalog.UnreachableCode, location: block.Statements.First().Location);
    }
}
