using System.Collections.Generic;

using Torque.Compiler.BoundAST.Statements;


namespace Torque.Compiler.Semantic.CFA;




public class AllPathReturnsAnalysis
{
    private Queue<BasicBlock> _worklist = [];




    public bool Analyze(ControlFlowGraph graph)
    {
        _worklist = new Queue<BasicBlock>(graph.Blocks);
        InitializeBlocksReturnState(graph);

        while (_worklist.Count > 0)
            UpdateBlockReturnsState(_worklist.Dequeue());

        return graph.Entry.State.Returns;
    }


    private static void InitializeBlocksReturnState(ControlFlowGraph graph)
    {
        foreach (var block in graph.Blocks)
            block.State.Returns = false;
    }


    private void UpdateBlockReturnsState(BasicBlock block)
    {
        var newValue = BlockReturns(block);

        if (block.State.Returns == newValue)
            return;

        block.State.Returns = newValue;

        foreach (var predecessor in block.Predecessors)
            _worklist.Enqueue(predecessor);
    }


    private static bool BlockReturns(BasicBlock block)
    {
        if (EndsWithReturn(block))
            return true;

        if (block.Successors.Count == 0)
            return false;

        foreach (var successor in block.Successors)
            if (!successor.State.Returns)
                return false;

        return true;
    }


    private static bool EndsWithReturn(BasicBlock block)
        => block.Statements.Count > 0 && block.Statements[^1] is BoundReturnStatement;
}
