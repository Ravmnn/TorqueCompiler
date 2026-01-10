using System.Collections.Generic;

using Torque.Compiler.Types;
using Torque.Compiler.Diagnostics;
using Torque.Compiler.Diagnostics.Catalogs;


namespace Torque.Compiler;




public class ControlFlowAnalysisReporter(IReadOnlyList<ControlFlowGraph> graphs) : DiagnosticReporter<ControlFlowAnalyzerCatalog>
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
        }
    }




    private bool ReportIfNonVoidAndDoesNotReturn(FunctionType functionType, ControlFlowGraph graph)
    {
        if (functionType.IsVoid || AllExecutionPathOfGraphReturns(graph.Entry))
            return false;

        Report(ControlFlowAnalyzerCatalog.FunctionMustReturnFromAllPaths, location: graph.FunctionDeclaration.Location);
        return true;
    }


    private bool ReportIfVoidAndReturn(FunctionType functionType, ControlFlowGraph graph)
    {
        if (!functionType.IsVoid || !AnyExecutionPathOfGraphReturns(graph.Entry))
            return false;

        Report(ControlFlowAnalyzerCatalog.FunctionCannotReturnAValue, location: graph.FunctionDeclaration.Location);
        return true;
    }

    // TODO: clean code everywhere




    private bool AllExecutionPathOfGraphReturns(BasicBlock start)
    {
        if (start.State.HasReturn)
            return true;

        if (start.Successors.Count == 0)
            return false;

        return AllExecutionPathOfSuccessorsReturns(start);
    }


    private bool AllExecutionPathOfSuccessorsReturns(BasicBlock start)
    {
        foreach (var successor in start.Successors)
            if (!successor.State.HasReturn)
                if (!AllExecutionPathOfGraphReturns(successor))
                    return false;

        return true;
    }




    private bool AnyExecutionPathOfGraphReturns(BasicBlock start)
    {
        foreach (var successor in start.Successors)
            if (!successor.State.HasReturn)
                if (AnyExecutionPathOfGraphReturns(successor))
                    return true;

        return start.State.HasReturn;
    }
}
