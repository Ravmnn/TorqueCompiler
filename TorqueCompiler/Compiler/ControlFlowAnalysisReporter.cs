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
            var functionDeclaration = graph.FunctionDeclaration;
            var functionType = functionDeclaration.Symbol.Type!;

            ReportIfNonVoidAndDoesNotReturn(functionType, graph);
            ReportIfVoidAndReturn(functionType, graph);
            ReportUnreachable(graph);
        }
    }




    private bool ReportIfNonVoidAndDoesNotReturn(FunctionType functionType, ControlFlowGraph graph)
    {
        if (functionType.IsVoid || AllExecutionPathOfGraphReturns(graph.Entry))
            return false;

        Report(Diagnostic.ControlFlowAnalyzerCatalog.FunctionMustReturnFromAllPaths, location: graph.FunctionDeclaration.Location);
        return true;
    }


    private bool ReportIfVoidAndReturn(FunctionType functionType, ControlFlowGraph graph)
    {
        if (!functionType.IsVoid || !AnyExecutionPathOfGraphReturns(graph.Entry))
            return false;

        Report(Diagnostic.ControlFlowAnalyzerCatalog.FunctionCannotReturnAValue, location: graph.FunctionDeclaration.Location);
        return true;
    }


    private void ReportUnreachable(ControlFlowGraph graph)
    {
        foreach (var block in graph.Blocks)
            if (!block.State.Reachable)
                Report(Diagnostic.ControlFlowAnalyzerCatalog.UnreachableCode, location: block.Statements.First().Location);
    }

    // TODO: clean code everywhere


    //  f
    //  f
    //  t


    private bool AllExecutionPathOfGraphReturns(BasicBlock start)
    {
        foreach (var sucessor in start.Successors)
        {
            if (!sucessor.State.HasReturn)
                if (!AllExecutionPathOfGraphReturns(sucessor))
                    return false;
        }

        return start.State.HasReturn || start.Successors.Count != 0;
    }


    private bool AnyExecutionPathOfGraphReturns(BasicBlock start)
    {
        foreach (var sucessor in start.Successors)
        {
            if (!sucessor.State.HasReturn)
                if (AnyExecutionPathOfGraphReturns(sucessor))
                    return true;
        }

        return start.State.HasReturn;
    }
}
