using System.Collections.Generic;

using Torque.Compiler.Diagnostics;
using Torque.Compiler.Diagnostics.Catalogs;


namespace Torque.Compiler.Semantic.CFA;




public class ControlFlowGraphReporter : DiagnosticReporter<ControlFlowAnalyzerCatalog>
{
    public void ReportAll(IEnumerable<ControlFlowGraph> graphs)
    {
        foreach (var graph in graphs)
            Report(graph);
    }


    public void Report(ControlFlowGraph graph)
    {
        ReportIfGraphDoesNotReturn(graph);

        foreach (var block in graph.Blocks)
        {
            if (block.Statements.Count < 1)
                continue;

            ReportIfBlockIsUnreachable(block);
        }
    }


    private void ReportIfBlockIsUnreachable(BasicBlock block)
    {
        if (!block.State.IsReachable)
            Report(ControlFlowAnalyzerCatalog.UnreachableCode, location: block.Statements[0].Location);
    }


    private void ReportIfGraphDoesNotReturn(ControlFlowGraph graph)
    {
        if (!graph.IgnoreAllPathReturnsAnalysis && !graph.Entry.State.Returns)
            Report(ControlFlowAnalyzerCatalog.FunctionMustReturnFromAllPaths, location: graph.Location);
    }
}
