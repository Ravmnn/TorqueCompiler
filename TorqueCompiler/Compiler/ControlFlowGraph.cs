using System.Collections.Generic;


namespace Torque.Compiler;




public class ControlFlowGraph(BoundFunctionDeclarationStatement functionDeclaration)
{
    public BoundFunctionDeclarationStatement FunctionDeclaration { get; } = functionDeclaration;

    public BasicBlock Entry { get; set; } = null!;
    public List<BasicBlock> Blocks { get; } = [];




    public BasicBlock Conclusion()
        => FindLastSuccessorRecursively(Entry);


    private BasicBlock FindLastSuccessorRecursively(BasicBlock block)
    {
        foreach (var successor in block.Successors)
            return FindLastSuccessorRecursively(successor);

        return block;
    }
}
