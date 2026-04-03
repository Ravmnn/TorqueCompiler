using System.Collections.Generic;


namespace Torque.Compiler.Semantic.CFA;




public class ReachabilityAnalysis
{
    private readonly Queue<BasicBlock> _worklist = [];




    public void Analyze(ControlFlowGraph graph)
    {
        _worklist.Clear();
        _worklist.Enqueue(graph.Entry);

        graph.Entry.State.IsReachable = true;

        while (_worklist.Count > 0)
            ProcessBlock(_worklist.Dequeue());
    }


    private void ProcessBlock(BasicBlock block)
    {
        foreach (var successor in block.Successors)
        {
            if (successor.State.IsReachable)
                continue;

            successor.State.IsReachable = true;
            _worklist.Enqueue(successor);
        }
    }
}
