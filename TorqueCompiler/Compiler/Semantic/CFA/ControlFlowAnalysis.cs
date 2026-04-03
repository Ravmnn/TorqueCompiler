using System.Collections.Generic;


namespace Torque.Compiler.Semantic.CFA;




public static class ControlFlowAnalysis
{
    public static void ExecuteAllAnalysis(IEnumerable<ControlFlowGraph> graphs)
    {
        foreach (var graph in graphs)
            ExecuteAllAnalysis(graph);
    }


    public static void ExecuteAllAnalysis(ControlFlowGraph graph)
    {
        new ReachabilityAnalysis().Analyze(graph);
        new AllPathReturnsAnalysis().Analyze(graph);
    }
}
