using System.Collections.Generic;
using System.Linq;


namespace Torque.Compiler;




public class ControlFlowGraph(BoundFunctionDeclarationStatement functionDeclaration)
{
    public BoundFunctionDeclarationStatement FunctionDeclaration { get; } = functionDeclaration;

    public BasicBlock Entry { get; set; } = null!;
    public List<BasicBlock> Blocks { get; } = [];




    public BlockState GetFinalState()
    {
        if (Blocks.Count == 0)
            return default;

        var currentState = Entry.State;

        foreach (var sucessor in Entry.Successors)
        {

        }

        return default;
    }




    private BlockState MergeBlockStateWithPredecessors(BlockState block, params IReadOnlyList<BlockState> predecessors)
        => new BlockState
        {
            Reachable = block.Reachable || predecessors.Any(predecessor => predecessor.Reachable),
            HasReturn = block.HasReturn || predecessors.All(predecessor => predecessor.HasReturn)
        };



    private BasicBlock FindLastSuccessorRecursively(BasicBlock block)
    {
        foreach (var successor in block.Successors)
            return FindLastSuccessorRecursively(successor);

        return block;
    }
}
