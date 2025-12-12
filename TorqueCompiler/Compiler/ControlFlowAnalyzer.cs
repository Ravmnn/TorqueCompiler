using System.Collections.Generic;
using System.Linq;


namespace Torque.Compiler;




public class ControlFlowAnalyzer(IReadOnlyList<ControlFlowGraph> graphs)
{
    public IReadOnlyList<ControlFlowGraph> Graphs { get; } = graphs;




    public void AnalyzeAll()
    {
        foreach (var graph in Graphs)
            AnalyzeGraph(graph);
    }


    private void AnalyzeGraph(ControlFlowGraph graph)
    {
        graph.Entry.State.Reachable = true;

        AnalyzeBlock(graph.Entry);
    }


    private void AnalyzeBlock(BasicBlock block)
    {
        var outState = Transfer(block);

        foreach (var successor in block.Successors)
        {
            var merged = Merge(successor.State, outState);

            if (merged == successor.State)
                continue;

            successor.State = merged;
            AnalyzeBlock(successor);
        }
    }





    private static BlockState Transfer(BasicBlock block)
    {
        var hasReturn = block.Statements.Last() is BoundReturnStatement;


        return block.State with
        {
            HasReturn = hasReturn
        };
    }


    private static BlockState Merge(BlockState old, BlockState incoming)
        => new BlockState
        {
            Reachable = old.Reachable || incoming.Reachable,
            HasReturn = old.HasReturn && incoming.HasReturn
        };
}
